using FactionColonies.util;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;

namespace FactionColonies
{
    public static class DebugUtil
    {

        [DebugAction("Empire", "View Events and ticks till", allowedGameStates = AllowedGameStates.Playing)]
        private static void ViewEventsAndLog()
        {
            FactionCache.FactionComp.events.ForEach(delegate (FCEvent e)
            {
                LogUtil.MessageForce(e.def.defName + " with cooldown: " + (e.timeTillTrigger - Find.TickManager.TicksGame));
            });
        }

        [DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
        private static void IncrementTimeFiveDays()
        {
            LogUtil.MessageForce("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
        }

        [DebugAction("Empire", "Increment Time 1 Year", allowedGameStates = AllowedGameStates.Playing)]
        private static void IncrementTimeOneYear()
        {
            LogUtil.MessageForce("Debug - Increment Time 1 Year");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + GenDate.TicksPerYear);
        }

        [DebugAction("Empire", "Kill & Regen Leader", allowedGameStates = AllowedGameStates.Playing)]
        private static void DebugKillAndRegenLeader()
        {
            Faction faction = FactionCache.PlayerColonyFaction;
            if (faction == null)
            {
                LogUtil.MessageForce("No Empire faction found.");
                return;
            }
            Pawn oldLeader = faction.leader;
            if (oldLeader != null)
            {
                LogUtil.MessageForce($"Killing leader: {oldLeader.Name} ({oldLeader.ThingID}), " +
                                     $"title: {faction.LeaderTitle}, " +
                                     $"ideo: {oldLeader.Ideo?.name ?? "none"}");
                oldLeader.Kill(null);
            }
            else
            {
                LogUtil.MessageForce("No current leader. Generating new one.");
            }
            ColonyUtil.CreatePlayerFactionLeader(faction);
            if (faction.leader != null)
            {
                LogUtil.MessageForce($"New leader: {faction.leader.Name} ({faction.leader.ThingID}), " +
                                     $"title: {faction.LeaderTitle}, " +
                                     $"pawnKind: {faction.leader.kindDef?.defName ?? "null"}, " +
                                     $"ideo: {faction.leader.Ideo?.name ?? "none"}");
            }
        }

        [DebugAction("Empire", "Print Races", allowedGameStates = AllowedGameStates.Playing)]
        private static void PrintRaces()
        {
            FactionCache.PlayerColonyFaction.def.pawnGroupMakers.ForEach(maker =>
            {
                LogUtil.MessageForce("Traders: " + maker.traders.Count);
                foreach (PawnGenOption option in maker.options)
                {
                    LogUtil.MessageForce("Race: " + option.kind.race.defName + ", " + option.kind.defName + ", " +
                                option.kind.isFighter + ", " + option.kind.trader + " for " + maker.kindDef);
                }
            });
        }

        [DebugAction("Empire", "Send Pawn To Settlement", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SendPawnToSettlement()
        {
            List<Pawn> selected = Find.Selector.SelectedPawns;
            if (!selected.Any())
            {
                Messages.Message("No prisoner selected!", MessageTypeDefOf.RejectInput);
                return;
            }
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                list.Add(new DebugMenuOption(
                    $"{settlement.Name} - Level: {settlement.settlementLevel} - Prisoners: {settlement.prisonerList.Count()}",
                    DebugMenuOptionMode.Action, delegate
                    {
                        foreach (Pawn pawn in selected)
                        {
                            TravelUtil.SendPrisoner(pawn, settlement);

                            foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map =>
                                map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>())
                            {
                                if (!Enumerable.Any(bed.OwnersForReading, found => found == pawn)) continue;
                                bed.ForPrisoners = false;
                                bed.ForPrisoners = true;
                            }
                        }
                    }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Clear faction traits and policies", allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearFactionTraitsAndPolicies()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            for (int i = 0; i < faction.factionTraits.Count; i++)
            {
                if (faction.factionTraits[i]?.behavior != null)
                {
                    try { faction.factionTraits[i].behavior.OnRemoved(faction); }
                    catch (Exception e) { LogUtil.Error($"FCPolicyBehavior.OnRemoved error: {e}"); }
                }
                faction.factionTraits[i] = new FCPolicy(FCPolicyDefOf.empty);
            }

            faction.RemoveAllPolicies(faction.policies);
            faction.RebuildBehaviorCache();

            LogUtil.Message("Cleared faction traits and policies.");
        }

        [DebugAction("Empire", "Reset All Military Squad Assignments", allowedGameStates = AllowedGameStates.Playing)]
        private static void ResetAllMilitarySquads()
        {
            LogUtil.MessageForce("Debug - Reset All Military Squad Assignments");
            MilitaryCustomizationUtil util = FactionCache.FactionComp.militaryCustomizationUtil;
            var allMercs = util.AllMercenaries.ToList();
            for (int i = allMercs.Count - 1; i >= 0; i--)
            {
                if (allMercs[i].squad.hasLord)
                {
                    allMercs[i].squad.map.lordManager.RemoveLord(allMercs[i].squad.lord);
                }

                allMercs[i].pawn.Destroy();
                allMercs[i].squad.mercenaries.Remove(allMercs[i]);
            }

            for (int k = util.mercenarySquads.Count() - 1; k >= 0; k--)
            {
                if (util.mercenarySquads[k].settlement.MilitaryComp != null)
                    util.mercenarySquads[k].settlement.MilitaryComp.militarySquad = null;
                util.mercenarySquads.RemoveAt(k);
            }


            util.CheckMilitaryUtilForErrors();
        }


        [DebugAction("Empire", "Make Random Event", allowedGameStates = AllowedGameStates.Playing)]
        private static void MakeRandomEvent()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEventDef evtDef in DefDatabase<FCEventDef>.AllDefsListForReading)
            {
                if (evtDef.isRandomEvent)
                    list.Add(new DebugMenuOption(evtDef.label, DebugMenuOptionMode.Action, delegate
                    {
                        LogUtil.MessageForce("Debug - Make Random Event - " + evtDef.label);
                        FCEvent evt = FCEventMaker.MakeRandomEvent(evtDef, null);
                        if (evt == null)
                        {
                            if (!evtDef.activateAtStart)
                                LogUtil.Warning("Debug - Event returned null: " + evtDef.defName);
                            return;
                        }

                        if (!evtDef.activateAtStart)
                        {
                            FactionCache.FactionComp.AddEvent(evt);
                        }

                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");
                        if (!settlementString.NullOrEmpty())
                            Find.LetterStack.ReceiveLetter("Random Event", $"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                        else
                            Find.LetterStack.ReceiveLetter("Random Event", evt.def.desc, LetterDefOf.NeutralEvent);
                    }
                    ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Proc MilitaryTimeDue", allowedGameStates = AllowedGameStates.Playing)]
        private static void ProcMilitaryTimeDue()
        {
            LogUtil.MessageForce("Debug - Proc MilitaryTimeDue");
            FactionCache.FactionComp.militaryTimeDue = Find.TickManager.TicksGame + 1;
        }

        [DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void AttackPlayerSettlement()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                list.Add(new DebugMenuOption(settlement.Name, DebugMenuOptionMode.Action, delegate
                {
                    Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
                    if (enemyFaction == null)
                    {
                        Messages.Message("No enemy faction found.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    List<DebugMenuOption> levelList = new List<DebugMenuOption>();
                    for (int level = 1; level <= 10; level++)
                    {
                        int chosenLevel = level;
                        levelList.Add(new DebugMenuOption($"Level {chosenLevel}", DebugMenuOptionMode.Action, delegate
                        {
                            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(enemyFaction.def.techLevel, out double _, out double efficiency);
                            militaryForce attackingForce = new militaryForce(chosenLevel, efficiency, null, enemyFaction);
                            LogUtil.MessageForce($"Debug - Attack Player Settlement - {settlement.Name} (level {chosenLevel}, efficiency {efficiency})");
                            MilitaryUtilFC.AttackPlayerSettlement(attackingForce, settlement, enemyFaction);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(levelList));
                }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Instant Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void InstantAttackPlayerSettlement()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                list.Add(new DebugMenuOption(settlement.Name, DebugMenuOptionMode.Action, delegate
                {
                    Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
                    if (enemyFaction == null)
                    {
                        Messages.Message("No enemy faction found.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    List<DebugMenuOption> levelList = new List<DebugMenuOption>();
                    for (int level = 1; level <= 10; level++)
                    {
                        int chosenLevel = level;
                        levelList.Add(new DebugMenuOption($"Level {chosenLevel}", DebugMenuOptionMode.Action, delegate
                        {
                            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(enemyFaction.def.techLevel, out double _, out double efficiency);
                            militaryForce attackingForce = new militaryForce(chosenLevel, efficiency, null, enemyFaction);
                            LogUtil.MessageForce($"Debug - Instant Attack Player Settlement - {settlement.Name} (level {chosenLevel}, efficiency {efficiency})");
                            MilitaryUtilFC.AttackPlayerSettlement(attackingForce, settlement, enemyFaction);

                            FCEvent attackEvt = FactionCache.FactionComp.events.LastOrDefault(e => e.def == FCEventDefOf.settlementBeingAttacked);
                            if (attackEvt != null)
                            {
                                attackEvt.timeTillTrigger = Find.TickManager.TicksGame + 1;
                            }
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(levelList));
                }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Force Attack + Event Same Tick", allowedGameStates = AllowedGameStates.Playing)]
        private static void ForceAttackAndEventSameTick()
        {
            FactionFC faction = FactionCache.FactionComp;
            FCEvent attackEvt = faction.events.FirstOrDefault(e => e.def == FCEventDefOf.settlementBeingAttacked);
            if (attackEvt == null)
            {
                LogUtil.MessageForce("Debug - No pending settlementBeingAttacked event. Use 'Attack Player Settlement' first.");
                return;
            }

            if (FCSettings.disableRandomEvents)
            {
                LogUtil.MessageForce("Debug - Warning: random events are disabled in settings. Random event will not fire.");
            }

            int nextDayBoundary = ((Find.TickManager.TicksGame / GenDate.TicksPerDay) + 1) * GenDate.TicksPerDay;
            attackEvt.timeTillTrigger = nextDayBoundary;
            faction.randomEventLastAdded = FCSettings.maxDaysTillRandomEvent + 1;
            Find.TickManager.DebugSetTicksGame(nextDayBoundary - 1);
            LogUtil.MessageForce($"Debug - Attack timer and random event aligned to tick {nextDayBoundary}. Unpause to trigger both on the same tick.");
        }

        [DebugAction("Empire", "Change Settlement Defending Force", allowedGameStates = AllowedGameStates.Playing)]
        private static void ChangeAttackPlayerSettlementMilitaryForce()
        {
            FactionFC worldcomp = FactionCache.FactionComp;
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEvent evt in worldcomp.events)
            {
                if (evt.def == FCEventDefOf.settlementBeingAttacked)
                {
                    list.Add(new DebugMenuOption(
                        worldcomp.ReturnSettlementByLocation(evt.location)?.Name ?? "Unknown",
                        DebugMenuOptionMode.Action, delegate
                        {
                            //when event is selected, select defending force to replace it with

                            List<DebugMenuOption> list2 = new List<DebugMenuOption>();
                            foreach (WorldSettlementFC settlement in worldcomp.settlements)
                            {
                                if (settlement.MilitaryComp != null && settlement.MilitaryComp.IsMilitaryValid() && settlement.Name != evt.settlementFCDefending?.Label)
                                {
                                    list2.Add(new DebugMenuOption(
                                        settlement.Name + " - " + settlement.settlementMilitaryLevel + " - Busy: " +
                                        settlement.MilitaryComp.IsMilitaryBusySilent(), DebugMenuOptionMode.Action, delegate
                                        {
                                            if (settlement.MilitaryComp.IsMilitaryBusy() == false)
                                            {
                                                LogUtil.MessageForce($"Debug - Change Player Settlement - {evt.militaryForceDefending?.homeSettlement?.Name ?? "Unknown"} to {settlement.Name}");
                                                MilitaryUtilFC.ChangeDefendingMilitaryForce(evt, settlement);
                                            }
                                        }
                                    ));
                                }
                            }

                            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
                        }
                    ));
                }
            }

            if (list.Any())
            {
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            }
        }

        [DebugAction("Empire", "Upgrade Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx1() => UpgradePlayerSettlement();

        [DebugAction("Empire", "Upgrade Player Settlement x5", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx5() => UpgradePlayerSettlement(5);

        private static void UpgradePlayerSettlement(int times = 1)
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                list.Add(new DebugMenuOption(settlement.Name, DebugMenuOptionMode.Action, delegate
                {
                    if (times > 0)
                    {
                        LogUtil.MessageForce("Debug - Upgrade Player Settlement x" + times + "- " + settlement.Name);
                    }
                    else
                    {
                        LogUtil.MessageForce("Debug - Downgrade Player Settlement x" + times + "- " + settlement.Name);
                    }
                    settlement.UpgradeSettlement(times);
                }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Flag Road Queue Update", allowedGameStates = AllowedGameStates.Playing)]
        private static void FlagRoadQueueUpdate()
        {
            LogUtil.MessageForce("Debug - Flag Road Queue Update");
            FactionCache.FactionComp.roadBuilder.FlagUpdateRoadQueues();
        }

        [DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void DelevelPlayerSettlement() => UpgradePlayerSettlement(-1);

        [DebugAction("Empire", "Reset All Military Squads", allowedGameStates = AllowedGameStates.Playing)]
        private static void ResetMilitarySquads()
        {
            FactionCache.FactionComp.militaryCustomizationUtil.mercenarySquads =
                new List<MercenarySquadFC>();
            LogUtil.MessageForce("Debug - Reset All Military Squads");
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                settlement.MilitaryComp?.ReturnMilitary(false);
            }
        }

        [DebugAction("Empire", "Clear Old Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearOldBills()
        {
            FactionCache.FactionComp.OldBills = new List<BillFC>();
        }

        [DebugAction("Empire", "Clear All Events", allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearAllEvents()
        {
            FactionCache.FactionComp.events = new List<FCEvent>();
        }

        [DebugAction("Empire", "Clear All Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearAllBills()
        {
            FactionCache.FactionComp.Bills = new List<BillFC>();
        }

        [DebugAction("Empire", "Place 500 Silver", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PlaceSilverFC() => SilverPlacer(500);

        [DebugAction("Empire", "Place 50000 Silver", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PlaceALotOfSilverFC() => SilverPlacer(50000);

        private static void SilverPlacer(int amount)
        {
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            GenPlace.TryPlaceThing(silver, UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
        }

        private static void CallInAlliedForcesSelect()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                if (settlement.MilitaryComp?.militarySquad != null)
                {
                    list.Add(new DebugMenuOption(settlement.Name, DebugMenuOptionMode.Action, delegate
                    {
                        IncidentParms parms = new IncidentParms();
                        parms.target = Find.CurrentMap;
                        parms.faction = FactionCache.PlayerColonyFaction;
                        parms.podOpenDelay = 140;
                        parms.points = 999;
                        parms.raidArrivalModeForQuickMilitaryAid = true;
                        parms.raidNeverFleeIndividual = true;
                        parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                        parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;

                        settlement.MilitaryComp.militarySquad.CheckInitialization();
                        settlement.MilitaryComp.militarySquad.UpdateSquadStats(settlement.settlementMilitaryLevel);

                        DebugTools.curTool = new DebugTool("Select Drop Position", delegate
                        {
                            IntVec3 dropPosition = UI.MouseCell();
                            parms.spawnCenter = dropPosition;

                            settlement.MilitaryComp.militarySquad.isDeployed = true;
                            settlement.MilitaryComp.militarySquad.orderLocation = dropPosition;
                            settlement.MilitaryComp.militarySquad.timeDeployed = Find.TickManager.TicksGame;

                            var debugEquippedPawns = settlement.MilitaryComp.militarySquad.AllEquippedMercenaryPawns.ToList();
                            PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, debugEquippedPawns);
                            debugEquippedPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
                            settlement.MilitaryComp.militarySquad.isDeployed = true;
                            DebugTools.curTool = null;
                        });
                    }));
                }
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }


        [DebugAction("Empire", "Call In Allied Forces", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CallInAlliedForcesDebug() => CallInAlliedForcesSelect();


        [DebugAction("Empire", "Level Up Faction", allowedGameStates = AllowedGameStates.Playing)]
        private static void LevelUpFaction()
        {
            FactionFC faction = FactionCache.FactionComp;
            faction.AddExperienceToFactionLevel(faction.factionXPGoal);
        }

        // ============================
        // Helper
        // ============================

        private static void WithSettlementChoice(Action<WorldSettlementFC> callback)
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in FactionCache.FactionComp.settlements)
            {
                WorldSettlementFC local = settlement;
                list.Add(new DebugMenuOption(
                    $"{local.Name} (Lv{local.settlementLevel})",
                    DebugMenuOptionMode.Action, () => callback(local)));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        // ============================
        // Settlement Debug Actions
        // ============================

        [DebugAction("Empire", "Log Settlement Stats", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogSettlementStats()
        {
            foreach (WorldSettlementFC s in FactionCache.FactionComp.settlements)
            {
                LogUtil.MessageForce($"[{s.Name}] Lv{s.settlementLevel} | Happy:{s.happiness:F0} Loyal:{s.loyalty:F0} Unrest:{s.unrest:F0} Prosper:{s.prosperity:F0} | Workers:{s.workers}/{s.workersMax} Prisoners:{s.prisonerList.Count}");
            }
        }

        [DebugAction("Empire", "Set Settlement Stat", allowedGameStates = AllowedGameStates.Playing)]
        private static void SetSettlementStat()
        {
            WithSettlementChoice(settlement =>
            {
                List<DebugMenuOption> stats = new List<DebugMenuOption>();
                string[] statNames = { "happiness", "loyalty", "unrest", "prosperity" };
                foreach (string stat in statNames)
                {
                    string localStat = stat;
                    stats.Add(new DebugMenuOption(localStat, DebugMenuOptionMode.Action, () =>
                    {
                        List<DebugMenuOption> values = new List<DebugMenuOption>();
                        foreach (int val in new[] { 0, 25, 50, 75, 100 })
                        {
                            int localVal = val;
                            values.Add(new DebugMenuOption(localVal.ToString(), DebugMenuOptionMode.Action, () =>
                            {
                                switch (localStat)
                                {
                                    case "happiness": settlement.happiness = localVal; break;
                                    case "loyalty": settlement.loyalty = localVal; break;
                                    case "unrest": settlement.unrest = localVal; break;
                                    case "prosperity": settlement.prosperity = localVal; break;
                                }
                                LogUtil.MessageForce($"Debug - Set {settlement.Name} {localStat} = {localVal}");
                            }));
                        }
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(values));
                    }));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(stats));
            });
        }

        [DebugAction("Empire", "Create Settlement (Instant)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
        private static void CreateSettlementInstant()
        {
            PlanetTile tile = GenWorld.MouseTile();
            if (tile == -1)
            {
                Messages.Message("Invalid tile selected.", MessageTypeDefOf.RejectInput);
                return;
            }

            List<WorldSettlementDef> defs = DefDatabase<WorldSettlementDef>.AllDefsListForReading;
            if (defs.Count == 1)
            {
                TryCreateInstantSettlement(tile, defs[0]);
            }
            else
            {
                List<DebugMenuOption> list = new List<DebugMenuOption>();
                foreach (WorldSettlementDef def in defs)
                {
                    WorldSettlementDef localDef = def;
                    list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Action,
                        () => TryCreateInstantSettlement(tile, localDef)));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            }
        }

        private static void TryCreateInstantSettlement(PlanetTile tile, WorldSettlementDef def)
        {
            StringBuilder reason = new StringBuilder();
            if (!WorldTileChecker.IsValidTileForNewSettlement(tile, def, reason))
            {
                Messages.Message($"Cannot settle here: {reason}", MessageTypeDefOf.RejectInput);
                return;
            }
            if (FactionCache.FactionComp.CheckSettlementCaravansList(tile))
            {
                Messages.Message("A settlement caravan is already heading to this tile.", MessageTypeDefOf.RejectInput);
                return;
            }
            LogUtil.MessageForce($"Debug - Create Settlement (Instant) at tile {tile.Tile} with type {def.defName}");
            ColonyUtil.CreatePlayerColonySettlement(tile, def);
        }

        [DebugAction("Empire", "Remove Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void RemovePlayerSettlement()
        {
            WithSettlementChoice(settlement =>
            {
                LogUtil.MessageForce($"Debug - Remove Player Settlement - {settlement.Name}");
                ColonyUtil.RemovePlayerSettlement(settlement);
            });
        }

        [DebugAction("Empire", "Add Stat Modifier", allowedGameStates = AllowedGameStates.Playing)]
        private static void AddStatModifier()
        {
            WithSettlementChoice(settlement =>
            {
                List<DebugMenuOption> list = new List<DebugMenuOption>();
                foreach (FCStatDef stat in DefDatabase<FCStatDef>.AllDefsListForReading)
                {
                    FCStatDef localStat = stat;
                    list.Add(new DebugMenuOption(localStat.defName, DebugMenuOptionMode.Action, () =>
                    {
                        List<DebugMenuOption> values = new List<DebugMenuOption>();
                        double[] options = localStat.aggregation == FCStatAggregation.Additive
                            ? new double[] { -10, -5, -1, 1, 5, 10 }
                            : new double[] { 0.5, 0.75, 1.25, 1.5, 2.0 };
                        foreach (double val in options)
                        {
                            double localVal = val;
                            string label = localStat.aggregation == FCStatAggregation.Additive
                                ? (localVal > 0 ? $"+{localVal}" : $"{localVal}")
                                : $"x{localVal}";
                            values.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, () =>
                            {
                                settlement.AddStatModifiers(
                                    new List<FCStatModifier> { new FCStatModifier { stat = localStat, value = localVal } },
                                    "debug");
                                LogUtil.MessageForce($"Debug - Added stat {localStat.defName} = {localVal} to {settlement.Name}");
                            }));
                        }
                        Find.WindowStack.Add(new Dialog_DebugOptionListLister(values));
                    }));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            });
        }

        [DebugAction("Empire", "Clear Debug Stat Modifiers", allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearDebugStatModifiers()
        {
            WithSettlementChoice(settlement =>
            {
                settlement.RemoveStatModifiersBySource("debug");
                LogUtil.MessageForce($"Debug - Cleared debug stat modifiers from {settlement.Name}");
            });
        }

        [DebugAction("Empire", "Log All Stat Values", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogAllStatValues()
        {
            WithSettlementChoice(settlement =>
            {
                FactionFC faction = FactionCache.FactionComp;
                LogUtil.MessageForce($"--- Stat Values for {settlement.Name} ---");
                int defaultCount = 0;
                foreach (FCStatDef stat in DefDatabase<FCStatDef>.AllDefsListForReading)
                {
                    if (stat.appliesToSettlements)
                    {
                        double final = faction.GetStatValue(stat, settlement);
                        double settlementPart = settlement.GetSettlementStatValue(stat);
                        double factionPart = faction.GetFactionStatValue(stat);
                        if (Math.Abs(final - stat.IdentityValue) < 0.001
                            && Math.Abs(settlementPart - stat.IdentityValue) < 0.001
                            && Math.Abs(factionPart - stat.IdentityValue) < 0.001)
                        {
                            defaultCount++;
                            continue;
                        }
                        string agg = stat.aggregation == FCStatAggregation.Additive ? "Add" : "Mult";
                        LogUtil.MessageForce($"  {stat.defName}: Final={final:F2} | Settlement={settlementPart:F2} | Faction={factionPart:F2} ({agg})");
                    }
                    else
                    {
                        double val = faction.GetFactionStatValue(stat);
                        if (Math.Abs(val - stat.IdentityValue) < 0.001)
                        {
                            defaultCount++;
                            continue;
                        }
                        LogUtil.MessageForce($"  {stat.defName}: {val:F2} (faction-only)");
                    }
                }
                LogUtil.MessageForce($"  ({defaultCount} stats at default value)");
            });
        }

        [DebugAction("Empire", "Log Stat Breakdown", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogStatBreakdown()
        {
            WithSettlementChoice(settlement =>
            {
                List<DebugMenuOption> list = new List<DebugMenuOption>();
                foreach (FCStatDef stat in DefDatabase<FCStatDef>.AllDefsListForReading)
                {
                    FCStatDef localStat = stat;
                    list.Add(new DebugMenuOption(localStat.defName, DebugMenuOptionMode.Action, () =>
                    {
                        FactionFC faction = FactionCache.FactionComp;
                        string agg = localStat.aggregation == FCStatAggregation.Additive ? "Additive" : "Multiplicative";
                        LogUtil.MessageForce($"--- Stat Breakdown: {localStat.defName} ({agg}, default={localStat.IdentityValue}) ---");

                        // Settlement-level modifiers
                        foreach (FCStatModifier mod in settlement.StatModifiers)
                        {
                            if (mod.stat == localStat)
                                LogUtil.MessageForce($"  Settlement modifier: {mod.value:F2}");
                        }

                        // IStatModifierProvider comps
                        foreach (WorldObjectComp comp in settlement.AllComps)
                        {
                            if (comp is IStatModifierProvider provider)
                            {
                                double compVal = provider.GetStatModifier(localStat);
                                if (Math.Abs(compVal - (localStat.aggregation == FCStatAggregation.Additive ? 0 : 1)) > 0.001)
                                    LogUtil.MessageForce($"  Comp ({comp.GetType().Name}): {compVal:F2}");
                            }
                        }
                        LogUtil.MessageForce($"  Settlement partial = {settlement.GetSettlementStatValue(localStat):F2}");

                        // Faction-level (policies + traits)
                        foreach (FCPolicy p in faction.policies)
                        {
                            if (p?.def == null) continue;
                            foreach (FCStatModifier mod in p.def.statModifiers)
                            {
                                if (mod.stat == localStat)
                                    LogUtil.MessageForce($"  Policy ({p.def.defName}): {mod.value:F2}");
                            }
                        }
                        foreach (FCPolicy p in faction.factionTraits)
                        {
                            if (p?.def == null || p.def == FCPolicyDefOf.empty) continue;
                            foreach (FCStatModifier mod in p.def.statModifiers)
                            {
                                if (mod.stat == localStat)
                                    LogUtil.MessageForce($"  Trait ({p.def.defName}): {mod.value:F2}");
                            }
                        }
                        LogUtil.MessageForce($"  Faction partial = {faction.GetFactionStatValue(localStat):F2}");

                        // Behavior contributions
                        foreach (FCPolicyBehavior b in faction.cachedBehaviors)
                        {
                            string desc = b.GetStatDescription(localStat, settlement);
                            if (!desc.NullOrEmpty())
                                LogUtil.MessageForce($"  Behavior ({b.GetType().Name}): {desc.TrimEnd()}");
                        }

                        double final = faction.GetStatValue(localStat, settlement);
                        LogUtil.MessageForce($"  FINAL = {final:F2}");
                    }));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            });
        }

        [DebugAction("Empire", "Log Faction Stats", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogFactionStatValues()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            LogUtil.MessageForce("--- Faction-Level Stat Values ---");
            int defaultCount = 0;
            foreach (FCStatDef stat in DefDatabase<FCStatDef>.AllDefsListForReading)
            {
                double val = faction.GetFactionStatValue(stat);
                if (Math.Abs(val - stat.IdentityValue) < 0.001)
                {
                    defaultCount++;
                    continue;
                }
                string agg = stat.aggregation == FCStatAggregation.Additive ? "Add" : "Mult";
                LogUtil.MessageForce($"  {stat.defName} = {val:F2} ({agg}, default={stat.IdentityValue})");
            }
            LogUtil.MessageForce($"  ({defaultCount} stats at default value)");
        }

        // ============================
        // Military Debug Actions
        // ============================

        [DebugAction("Empire", "Log Military Status", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogMilitaryStatus()
        {
            foreach (WorldSettlementFC s in FactionCache.FactionComp.settlements)
            {
                var comp = s.MilitaryComp;
                if (comp == null)
                {
                    LogUtil.MessageForce($"[{s.Name}] MilitaryComp: null");
                    continue;
                }
                string squadInfo = comp.militarySquad != null
                    ? $"Deployed:{comp.militarySquad.isDeployed} Job:{comp.militaryJob}"
                    : "No squad";
                LogUtil.MessageForce($"[{s.Name}] MilLv:{s.settlementMilitaryLevel} Busy:{comp.IsMilitaryBusySilent()} | {squadInfo}");
            }
        }

        [DebugAction("Empire", "Force Return Settlement Military", allowedGameStates = AllowedGameStates.Playing)]
        private static void ForceReturnSettlementMilitary()
        {
            WithSettlementChoice(settlement =>
            {
                if (settlement.MilitaryComp != null)
                {
                    settlement.MilitaryComp.ReturnMilitary(true);
                    LogUtil.MessageForce($"Debug - Force returned military for {settlement.Name}");
                }
                else
                {
                    LogUtil.MessageForce($"Debug - {settlement.Name} has no MilitaryComp");
                }
            });
        }

        [DebugAction("Empire", "Run Military Error Check", allowedGameStates = AllowedGameStates.Playing)]
        private static void RunMilitaryErrorCheck()
        {
            LogUtil.MessageForce("Debug - Running military error check");
            FactionCache.FactionComp.militaryCustomizationUtil.CheckMilitaryUtilForErrors();
            LogUtil.MessageForce("Debug - Military error check complete");
        }

        // ============================
        // Faction / Economy Debug Actions
        // ============================

        [DebugAction("Empire", "Log Faction Status", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogFactionStatus()
        {
            FactionFC f = FactionCache.FactionComp;
            LogUtil.MessageForce($"Faction Lv{f.factionLevel} | XP:{f.factionXPCurrent:F0}/{f.factionXPGoal:F0}");
            LogUtil.MessageForce($"Settlements:{f.settlements.Count} | Income:{f.income:F0} Upkeep:{f.upkeep:F0} Profit:{f.profit:F0}");
            LogUtil.MessageForce($"TaxDue:{f.taxTimeDue - Find.TickManager.TicksGame} ticks | MilDue:{f.militaryTimeDue - Find.TickManager.TicksGame} ticks");
            LogUtil.MessageForce($"AvgHappy:{f.averageHappiness:F0} AvgLoyal:{f.averageLoyalty:F0} AvgUnrest:{f.averageUnrest:F0} AvgProsper:{f.averageProsperity:F0}");
            LogUtil.MessageForce($"Policies:{f.policies.Count} | Traits:{f.factionTraits.Count} | ResearchPool:{f.researchPointPool:F0}");
            if (f.factionTraits.Any())
            {
                LogUtil.MessageForce($"Trait list: {f.factionTraits.Select(t => t.def?.defName).ToCommaList()}");
            }
            if (f.policies.Any())
            {
                LogUtil.MessageForce($"Policy list: {f.policies.Select(t => t.def?.defName).ToCommaList()}");
            }
        }

        [DebugAction("Empire", "Add Faction XP", allowedGameStates = AllowedGameStates.Playing)]
        private static void AddFactionXP()
        {
            FactionFC faction = FactionCache.FactionComp;
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (int amount in new[] { 100, 500, 1000 })
            {
                int localAmount = amount;
                list.Add(new DebugMenuOption($"+{localAmount} XP", DebugMenuOptionMode.Action, () =>
                {
                    faction.AddExperienceToFactionLevel(localAmount);
                    LogUtil.MessageForce($"Debug - Added {localAmount} XP (now {faction.factionXPCurrent:F0}/{faction.factionXPGoal:F0})");
                }));
            }
            float remaining = faction.factionXPGoal - faction.factionXPCurrent;
            list.Add(new DebugMenuOption($"+{remaining:F0} XP (to next level)", DebugMenuOptionMode.Action, () =>
            {
                faction.AddExperienceToFactionLevel(remaining);
                LogUtil.MessageForce($"Debug - Added {remaining:F0} XP to reach next level");
            }));
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Proc Tax Due", allowedGameStates = AllowedGameStates.Playing)]
        private static void ProcTaxDue()
        {
            LogUtil.MessageForce("Debug - Proc TaxTimeDue");
            FactionCache.FactionComp.taxTimeDue = Find.TickManager.TicksGame + 1;
        }

        [DebugAction("Empire", "Log Resource Production", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogResourceProduction()
        {
            WithSettlementChoice(settlement =>
            {
                LogUtil.MessageForce($"--- Resources for {settlement.Name} ---");
                foreach (ResourceFC r in settlement.Resources)
                {
                    LogUtil.MessageForce($"  {r.def.defName}: Workers:{r.assignedWorkers} Base:{r.productionBase:F2} Mult:{r.productionMult:F2} Production:{r.production:F2} Income:{r.actualIncome:F2}");
                }
            });
        }

        // ============================
        // Events Debug Actions
        // ============================

        [DebugAction("Empire", "Force Trigger Event", allowedGameStates = AllowedGameStates.Playing)]
        private static void ForceTriggerEvent()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEvent evt in FactionCache.FactionComp.events)
            {
                FCEvent localEvt = evt;
                int ticksLeft = localEvt.timeTillTrigger - Find.TickManager.TicksGame;
                list.Add(new DebugMenuOption(
                    $"{localEvt.def.defName} (in {ticksLeft} ticks)",
                    DebugMenuOptionMode.Action, () =>
                    {
                        localEvt.timeTillTrigger = Find.TickManager.TicksGame + 1;
                        LogUtil.MessageForce($"Debug - Force triggering {localEvt.def.defName}");
                    }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        // ============================
        // Road Debug Actions
        // ============================

        [DebugAction("Empire", "Build Road Segment Now", allowedGameStates = AllowedGameStates.Playing)]
        private static void BuildRoadSegmentNow()
        {
            var rb = FactionCache.FactionComp.roadBuilder;
            if (rb.roadDef == null)
            {
                LogUtil.MessageForce("Debug - No road research completed yet");
                return;
            }
            if (rb.roadQueue == null)
            {
                LogUtil.MessageForce("Debug - No road queue exists");
                return;
            }
            rb.roadQueue.nextRoadTick = Find.TickManager.TicksGame;
            rb.roadQueue.ProcessOnePath();
            bool built = rb.roadQueue.BuildRoadSegments();
            LogUtil.MessageForce($"Debug - Build Road Segment Now: {(built ? "segment built" : "no segment to build")}");
        }

        // ============================
        // Policy Debug Actions
        // ============================

        [DebugAction("Empire", "Enact Policy (Debug)", allowedGameStates = AllowedGameStates.Playing)]
        private static void EnactPolicyDebug()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                if (def == FCPolicyDefOf.empty) continue;
                if (def.category != FCPolicyCategory.Core) continue;
                FCPolicyDef local = def;
                string status = faction.policies.Any(p => p.def == local) ? " [ACTIVE]" : "";
                list.Add(new DebugMenuOption($"{local.defName}{status}", DebugMenuOptionMode.Action, () =>
                {
                    var policy = new FCPolicy(local);
                    faction.policies.Add(policy);
                    faction.RebuildBehaviorCache();
                    LogUtil.MessageForce($"Debug - Enacted policy: {local.defName} (behavior: {(policy.behavior != null ? policy.behavior.GetType().Name : "none")})");
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Enact Trait (Debug)", allowedGameStates = AllowedGameStates.Playing)]
        private static void EnactTraitDebug()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            List<DebugMenuOption> traitList = new List<DebugMenuOption>();
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                if (def == FCPolicyDefOf.empty) continue;
                if (def.category != FCPolicyCategory.Trait) continue;
                FCPolicyDef local = def;
                traitList.Add(new DebugMenuOption(local.defName, DebugMenuOptionMode.Action, () =>
                {
                    List<DebugMenuOption> slotList = new List<DebugMenuOption>();
                    for (int i = 0; i < faction.factionTraits.Count; i++)
                    {
                        int slot = i;
                        string current = faction.factionTraits[slot]?.def?.defName ?? "empty";
                        slotList.Add(new DebugMenuOption($"Slot {slot} [{current}]", DebugMenuOptionMode.Action, () =>
                        {
                            if (faction.factionTraits[slot]?.behavior != null)
                            {
                                try { faction.factionTraits[slot].behavior.OnRemoved(faction); }
                                catch (Exception e) { LogUtil.Error($"OnRemoved error: {e}"); }
                            }
                            var trait = new FCPolicy(local);
                            faction.factionTraits[slot] = trait;
                            faction.RebuildBehaviorCache();
                            LogUtil.MessageForce($"Debug - Set trait slot {slot} to: {local.defName}");
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(slotList));
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(traitList));
        }

        [DebugAction("Empire", "Instantly Enact Edict", allowedGameStates = AllowedGameStates.Playing)]
        private static void InstantlyEnactEdict()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCPolicyDef def in DefDatabase<FCPolicyDef>.AllDefsListForReading)
            {
                if (!def.IsEdict) continue;
                FCPolicyDef local = def;
                FCPolicy existing;
                faction.edicts.TryGetValue(local.category, out existing);
                string status = (existing != null && existing.def == local) ? " [ACTIVE]" : "";
                list.Add(new DebugMenuOption($"[{local.category}] {local.LabelCap}{status}", DebugMenuOptionMode.Action, () =>
                {
                    faction.EnactEdict(local);
                    FCPolicy edict;
                    if (faction.edicts.TryGetValue(local.category, out edict))
                    {
                        edict.timeEnacted = Find.TickManager.TicksGame - local.enactDuration;
                        faction.InvalidateFactionStatCache();
                        faction.DirtyFactionProfitCache();
                    }
                    LogUtil.MessageForce($"Debug - Instantly enacted edict: {local.defName}");
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Log Policy Behavior State", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogPolicyBehaviorState()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            LogUtil.MessageForce("=== Policy Behavior State ===");

            foreach (FCPolicy p in faction.policies)
            {
                if (p?.behavior == null)
                {
                    LogUtil.MessageForce($"[Policy] {p?.def?.defName ?? "null"}: no behavior");
                    continue;
                }
                LogBehaviorState("Policy", p);
            }

            for (int i = 0; i < faction.factionTraits.Count; i++)
            {
                FCPolicy p = faction.factionTraits[i];
                if (p?.def == null || p.def == FCPolicyDefOf.empty) continue;
                if (p.behavior == null)
                {
                    LogUtil.MessageForce($"[Trait {i}] {p.def.defName}: no behavior");
                    continue;
                }
                LogBehaviorState($"Trait {i}", p);
            }
        }

        private static void LogBehaviorState(string prefix, FCPolicy p)
        {
            string behaviorType = p.behavior.GetType().Name;

            if (p.behavior is FCPolicyBehavior_Militaristic mil)
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): extraSquadCooldown Ready={mil.DebugCooldownReady()} Days={mil.DebugCooldownDays():F1}");
            }
            else if (p.behavior is FCPolicyBehavior_Pacifist pac)
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): diplomatCooldown Ready={pac.DebugCooldownReady()} Days={pac.DebugCooldownDays():F1}");
            }
            else if (p.behavior is FCPolicyBehavior_Feudal feu)
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): mercenaryCooldown Ready={feu.DebugCooldownReady()} Days={feu.DebugCooldownDays():F1}");
            }
            else if (p.behavior is FCPolicyBehavior_Expansionist exp)
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): feeReduction Ready={exp.DebugCooldownReady()} Days={exp.DebugCooldownDays():F1}");
            }
            else if (p.behavior is FCPolicyBehavior_Egalitarian egal)
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): taxBreaks={egal.DebugTaxBreakCount()} active={egal.DebugActiveTaxBreakCount()}");
            }
            else if (p.behavior is FCPolicyBehavior_Mercantile merc)
            {
                int ticksUntil = merc.DebugNextCaravanTick() - Find.TickManager.TicksGame;
                float daysUntil = ticksUntil / (float)GenDate.TicksPerDay;
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): nextCaravan in {daysUntil:F1} days ({ticksUntil} ticks)");
            }
            else
            {
                LogUtil.MessageForce($"[{prefix}] {p.def.defName} ({behaviorType}): (no inspectable state)");
            }
        }

        [DebugAction("Empire", "Force Policy Cooldowns Ready", allowedGameStates = AllowedGameStates.Playing)]
        private static void ForcePolicyCooldownsReady()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            int count = 0;
            foreach (FCPolicyBehavior b in faction.cachedBehaviors)
            {
                if (b is FCPolicyBehavior_Militaristic mil) { mil.DebugResetCooldown(); count++; }
                else if (b is FCPolicyBehavior_Pacifist pac) { pac.DebugResetCooldown(); count++; }
                else if (b is FCPolicyBehavior_Feudal feu) { feu.DebugResetCooldown(); count++; }
                else if (b is FCPolicyBehavior_Expansionist exp) { exp.DebugResetCooldown(); count++; }
                else if (b is FCPolicyBehavior_Mercantile merc) { merc.DebugResetNextCaravan(); count++; }
            }
            LogUtil.MessageForce($"Debug - Reset {count} policy cooldowns to ready");
        }

        [DebugAction("Empire", "Trigger Policy Hook", allowedGameStates = AllowedGameStates.Playing)]
        private static void TriggerPolicyHook()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            List<DebugMenuOption> hookList = new List<DebugMenuOption>();

            hookList.Add(new DebugMenuOption("OnSettlementCreated", DebugMenuOptionMode.Action, () =>
            {
                WithSettlementChoice(settlement =>
                {
                    faction.ForEachBehavior(b => b.OnSettlementCreated(faction, settlement));
                    LogUtil.MessageForce($"Debug - Triggered OnSettlementCreated on {settlement.Name}");
                });
            }));

            hookList.Add(new DebugMenuOption("OnSettlementRemoved", DebugMenuOptionMode.Action, () =>
            {
                WithSettlementChoice(settlement =>
                {
                    faction.ForEachBehavior(b => b.OnSettlementRemoved(faction, settlement));
                    LogUtil.MessageForce($"Debug - Triggered OnSettlementRemoved on {settlement.Name}");
                });
            }));

            hookList.Add(new DebugMenuOption("OnSquadDeployed", DebugMenuOptionMode.Action, () =>
            {
                WithSettlementChoice(settlement =>
                {
                    faction.ForEachBehavior(b => b.OnSquadDeployed(faction, settlement, false));
                    LogUtil.MessageForce($"Debug - Triggered OnSquadDeployed on {settlement.Name}");
                });
            }));

            hookList.Add(new DebugMenuOption("OnSquadRecalled", DebugMenuOptionMode.Action, () =>
            {
                WithSettlementChoice(settlement =>
                {
                    faction.ForEachBehavior(b => b.OnSquadRecalled(faction, settlement));
                    LogUtil.MessageForce($"Debug - Triggered OnSquadRecalled on {settlement.Name}");
                });
            }));

            hookList.Add(new DebugMenuOption("OnTaxCollected", DebugMenuOptionMode.Action, () =>
            {
                WithSettlementChoice(settlement =>
                {
                    faction.ForEachBehavior(b => b.OnTaxCollected(faction, settlement));
                    LogUtil.MessageForce($"Debug - Triggered OnTaxCollected on {settlement.Name}");
                });
            }));

            hookList.Add(new DebugMenuOption("OnSettlementCostPaid", DebugMenuOptionMode.Action, () =>
            {
                faction.ForEachBehavior(b => b.OnSettlementCostPaid(faction));
                LogUtil.MessageForce("Debug - Triggered OnSettlementCostPaid");
            }));

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(hookList));
        }

        // ============================
        // Road Debug Actions
        // ============================

        [DebugAction("Empire", "Log Road Builder Status", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogRoadBuilderStatus()
        {
            var rb = FactionCache.FactionComp.roadBuilder;
            LogUtil.MessageForce($"Road Builder: Enabled:{rb.roadBuildingEnabled} RoadDef:{rb.roadDef?.defName ?? "null"} DaysBetweenTicks:{rb.daysBetweenTicks}");
            if (rb.roadQueue != null)
            {
                var rq = rb.roadQueue;
                LogUtil.MessageForce($"Road Queue: NextTick:{rq.nextRoadTick - Find.TickManager.TicksGame} ticks | FromTiles:{rq.settlementsFromTiles.Count} ToTiles:{rq.settlementsToTiles.Count} Paths:{rq.roadPaths.Count} NeedsUpdate:{rq.shouldUpdateSettlementsToProcess}");
            }
            else
            {
                LogUtil.MessageForce("Road Queue: null");
            }
        }
    }
}
