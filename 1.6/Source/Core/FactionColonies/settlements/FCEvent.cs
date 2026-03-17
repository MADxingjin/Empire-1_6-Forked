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

                //letter


                string settlementString = tempEvent.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");

                if (!settlementString.NullOrEmpty())
                {
                    Find.LetterStack.ReceiveLetter(tempEvent.def.label, $"{tempEvent.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                }
                else
                {
                    Find.LetterStack.ReceiveLetter(tempEvent.def.label, tempEvent.def.desc, LetterDefOf.NeutralEvent);
                }
            }
        }

        public static bool IsValidRandomEvent(FCEventDef cEvent)
        {
            FactionFC tmp = FactionCache.FactionComp;

            if (!cEvent.isRandomEvent) return false;
            if (Find.World.PlayerWealthForStoryteller < cEvent.requiredWealth) return false;

            // Stat range checks
            if (cEvent.minimumHappiness > tmp.averageHappiness || tmp.averageHappiness > cEvent.maximumHappiness) return false;
            if (cEvent.minimumLoyalty > tmp.averageLoyalty || tmp.averageLoyalty > cEvent.maximumLoyalty) return false;
            if (cEvent.minimumUnrest > tmp.averageUnrest || tmp.averageUnrest > cEvent.maximumUnrest) return false;
            if (cEvent.minimumProsperity > tmp.averageProsperity || tmp.averageProsperity > cEvent.maximumProsperity) return false;

            // Settlement count check
            bool noSettlementRequirement = cEvent.rangeSettlementsAffected.min == 0 && cEvent.rangeSettlementsAffected.max == 0;
            if (!noSettlementRequirement && FactionCache.FactionComp.settlements.Count() < cEvent.rangeSettlementsAffected.min) return false;

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

            return true;
        }

        public static FCEventDef ReturnRandomEvent()
        {
            //create new list
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

            if (tmpEventList.Count() != 0)
            {
                return tmpEventList.RandomElement();
            }

            return null;
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
            tempEvent.timeTillTrigger = def.timeTillTrigger + Find.TickManager.TicksGame;
            return tempEvent;
        }

        public static FCEvent MakeRandomEvent(FCEventDef def, List<WorldSettlementFC> SettlementTraitLocations)
        {
            if (def is null) return null;

            FactionFC worldcomp = FactionCache.FactionComp;

            FCEvent tempEvent = new FCEvent(true)
            {
                def = def,
                tickStarted = Find.TickManager.TicksGame,
                timeTillTrigger = def.timeTillTrigger + Find.TickManager.TicksGame,
                settlementTraitLocations = new List<WorldSettlementFC>()
            };

            try
            {
                //if affects specific settlement(s) then get settlements.
                if (tempEvent.def.rangeSettlementsAffected.max != 0)
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


                    if (SettlementTraitLocations == null || SettlementTraitLocations.Count == 0)
                    {
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
                            if (tempEvent.def.requiredResource != null)
                            {
                                ResourceFC res = settlement.GetResource(tempEvent.def.requiredResource);
                                if (res != null && res.InstantaneousProduction > 0)
                                {
                                    // Settlements that produce more of a resource should have a higher weight
                                    for (int i = 0; i < res.InstantaneousProduction; i++)
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
                    }
                    else
                    {
                        tempEvent.settlementTraitLocations.AddRange(SettlementTraitLocations);
                    }

                    // If no valid settlements were chosen, then return early instead of firing the event
                    if (tempEvent.settlementTraitLocations.Count == 0)
                    {
                        LogUtil.Warning($"Random event '{def.defName}' found no valid settlements"
                            + (def.requiredResource != null ? $" (requires {def.requiredResource.defName} production)" : ""));
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
                                //Settle new colony event
                                faction.AddExperienceToFactionLevel(10f);

                                ColonyUtil.CreatePlayerColonySettlement(evt.location, evt.settlementToCreate);

                                faction.settlementCaravansList.Remove(evt.location);
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
                                tempEvent = MakeRandomEvent(evt.def.followingEvent2,
                                    evt.settlementTraitLocations);
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

                        string settlementString = tempEvent.settlementTraitLocations.Join((worldsettlement) => $" {worldsettlement.Name}", "\n");

                        if (!settlementString.NullOrEmpty())
                        {
                            Find.LetterStack.ReceiveLetter(tempEvent.def.label, $"{tempEvent.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                        }
                        else
                        {
                            Find.LetterStack.ReceiveLetter(tempEvent.def.label, tempEvent.def.desc,
                                LetterDefOf.NeutralEvent);
                        }
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
                worldSettlement.MilitaryComp.EndAttack();
                return;
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
            if (tmp.goods.Count > 0) //if any silver or tithe in bill create event. else, well, don't
            {
                faction.AddEvent(tmp);
            }

            faction.Bills.Remove(bill);
        }
    }
    public class FCEvent : IExposable, ILoadReferenceable
    {
        public FCEventDef def = new FCEventDef();
        public PlanetTile location = -1;
        public int timeTillTrigger = -1;
        public int tickStarted = -1;
        public int loadID = -1;
        public PlanetTile source = -1;
        public bool hasDestination;
        public int buildingSlot = -1;
        public BuildingFCDef building;
        public List<WorldSettlementFC> settlementTraitLocations = new List<WorldSettlementFC>();
        public List<Thing> goods = new List<Thing>();
        public bool hasCustomDescription;
        public string customDescription = "";

        //Delivery things
        public Message msg = null;
        public Letter let = null;
        public bool isDelayed = false;

        //Military Force stuff
        public militaryForce militaryForceAttacking;
        public Faction militaryForceAttackingFaction;
        public militaryForce militaryForceDefending;
        public Faction militaryForceDefendingFaction;
        public WorldObject settlementFCDefending;
        /// <summary>
        /// If the defending force was provided by an external <see cref="IAutoDefender"/> (not an Empire settlement),
        /// this references the defender's world object so it can be notified on battle completion.
        /// </summary>
        public WorldObject externalDefenderSource;

        public WorldSettlementDef settlementToCreate = null;

        public float Progress
        {
            get
            {
                if (tickStarted < 0 || timeTillTrigger <= tickStarted) return 1f;
                int now = Find.TickManager.TicksGame;
                if (now >= timeTillTrigger) return 1f;
                return (float)(now - tickStarted) / (timeTillTrigger - tickStarted);
            }
        }

        public FCEvent()
        {
            //Constructor
        }

        public FCEvent(bool New)
        {
            loadID = FactionCache.FactionComp.GetNextEventID();
        }
        
        /// <summary>
        /// Defines parameters of event with custom description
        /// </summary>
        /// <param name="f">FactionFC object</param>
        /// <param name="mapLocation">Location of the event object</param>
        /// <param name="timeToFinish">Time of event's completion</param>
        public void DefineEvent(FactionFC f, int mapLocation, int timeToFinish) {
            this.hasCustomDescription = true;
            this.tickStarted = Find.TickManager.TicksGame;
            this.timeTillTrigger = Find.TickManager.TicksGame + timeToFinish;
            this.location = mapLocation;
            f.AddEvent(this);
        }

        public void ExposeData()
        {
            //Ref
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref location, "location");
            Scribe_Values.Look(ref timeTillTrigger, "timeTillTrigger");
            Scribe_Values.Look(ref tickStarted, "tickStarted", -1);
            Scribe_Values.Look(ref source, "source");
            Scribe_Values.Look(ref hasDestination, "hasDestination");
            Scribe_Collections.Look(ref settlementTraitLocations, "settlementTraitLocations", LookMode.Reference);
            Scribe_Collections.Look(ref goods, "goods", LookMode.Deep);
            Scribe_Values.Look(ref loadID, "loadID");

            Scribe_Values.Look(ref buildingSlot, "buildingSlot");

            Scribe_Defs.Look(ref building, "building");


            Scribe_Values.Look(ref hasCustomDescription, "hasCustomDescription");
            Scribe_Values.Look(ref customDescription, "customDescription");

            Scribe_Deep.Look(ref msg, "msg");
            Scribe_Deep.Look(ref let, "let");
            Scribe_Values.Look(ref isDelayed, "isDelayed", false);

            //Military stuff
            Scribe_Deep.Look(ref militaryForceAttacking, "militaryForceAttacking");
            Scribe_References.Look(ref militaryForceAttackingFaction, "militaryForceAttackingFaction");
            Scribe_Deep.Look(ref militaryForceDefending, "militaryForceDefending");
            Scribe_References.Look(ref militaryForceDefendingFaction, "militaryForceDefendingFaction");
            Scribe_References.Look(ref settlementFCDefending, "SettlementFCDefending");
            Scribe_References.Look(ref externalDefenderSource, "externalDefenderSource");

            Scribe_Defs.Look(ref settlementToCreate, "settlementToCreate");
        }

        public string GetUniqueLoadID()
        {
            return "FCEvent_" + loadID;
        }

        public void RunAction()
        {
            try
            {
                def?.GetModExtension<FCEventHandlerExtension>()?.OnEventTriggered(this);
            }
            catch (Exception e)
            {
                LogUtil.Error($"FCEvent.RunAction: OnEventTriggered threw for '{def?.defName ?? "NULL"}': {e}");
            }
        }
    }


    public class FCEventDef : Def
    {
        public int timeTillTrigger = -1;
        public string desc;
        public FCEventCategoryDef category;

        //Random Event Information
        public bool isRandomEvent = false;
        public bool activateAtStart;
        public int requiredWealth = 0;
        public IntRange rangeSettlementsAffected = new IntRange(0, 0);
        public bool settlementsCarryOver = true;
        public bool useProximity = true;
        public float proximityFalloff = 20f;
        public int weight = 0;
        public int minimumHappiness = 0;
        public int maximumHappiness = 100;
        public int minimumLoyalty = 0;
        public int maximumLoyalty = 100;
        public int minimumUnrest = 0;
        public int maximumUnrest = 100;
        public int minimumProsperity = 0;
        public int maximumProsperity = 100;
        public ResourceTypeDef requiredResource;
        public List<FCEventDef> incompatibleEvents = new List<FCEventDef>();

        //Options
        public List<FCOptionDef> options = new List<FCOptionDef>();
        public string optionDescription = "";

        //Event chain
        public bool eventFollows = false;
        public FCEventDef followingEvent = null;
        public FCEventDef followingEvent2 = null;
        public bool splitEventFollows = false;
        public int splitEventChance = 50;

        //Rewards
        public List<ThingDef> loot = new List<ThingDef>();
        public int randomThingValue = 0;
        public ResourceEventRewardDef randomThingRewardDef;
        public int prosperityLost = 0;
        public List<string> applicableBiomes = new List<string>();

        //Stat modifiers during event
        public List<FCStatModifier> statModifiers = new List<FCStatModifier>();

        public bool isMilitaryEvent = false;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string err in base.ConfigErrors())
                yield return err;
            foreach (string err in FCStatModifier.ConfigErrors(statModifiers, defName))
                yield return err;
        }
    }

    [DefOf]
    public class FCEventDefOf
    {
        //List Events here - loads events at start
        public static FCEventDef Null;
        public static FCEventDef settleNewColony;
        public static FCEventDef taxColony;
        public static FCEventDef constructBuilding;
        public static FCEventDef enactSettlementPolicy;
        public static FCEventDef enactFactionPolicy;
        public static FCEventDef upgradeSettlement;
        public static FCEventDef raidEnemySettlement;
        public static FCEventDef enslaveEnemySettlement;
        public static FCEventDef captureEnemySettlement;
        public static FCEventDef cooldownMilitary;
        public static FCEventDef settlementBeingAttacked;
        public static FCEventDef deliveryArrival;

        static FCEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCEventDefOf));
        }
    }
}