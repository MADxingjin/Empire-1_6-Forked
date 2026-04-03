using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    public static class FCEventMaker
    {
        public static void CalculateSuccess(FCOptionDef option, FCEvent parentEvent)
        {
            float baseChance = option.baseChanceOfSuccess;
            int roll = Rand.Range(1, 100);

            FCEvent tempEvent = new FCEvent(true);


            if (roll <= baseChance)
            {
                //if success
                if (option.parentEvent.settlementsCarryOver)
                {
                    tempEvent = MakeRandomEvent(option.successEvent, parentEvent.settlementTraitLocations);
                }
                else
                {
                    tempEvent = MakeRandomEvent(option.successEvent, null);
                }
            }
            else
            {
                if (option.parentEvent.settlementsCarryOver)
                {
                    tempEvent = MakeRandomEvent(option.failEvent, parentEvent.settlementTraitLocations);
                }
                else
                {
                    tempEvent = MakeRandomEvent(option.failEvent, null);
                }
            }

            if (tempEvent.def != FCEventDefOf.Null)
            {
                FactionCache.FactionComp.AddEvent(tempEvent);

                Find.LetterStack.ReceiveLetter(tempEvent.def.label, BuildEventLetterBody(tempEvent), LetterDefOf.NeutralEvent);
            }
        }

        public static string BuildEventLetterBody(FCEvent evt)
        {
            string desc = evt.hasCustomDescription && !evt.customDescription.NullOrEmpty()
                ? evt.customDescription
                : evt.def.desc ?? "";

            string body = desc;

            // Stat modifiers
            TaggedString statDesc = FCStatModifier.GetDescription(evt.def.statModifiers);
            if (!statDesc.NullOrEmpty())
            {
                body += "\n\n" + statDesc;
            }

            // Permanent stat modifiers
            TaggedString permDesc = FCStatModifier.GetDescription(evt.def.permanentStatModifiers);
            if (!permDesc.NullOrEmpty())
            {
                body += "\n\n" + permDesc;
            }

            // Affected settlements
            if (evt.settlementTraitLocations != null && evt.settlementTraitLocations.Count > 0)
            {
                string settlementString = evt.settlementTraitLocations
                    .Where(s => s != null)
                    .Join(s => " " + s.Name, "\n");
                if (!settlementString.NullOrEmpty())
                {
                    body += "\n\n" + "EventAffectingSettlements".Translate() + "\n" + settlementString;
                }
            }

            return body;
        }

        public static bool IsValidRandomEvent(FCEventDef cEvent)
        {
            FactionFC tmp = FactionCache.FactionComp;

            if (!cEvent.isRandomEvent) return false;
            if (FCSettings.IsEventDisabled(cEvent.defName)) return false;
            if (Find.World.PlayerWealthForStoryteller < cEvent.requiredWealth) return false;

            // Stat range checks
            if (cEvent.minimumHappiness > tmp.averageHappiness || tmp.averageHappiness > cEvent.maximumHappiness) return false;
            if (cEvent.minimumLoyalty > tmp.averageLoyalty || tmp.averageLoyalty > cEvent.maximumLoyalty) return false;
            if (cEvent.minimumUnrest > tmp.averageUnrest || tmp.averageUnrest > cEvent.maximumUnrest) return false;
            if (cEvent.minimumProsperity > tmp.averageProsperity || tmp.averageProsperity > cEvent.maximumProsperity) return false;

            // Settlement count check
            bool noSettlementRequirement = cEvent.rangeSettlementsAffected.min == 0
                                           && cEvent.rangeSettlementsAffected.max == 0
                                           && !cEvent.targetAllSettlements;
            if (!noSettlementRequirement && FactionCache.FactionComp.settlements.Count < cEvent.rangeSettlementsAffected.min) return false;
            if (cEvent.targetAllSettlements && FactionCache.FactionComp.settlements.Count == 0) return false;

            // Biome check — for settlement-targeting events, at least one settlement must qualify
            if (!noSettlementRequirement && (cEvent.applicableBiomes.Count > 0 || cEvent.restrictedBiomes.Count > 0))
            {
                bool anyMatch = false;
                foreach (WorldSettlementFC s in FactionCache.FactionComp.settlements)
                {
                    if (cEvent.BiomeAllowed(s.biome)) { anyMatch = true; break; }
                }
                if (!anyMatch) return false;
            }

            // Required resource check
            if (cEvent.requiredResource != null)
            {
                bool hasResource = FactionCache.FactionComp.ReturnResource(cEvent.requiredResource).amount > 0;
                if (!hasResource) return false;
            }

            // Incompatible/duplicate event check
            // Faction-wide events are blocked globally if already active.
            // Settlement-specific events are allowed through — MakeRandomEvent handles per-settlement filtering.
            foreach (FCEvent evt in FactionCache.FactionComp.events)
            {
                if (evt.def == null) continue;
                if (cEvent == evt.def && noSettlementRequirement) return false;

                foreach (FCEventDef inEvt in evt.def.incompatibleEvents)
                {
                    if (cEvent == inEvt) return false;
                }
            }

            // Tech level and research requirements
            if (!cEvent.SatisfiesTechRequirements(tmp.techLevel)) return false;

            // Cooldown check
            if (tmp.IsEventOnCooldown(cEvent)) return false;

            // Max fire count check
            if (tmp.HasReachedMaxFireCount(cEvent)) return false;

            // Minimum settlements prerequisite (independent of rangeSettlementsAffected targeting)
            if (cEvent.minSettlements > 0 && tmp.settlements.Count < cEvent.minSettlements) return false;

            // Required policy/trait/edict
            if (cEvent.requiredPolicy != null
                && !tmp.HasPolicy(cEvent.requiredPolicy)
                && !tmp.HasTrait(cEvent.requiredPolicy)
                && !tmp.HasEdict(cEvent.requiredPolicy)) return false;

            // Minimum faction age
            if (cEvent.minDaysSinceFounded > 0
                && (Find.TickManager.TicksGame - tmp.FoundingTick) < cEvent.minDaysSinceFounded * GenDate.TicksPerDay) return false;

            return true;
        }

        public static FCEventDef ReturnRandomEvent()
        {
            List<FCEventDef> tmpEventList = new List<FCEventDef>();

            foreach (FCEventDef eventDef in DefDatabase<FCEventDef>.AllDefsListForReading)
            {
                if (IsValidRandomEvent(eventDef))
                {
                    for (int i = 0; i < eventDef.weight; i++)
                    {
                        tmpEventList.Add(eventDef);
                    }
                }
            }

            if (tmpEventList.Count() == 0)
                return null;

            FCEventDef selected = tmpEventList.RandomElement();

            // Allow active behaviors to request a single re-roll
            FactionFC faction = FactionCache.FactionComp;
            if (faction != null)
            {
                bool reroll = false;
                faction.ForEachBehavior(b =>
                {
                    if (!reroll && b.ShouldRerollEvent(selected))
                        reroll = true;
                });
                if (reroll)
                    selected = tmpEventList.RandomElement();
            }

            return selected;
        }

        public static FCEvent MakeEvent(FCEventDef def)
        {
            if (def == null)
            {
                return null;
            }

            FCEvent tempEvent = new FCEvent(true);
            tempEvent.def = def;
            tempEvent.tickStarted = Find.TickManager.TicksGame;
            int duration = def.timeTillTrigger;
            if (def.HasVariableDuration)
            {
                duration = Rand.Range(def.timeTillTrigger, def.timeTillTriggerMax);
                tempEvent.timeMinTrigger = Find.TickManager.TicksGame + def.timeTillTrigger;
                tempEvent.timeMaxTrigger = Find.TickManager.TicksGame + def.timeTillTriggerMax;
                LogUtil.Message($"Making event {def.defName} with variable duration. Min: {def.timeTillTrigger}, Max: {def.timeTillTriggerMax}, Duration: {duration}");
            }
            else
            {
                LogUtil.Message($"Making event {def.defName} with duration {duration}");
            }
            tempEvent.timeTillTrigger = Find.TickManager.TicksGame + duration;
            return tempEvent;
        }

        public static FCEvent MakeRandomEvent(FCEventDef def, List<WorldSettlementFC> SettlementTraitLocations)
        {
            if (def is null) return null;

            FactionFC worldcomp = FactionCache.FactionComp;

            int now = Find.TickManager.TicksGame;
            int duration = def.timeTillTrigger;
            FCEvent tempEvent = new FCEvent(true)
            {
                def = def,
                tickStarted = now,
                settlementTraitLocations = new List<WorldSettlementFC>()
            };
            if (def.HasVariableDuration)
            {
                duration = Rand.Range(def.timeTillTrigger, def.timeTillTriggerMax);
                tempEvent.timeMinTrigger = now + def.timeTillTrigger;
                tempEvent.timeMaxTrigger = now + def.timeTillTriggerMax;
            }
            tempEvent.timeTillTrigger = now + duration;

            try
            {
                // Carry over settlement locations from parent event, or pick new ones
                if (SettlementTraitLocations != null && SettlementTraitLocations.Count > 0)
                {
                    tempEvent.settlementTraitLocations.AddRange(SettlementTraitLocations);
                }
                else if (tempEvent.def.targetAllSettlements)
                {
                    // Deterministically target every qualifying settlement
                    HashSet<WorldSettlementFC> excludedSettlements = new HashSet<WorldSettlementFC>();
                    foreach (FCEvent activeEvt in worldcomp.events)
                    {
                        if (activeEvt.def == null) continue;
                        bool isSameDef = activeEvt.def == def;
                        bool isIncompatible = false;
                        if (!isSameDef)
                        {
                            foreach (FCEventDef inEvt in activeEvt.def.incompatibleEvents)
                            {
                                if (inEvt == def) { isIncompatible = true; break; }
                            }
                        }
                        if (isSameDef || isIncompatible)
                        {
                            foreach (WorldSettlementFC s in activeEvt.settlementTraitLocations)
                            {
                                if (s != null) excludedSettlements.Add(s);
                            }
                        }
                    }

                    foreach (WorldSettlementFC settlement in worldcomp.settlements)
                    {
                        if (excludedSettlements.Contains(settlement)) continue;
                        if (!tempEvent.def.BiomeAllowed(settlement.biome)) continue;
                        if (!tempEvent.def.SettlementTypeAllowed(settlement.settlementDef)) continue;
                        if (tempEvent.def.requiredResource != null)
                        {
                            ResourceFC res = settlement.GetResource(tempEvent.def.requiredResource);
                            // null always fails theta comparisons, so this check is safe
                            if (res?.InstantaneousProduction <= 0) continue;
                        }
                        tempEvent.settlementTraitLocations.Add(settlement);
                    }

                    if (tempEvent.settlementTraitLocations.Count == 0)
                    {
                        LogUtil.Warning($"targetAllSettlements event '{def.defName}' found no qualifying settlements");
                        return null;
                    }
                }
                else if (tempEvent.def.rangeSettlementsAffected.max != 0)
                {
                    int numSettlements = tempEvent.def.rangeSettlementsAffected.RandomInRange;

                    //if random number of settlements more than total settlements, reset number settlements.
                    if (numSettlements > worldcomp.settlements.Count())
                    {
                        numSettlements = worldcomp.settlements.Count();
                    }

                    //List of map locations
                    List<WorldSettlementFC> settlements = new List<WorldSettlementFC>();
                    //temporary list of settlemnts.
                    List<WorldSettlementFC> tmp = new List<WorldSettlementFC>();

                    // Exclude settlements already affected by the same or an incompatible event
                    HashSet<WorldSettlementFC> excludedSettlements = new HashSet<WorldSettlementFC>();
                    foreach (FCEvent activeEvt in worldcomp.events)
                    {
                        if (activeEvt.def == null) continue;
                        bool isSameDef = activeEvt.def == def;
                        bool isIncompatible = false;
                        if (!isSameDef)
                        {
                            foreach (FCEventDef inEvt in activeEvt.def.incompatibleEvents)
                            {
                                if (inEvt == def) { isIncompatible = true; break; }
                            }
                        }
                        if (isSameDef || isIncompatible)
                        {
                            foreach (WorldSettlementFC s in activeEvt.settlementTraitLocations)
                            {
                                if (s != null) excludedSettlements.Add(s);
                            }
                        }
                    }

                    foreach (WorldSettlementFC settlement in worldcomp.settlements.InRandomOrder())
                    {
                        if (excludedSettlements.Contains(settlement)) continue;
                        if (!tempEvent.def.BiomeAllowed(settlement.biome)) continue;
                        if (!tempEvent.def.SettlementTypeAllowed(settlement.settlementDef)) continue;
                        if (tempEvent.def.requiredResource != null)
                        {
                            ResourceFC res = settlement.GetResource(tempEvent.def.requiredResource);
                            if (res != null && res.InstantaneousProduction > 0)
                            {
                                // Settlements that produce more of a resource should have a higher weight
                                for (int i = 0; i < Math.Max(0, res.InstantaneousProduction); i++)
                                {
                                    tmp.Add(settlement);
                                }
                            }
                        }
                        else
                        {
                            tmp.Add(settlement);
                        }
                    }

                    // Pick first settlement randomly
                    if (tmp.Count > 0)
                    {
                        WorldSettlementFC first = tmp.RandomElement();
                        settlements.Add(first);
                        tmp.Remove(first);
                    }

                    // Pick remaining settlements, weighted by proximity to the first
                    while (tmp.Count > 0 && settlements.Count < numSettlements)
                    {
                        WorldSettlementFC next;
                        if (tempEvent.def.useProximity && settlements.Count > 0)
                        {
                            next = SelectByProximity(tmp, settlements[0], tempEvent.def.proximityFalloff);
                        }
                        else
                        {
                            next = tmp.RandomElement();
                        }

                        if (!settlements.Contains(next))
                        {
                            settlements.Add(next);
                        }
                        tmp.Remove(next);
                    }

                    tempEvent.settlementTraitLocations.AddRange(settlements);

                    // If no valid settlements were chosen, then return early instead of firing the event
                    if (tempEvent.settlementTraitLocations.Count == 0)
                    {
                        LogUtil.Warning($"Random event '{def.defName}' found no valid settlements"
                                        + (def.requiredResource != null ? $" (requires {def.requiredResource.defName} production)" : "")
                                        + (def.applicableBiomes.Count > 0 ? $" (biomes: {string.Join(", ", def.applicableBiomes)})" : "")
                                        + (def.restrictedBiomes.Count > 0 ? $" (excluded biomes: {string.Join(", ", def.restrictedBiomes)})" : ""));
                        return null;
                    }
                }

                //if event has options
                //open event option window
                if (tempEvent.def.options.Count > 0 && tempEvent.def.activateAtStart)
                {
                    Find.WindowStack.Add(new FCOptionWindow(tempEvent.def, tempEvent));
                    return null;
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"Couldn't create Random Event with def: {def?.defName ?? "NULL"} and list of size: {SettlementTraitLocations?.Count ?? 0}: {e.Message}");
            }

            return tempEvent;
        }


        private static WorldSettlementFC SelectByProximity(
            List<WorldSettlementFC> candidates, WorldSettlementFC anchor, float falloff)
        {
            float totalWeight = 0f;
            float[] weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                float dist = Find.WorldGrid.ApproxDistanceInTiles(anchor.Tile, candidates[i].Tile);
                weights[i] = 1f / (1f + dist / falloff);
                totalWeight += weights[i];
            }

            float roll = Rand.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        public static void ProcessEvents(in List<FCEvent> events)
        {
            FactionFC faction = FactionCache.FactionComp;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i].timeTillTrigger > Find.TickManager.TicksGame) continue;

                FCEvent evt = events[i];
                faction.events.RemoveAt(i);
                faction.eventsVersion++;

                // Record cooldown for events that define one
                if (evt.def != null && evt.def.cooldownTicks > 0)
                {
                    faction.RecordEventCooldown(evt.def);
                }

                // Track fire count for events with a max
                if (evt.def?.maxFireCount > 0)
                {
                    faction.RecordEventFired(evt.def);
                }

                if (evt.def == null)
                {
                    LogUtil.Warning($"Skipping event with null def (loadID={evt.loadID}). Likely corrupted save data.");
                    continue;
                }

                WorldSettlementFC settlement;

                LogUtil.Message($"Processing event {evt.def.defName}");

                FCEventHandlerExtension handler = evt.def.GetModExtension<FCEventHandlerExtension>();
                bool handled = handler != null && handler.ResolveEvent(evt, faction);

                if (!handled)
                {
                    switch (evt.def.defName)
                    {
                        case "settleNewColony":
                        {
                            try
                            {
                                //Settle new colony event
                                faction.AddExperienceToFactionLevel(10f);

                                ColonyUtil.CreatePlayerColonySettlement(evt.location, evt.settlementToCreate);

                                faction.settlementCaravansList.Remove(evt.location);
                            }
                            catch (Exception e)
                            {
                                LogUtil.Error($"Exception processing event '{evt.def?.defName ?? "NULL"}' (loadID={evt.loadID}): {e}");

                                faction.settlementCaravansList.Remove(evt.location);
                            }
                            break;
                        }
                        case "taxColony":
                        {
                            settlement = faction.ReturnSettlementByLocation(evt.source);
                            if (settlement == null)
                            {
                                LogUtil.Warning($"taxColony event references missing settlement at tile {evt.source}. Skipping delivery.");
                                break;
                            }

                            string str = "TaxesFrom".Translate() + " " + settlement.Name + " " + "HaveBeenDelivered".Translate() + "!";

                            Message msg = new Message(str, MessageTypeDefOf.PositiveEvent);

                            PaymentUtil.DeliverThings(evt, LetterMaker.MakeLetter("TaxesHaveArrived".Translate(), str + "\n" + evt.goods.ToLetterString(), LetterDefOf.PositiveEvent), msg);
                            break;
                        }
                        case "constructBuilding":
                            //Create building
                            settlement = faction.ReturnSettlementByLocation(evt.source);
                            if (settlement != null)
                            {
                                settlement.ConstructBuilding(evt.building, evt.buildingSlot);
                                Messages.Message("BuildingEventCompletedMsg".Translate(evt.building.LabelCap, settlement.Name), MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                LogUtil.Error($"Attempted to resolve a constructBuilding event for an invalid settlement");
                            }
                            break;
                        case "upgradeSettlement":
                        {
                            if (faction.ReturnSettlementByLocation(evt.location) != null)
                            {
                                //if settlement is not null
                                settlement = faction.ReturnSettlementByLocation(evt.location);
                                settlement.UpgradeSettlement();
                                Find.LetterStack.ReceiveLetter("UpgradeSettlement".Translate(),
                                    "UpgradeEventCompletedDesc".Translate(settlement.Name, settlement.settlementLevel, "UpgradeColonyDesc".Translate()),
                                    LetterDefOf.PositiveEvent);
                                /* We set these values here, instead of in UpgradeSettlement(), because sometimes UpgradeSettlement is called to handle changing a settlement's level outside of the
                                     * "upgrade settlement" event. We only want to reset these values as a result of resolving the event, so, we handle that here. */
                                settlement.isUpgrading = false;
                                settlement.startUpgradeTick = -1;
                                settlement.finishUpgradeTick = -1;
                            }

                            break;
                        }
                        case "captureEnemySettlement":
                        case "raidEnemySettlement":
                        case "enslaveEnemySettlement":
                        {
                            WorldSettlementFC militarySettlement = faction.ReturnSettlementByLocation(evt.location);
                            if (militarySettlement != null)
                                militarySettlement.MilitaryComp?.ProcessMilitaryEvent();
                            else
                                LogUtil.Warning($"Military event '{evt.def.defName}' references missing settlement at tile {evt.location}. Skipping.");
                            break;
                        }
                        case "cooldownMilitary":
                        {
                            WorldSettlementFC cooldownSettlement = faction.ReturnSettlementByLocation(evt.location);
                            if (cooldownSettlement != null)
                                cooldownSettlement.MilitaryComp?.ReturnMilitary(true);
                            else
                                LogUtil.Warning($"cooldownMilitary event references missing settlement at tile {evt.location}. Skipping.");
                            break;
                        }
                    }

                    if (evt.def.defName == "settlementBeingAttacked")
                    {
                        if (evt.settlementFCDefending == null)
                        {
                            LogUtil.Warning($"settlementBeingAttacked event has null settlementFCDefending (loadID={evt.loadID}). Skipping defense.");
                        }
                        else if (evt.settlementFCDefending is WorldSettlementFC worldSettlement)
                        {
                            if (worldSettlement.MilitaryComp == null)
                            {
                                LogUtil.Warning($"settlementBeingAttacked: {worldSettlement.Name} has no MilitaryComp. Skipping defense.");
                            }
                            else
                            {
                                worldSettlement.MilitaryComp.StartDefence(evt, () => SetupAttack(worldSettlement, evt));
                            }
                        }
                        else
                        {
                            // External raid target (registered via RaidTargetRegistry) — auto-resolve only
                            ResolveExternalRaidTarget(evt);
                        }
                    }
                    else //if undefined event
                    {
                        if (evt.def.randomThingValue > 0 && evt.def.randomThingRewardDef != null)
                        {
                            List<Thing> list = PaymentUtil.GenerateRewardThings(evt.def.randomThingValue, evt.def.randomThingRewardDef);

                            string str = "GoodsReceivedFollowing".Translate(evt.def.label);

                            str = list.Aggregate(str, (before, after) => before + "\n" + after.LabelCap);

                            evt.goods.AddRange(list);

                            evt.let = LetterMaker.MakeLetter("GoodsReceived".Translate(), str, LetterDefOf.PositiveEvent);
                            if (list.Count > 0)
                            {
                                if (!evt.source.IsValidTile())
                                {
                                    if (evt.settlementTraitLocations.Any())
                                    {
                                        evt.source = evt.settlementTraitLocations.First().Tile;
                                    }
                                    else
                                    {
                                        evt.source = FactionCache.FactionComp.capitalLocation;
                                    }
                                }
                                DeliveryEvent.CreateDeliveryEvent(evt);
                            }
                        }
                    }
                }

                //If has loot to give
                if (evt.def.loot.Any())
                {
                    List<Thing> list = evt.def.loot.Select(thing => ThingMaker.MakeThing(thing)).ToList();
                    PaymentUtil.DeliverThings(list, evt.source);
                }


                //check if event has a location, if does, remove stat modifiers from that specific location;
                if (evt.settlementTraitLocations.Any()) //if has specific locations
                {
                    evt.settlementTraitLocations.RemoveAll(s => s == null);

                    foreach (WorldSettlementFC location in evt.settlementTraitLocations)
                    {
                        if (location != null)
                        {
                            location.RemoveStatModifiers(evt.def.statModifiers, "event_" + evt.def.defName);

                            //prosperity loss calculation
                            location.prosperity -= evt.def.prosperityLost;
                        }
                    }
                }
                else
                {
                    //if no specific location then faction wide
                    foreach (WorldSettlementFC worldsettlement in faction.settlements)
                    {
                        worldsettlement.RemoveStatModifiers(evt.def.statModifiers, "event_" + evt.def.defName);
                        worldsettlement.prosperity -= evt.def.prosperityLost;
                    }
                }

                faction.InvalidateFactionStatCache();

                //if have options
                if (evt.def != null && evt.def.options.Count > 0 && evt.def.activateAtStart == false)
                {
                    Find.WindowStack.Add(new FCOptionWindow(evt.def, evt));
                }

                //if has following event
                if (evt.def.eventFollows)
                {
                    FCEvent tempEvent = new FCEvent(true);
                    if (evt.def.splitEventFollows) //if a split event
                    {
                        //remove null settlement references
                        float baseChance = evt.def.splitEventChance;
                        int roll = Rand.Range(1, 100);
                        if (evt.def.settlementsCarryOver)
                        {
                            //if settlements carry
                            if (roll <= baseChance)
                            {
                                //first event
                                tempEvent = MakeRandomEvent(evt.def.followingEvent, evt.settlementTraitLocations);
                            }
                            else
                            {
                                //if second event
                                tempEvent = MakeRandomEvent(evt.def.followingEvent2, evt.settlementTraitLocations);
                            }
                        }
                        else
                        {
                            if (roll <= baseChance)
                            {
                                //first event
                                tempEvent = MakeRandomEvent(evt.def.followingEvent, null);
                            }
                            else
                            {
                                //if second event
                                tempEvent = MakeRandomEvent(evt.def.followingEvent2, null);
                            }
                        }
                    }
                    else
                    {
                        if (evt.def.settlementsCarryOver)
                        {
                            //if settlements carry
                            tempEvent = MakeRandomEvent(evt.def.followingEvent, evt.settlementTraitLocations);
                        }
                        else
                        {
                            tempEvent = MakeRandomEvent(evt.def.followingEvent, null);
                        }
                    }


                    if (tempEvent != null)
                    {
                        faction.AddEvent(tempEvent);

                        Find.LetterStack.ReceiveLetter(tempEvent.def.label, BuildEventLetterBody(tempEvent), LetterDefOf.NeutralEvent);
                    }
                }

                evt.RunAction();
            }
        }

        /// <summary>
        /// Auto-resolves a raid on an external <see cref="IRaidTarget"/> (registered via <see cref="RaidTargetRegistry"/>).
        /// Called when the 24-hour warning timer expires for a non-<see cref="WorldSettlementFC"/> target.
        /// </summary>
        private static void ResolveExternalRaidTarget(FCEvent evt)
        {
            IRaidTarget target = RaidTargetRegistry.FindByWorldObject(evt.settlementFCDefending);
            if (target == null)
            {
                LogUtil.Warning($"settlementBeingAttacked: target at tile {evt.location} not found in RaidTargetRegistry. Skipping.");
                return;
            }

            try
            {
                BattleResult result = SimulateBattleFc.FightBattle(evt.militaryForceAttacking, evt.militaryForceDefending);

                if (result.DefenderVictory)
                {
                    target.OnRaidWon(result);
                    FactionCache.FactionComp.AddExperienceToFactionLevel(5f);
                    FactionCache.FactionComp.threatAdaptation.Notify_BattleWon();
                }
                else
                {
                    target.OnRaidLost(result);
                    FactionCache.FactionComp.threatAdaptation.Notify_BattleLost();
                }

                // Handle defending settlement cooldown (if an Empire settlement was assigned as defender)
                if (evt.militaryForceDefending?.homeSettlement != null)
                {
                    var defenderComp = evt.militaryForceDefending.homeSettlement.MilitaryComp;
                    if (defenderComp != null)
                    {
                        int remaining = (int)Math.Max(0, evt.militaryForceDefending.forceRemaining);
                        int initial = (int)Math.Max(0, evt.militaryForceDefending.militaryLevel * evt.militaryForceDefending.militaryEfficiency);
                        int deaths = Math.Max(0, initial - remaining);
                        if (result.DefenderVictory && remaining >= initial)
                        {
                            defenderComp.ReturnMilitary(true);
                        }
                        else
                        {
                            defenderComp.CooldownMilitaryFinal(deaths);
                        }
                    }
                }

                // Notify external auto-defender if one was assigned
                if (evt.externalDefenderSource != null)
                {
                    IAutoDefender autoDefender = AutoDefenderRegistry.FindByWorldObject(evt.externalDefenderSource);
                    if (autoDefender != null)
                    {
                        autoDefender.OnDefenseComplete(result.DefenderVictory, result);
                    }
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"Error resolving external raid target at tile {evt.location}: {e}");
            }
            finally
            {
                target.IsUnderAttack = false;
            }
        }

        private static void SetupAttack(WorldSettlementFC worldSettlement, FCEvent temp)
        {
            if (worldSettlement?.MilitaryComp is null)
            {
                LogUtil.Warning($"SetupAttack called on {worldSettlement?.Name} with no MilitaryComp. Aborting.");
                return;
            }

            if (worldSettlement.Map is null)
            {
                LogUtil.Error($"SetupAttack: {worldSettlement.Name} has no map. Resetting battle state.");
                worldSettlement.MilitaryComp.EndBattle(false, 0, null);
                return;
            }

            if (temp.militaryForceAttacking is null || temp.militaryForceAttackingFaction is null)
            {
                LogUtil.Error($"SetupAttack: Missing attacking force or faction for {worldSettlement.Name}. Resetting battle state.");
                worldSettlement.MilitaryComp.EndBattle(false, 0, null);
                return;
            }

            IncidentParms parms = new IncidentParms
            {
                target = worldSettlement.Map,
                faction = temp.militaryForceAttackingFaction,
                generateFightersOnly = true,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                raidNeverFleeIndividual = true
            };
            parms.points = Math.Max(
                IncidentWorker_Raid.AdjustedRaidPoints(
                    (float)temp.militaryForceAttacking.forceRemaining * 175,
                    PawnsArrivalModeDefOf.EdgeWalkIn, parms.raidStrategy,
                    parms.faction, PawnGroupKindDefOf.Combat,
                    parms.target // new required parameter
                ),
                300f // Minimum floor — ensures at least 1 pawn for any faction
            );
            parms.raidArrivalMode = ResolveRaidArriveMode(parms) ?? PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);

            List<Pawn> attackers = PawnGroupMakerUtility.GeneratePawns(
                IncidentParmsUtility.GetDefaultPawnGroupMakerParms(
                    PawnGroupKindDefOf.Combat, parms, true)).ToList();
            if (!attackers.Any())
            {
                LogUtil.Error("Got no pawns spawning raid from parms " + parms);
                // Queue cleanup as a separate LongEvent so the map's deferred initialization
                // (MapDrawer.RegenerateEverythingNow) completes before we try to dispose it.
                // Calling EndAttack synchronously here would crash in MapDrawer.Dispose()
                // because MapDrawer.sections hasn't been initialized yet.
                LongEventHandler.QueueLongEvent(
                    () => worldSettlement.MilitaryComp.EndAttack(),
                    "EndingAttack", false, null);
                return;
            }

            double attackerEfficiency = temp.militaryForceAttacking.militaryEfficiency;
            foreach (Pawn attacker in attackers)
            {
                MilitaryEfficiencyUtil.ApplyCombatEfficiencyHediff(attacker, attackerEfficiency);
            }

            parms.raidArrivalMode.Worker.Arrive(attackers, parms);

            worldSettlement.MilitaryComp.attackers = attackers;
            worldSettlement.MilitaryComp.attackerForce = temp.militaryForceAttacking;
            worldSettlement.MilitaryComp.defenderForce = temp.militaryForceDefending;
            LordMaker.MakeNewLord(
                parms.faction, new LordJob_HuntColonists(worldSettlement, parms.raidArrivalMode != PawnsArrivalModeDefOf.CenterDrop),
                worldSettlement.Map, attackers);
        }

        private static PawnsArrivalModeDef ResolveRaidArriveMode(IncidentParms parms)
        {
            return
                parms.raidStrategy.arriveModes.Where(testing => testing.Worker.CanUseWith(parms))
                    .TryRandomElementByWeight(
                        x => x.Worker.GetSelectionWeight(parms), out PawnsArrivalModeDef output)
                    ? output
                    : PawnsArrivalModeDefOf.EdgeWalkIn;
        }

        public static void CreateTaxEvent(BillFC bill)
        {
            FactionFC faction = FactionCache.FactionComp;

            FCEvent tmp = MakeEvent(FCEventDefOf.taxColony);
            tmp.tickStarted = Find.TickManager.TicksGame;

            if (bill.settlement != null && faction.settlements.Contains(bill.settlement))
            {
                tmp.source = bill.settlement.Tile; //source location
                tmp.customDescription = "TaxesFromSettlementAreBeingDelivered".Translate(bill.settlement.Name);
            }
            else
            {
                // FIX: Instead of using -1, use the capital location as both source and destination
                // This represents taxes being collected locally at the capital
                PlanetTile fallbackTile = Find.AnyPlayerHomeMap?.Tile ?? PlanetTile.Invalid;
                tmp.source = faction.capitalLocation != PlanetTile.Invalid ? faction.capitalLocation : fallbackTile;
                tmp.customDescription = "TaxesFromSettlementAreBeingDelivered".Translate("Capital".Translate());

                LogUtil.Message($"Tax Event Debug: faction.capitalLocation={faction.capitalLocation}, fallbackTile={fallbackTile}, tmp.source={tmp.source}");
            }

            tmp.location = faction.capitalLocation;

            // Lock in delivery mode at creation time so changing settings mid-transit doesn't alter delivery
            bool canUseShuttle = faction.settlements.FirstOrFallback(s => s.Tile == tmp.source)
                ?.BuildingsComp?.HasBuilding(BuildingFCDefOf.shuttlePort) ?? false;
            tmp.deliveryMode = DeliveryEvent.TaxDeliveryModeForSettlement(canUseShuttle, tmp.source);

            // FIX: Handle case where source equals destination (local delivery)
            if (tmp.source == tmp.location)
            {
                // Local delivery - very short time
                tmp.timeTillTrigger = Find.TickManager.TicksGame + GenDate.TicksPerHour; // 1 hour
            }
            else
            {
                int travelTime = TravelUtil.ReturnTicksToArrive(tmp.source, tmp.location);
                tmp.timeTillTrigger = Find.TickManager.TicksGame + travelTime;
                LogUtil.Message($"Tax Event Travel Debug: source={tmp.source}, destination={tmp.location}, travelTime={travelTime} ticks ({travelTime / GenDate.TicksPerDay:F1} days)");
            }

            tmp.hasCustomDescription = true;
            //add tithe
            tmp.goods = bill.taxes.itemTithes;

            if (bill.taxes.silverAmount > 0) //if getting paid, add silver to tithe
            {
                //add to tithe
                //tmp.goods.Add()
                int silverTotal = (int)bill.taxes.silverAmount;
                while (silverTotal > 0)
                {
                    Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

                    if (silverTotal > thing.def.stackLimit)
                    {
                        thing.stackCount = thing.def.stackLimit;
                        silverTotal -= thing.def.stackLimit;
                    }
                    else
                    {
                        //if not above stack limit
                        thing.stackCount = silverTotal;
                        silverTotal -= silverTotal;
                    }

                    tmp.goods.Add(thing);
                }
            }
            else if (bill.taxes.silverAmount < 0) //if paying money
            {
                //remove money from colony
                PaymentUtil.PaySilver((int)(-1 * (bill.taxes.silverAmount)), PaymentUtil.Reason_TaxPayment, bill.settlement);
            }


            // add event to queue and remove bill
            try
            {
                if (tmp.goods.Count > 0) //if any silver or tithe in bill create event. else, well, don't
                {
                    faction.AddEvent(tmp);
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"Error in CreateTaxEvent: {e}");
            }
            finally
            {
                faction.Bills.Remove(bill);
            }
        }
    }
}