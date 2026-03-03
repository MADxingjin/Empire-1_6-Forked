using FactionColonies.util;
using LudeonTK;
using RimWorld;
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
                            TravelUtil.sendPrisoner(pawn, settlement);

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
                faction.factionTraits[i] = new FCPolicy(FCPolicyDefOf.empty);

            faction.policies.Clear();

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


            util.checkMilitaryUtilForErrors();
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
                        if (evtDef.activateAtStart == false)
                        {
                            FCEventMaker.MakeRandomEvent(evtDef, null);
                            FactionCache.FactionComp.addEvent(evt);
                        }

                        //letter code
                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");

                        if (!settlementString.NullOrEmpty()) Find.LetterStack.ReceiveLetter("Random Event", $"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
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
                    LogUtil.MessageForce($"Debug - Attack Player Settlement - {settlement.Name}");
                    Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
                    MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemyFaction, true), settlement, enemyFaction);
                }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
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
                        worldcomp.returnSettlementByLocation(evt.location).Name,
                        DebugMenuOptionMode.Action, delegate
                        {
                            //when event is selected, select defending force to replace it with

                            List<DebugMenuOption> list2 = new List<DebugMenuOption>();
                            foreach (WorldSettlementFC settlement in worldcomp.settlements)
                            {
                                if (settlement.MilitaryComp != null && settlement.MilitaryComp.isMilitaryValid() && settlement.Name != evt.settlementFCDefending.Name)
                                {
                                    list2.Add(new DebugMenuOption(
                                        settlement.Name + " - " + settlement.settlementMilitaryLevel + " - Busy: " +
                                        settlement.MilitaryComp.isMilitaryBusySilent(), DebugMenuOptionMode.Action, delegate
                                        {
                                            if (settlement.MilitaryComp.isMilitaryBusy() == false)
                                            {
                                                LogUtil.MessageForce($"Debug - Change Player Settlement - {evt.militaryForceDefending.homeSettlement.Name} to {settlement.Name}");
                                                MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
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
                    settlement.upgradeSettlement(times);
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
                settlement.MilitaryComp?.returnMilitary(false);
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
                        settlement.MilitaryComp.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);

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
            faction.addExperienceToFactionLevel(faction.factionXPGoal);
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
                LogUtil.MessageForce($"[{s.Name}] Lv{s.settlementLevel} | Happy:{s.happiness:F0} Loyal:{s.loyalty:F0} Unrest:{s.unrest:F0} Prosper:{s.prosperity:F0} | Workers:{s.workers}/{s.workersMax} Prisoners:{s.prisonerList.Count} Traits:{s.Traits.Count}");
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

        [DebugAction("Empire", "Remove Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void RemovePlayerSettlement()
        {
            WithSettlementChoice(settlement =>
            {
                LogUtil.MessageForce($"Debug - Remove Player Settlement - {settlement.Name}");
                ColonyUtil.removePlayerSettlement(settlement);
            });
        }

        [DebugAction("Empire", "Add Settlement Trait", allowedGameStates = AllowedGameStates.Playing)]
        private static void AddSettlementTrait()
        {
            WithSettlementChoice(settlement =>
            {
                List<DebugMenuOption> list = new List<DebugMenuOption>();
                foreach (FCTraitEffectDef trait in DefDatabase<FCTraitEffectDef>.AllDefsListForReading)
                {
                    if (settlement.Traits.Contains(trait)) continue;
                    FCTraitEffectDef localTrait = trait;
                    list.Add(new DebugMenuOption(localTrait.defName, DebugMenuOptionMode.Action, () =>
                    {
                        settlement.addTrait(localTrait);
                        LogUtil.MessageForce($"Debug - Added trait {localTrait.defName} to {settlement.Name}");
                    }));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            });
        }

        [DebugAction("Empire", "Remove Settlement Trait", allowedGameStates = AllowedGameStates.Playing)]
        private static void RemoveSettlementTrait()
        {
            WithSettlementChoice(settlement =>
            {
                List<DebugMenuOption> list = new List<DebugMenuOption>();
                foreach (FCTraitEffectDef trait in settlement.Traits)
                {
                    FCTraitEffectDef localTrait = trait;
                    list.Add(new DebugMenuOption(localTrait.defName, DebugMenuOptionMode.Action, () =>
                    {
                        settlement.removeTrait(localTrait);
                        LogUtil.MessageForce($"Debug - Removed trait {localTrait.defName} from {settlement.Name}");
                    }));
                }
                Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
            });
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
                LogUtil.MessageForce($"[{s.Name}] MilLv:{s.settlementMilitaryLevel} Busy:{comp.isMilitaryBusySilent()} | {squadInfo}");
            }
        }

        [DebugAction("Empire", "Force Return Settlement Military", allowedGameStates = AllowedGameStates.Playing)]
        private static void ForceReturnSettlementMilitary()
        {
            WithSettlementChoice(settlement =>
            {
                if (settlement.MilitaryComp != null)
                {
                    settlement.MilitaryComp.returnMilitary(true);
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
            FactionCache.FactionComp.militaryCustomizationUtil.checkMilitaryUtilForErrors();
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
            LogUtil.MessageForce($"Policies:{f.policies.Count} | Traits:{f.Traits.Count} | ResearchPool:{f.researchPointPool:F0}");
            if (f.Traits.Any())
            {
                LogUtil.MessageForce($"Trait list: {f.Traits.Select(t => t.defName).ToCommaList()}");
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
                    faction.addExperienceToFactionLevel(localAmount);
                    LogUtil.MessageForce($"Debug - Added {localAmount} XP (now {faction.factionXPCurrent:F0}/{faction.factionXPGoal:F0})");
                }));
            }
            float remaining = faction.factionXPGoal - faction.factionXPCurrent;
            list.Add(new DebugMenuOption($"+{remaining:F0} XP (to next level)", DebugMenuOptionMode.Action, () =>
            {
                faction.addExperienceToFactionLevel(remaining);
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
