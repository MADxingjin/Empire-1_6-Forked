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
            Find.World.GetComponent<FactionFC>().events.ForEach(delegate (FCEvent e)
            {
                LogUtil.MessageForce(e.def.defName + " with cooldown: " + (e.timeTillTrigger - Find.TickManager.TicksGame));
            });
        }

        [DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeFiveDays()
        {
            LogUtil.MessageForce("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
        }

        [DebugAction("Empire", "Increment Time 1 Year", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeOneYear()
        {
            LogUtil.MessageForce("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + GenDate.TicksPerYear);
        }

        [DebugAction("Empire", "Print Races", allowedGameStates = AllowedGameStates.Playing)]
        private static void PrintRaces()
        {
            ColonyUtil.getPlayerColonyFaction().def.pawnGroupMakers.ForEach(maker =>
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
        private static void sendPawnToSettlement()
        {
            List<Pawn> selected = Find.Selector.SelectedPawns;
            if (!selected.Any())
            {
                Messages.Message("No prisoner selected!", MessageTypeDefOf.RejectInput);
                return;
            }
            List<FloatMenuOption> settlementList = Find.World.GetComponent<FactionFC>()
                .settlements.Select(settlement => new FloatMenuOption(settlement.Name + " - Settlement Level : " +
                    settlement.settlementLevel + " - Prisoners: " +
                    settlement.prisonerList.Count(), delegate
                    {
                        foreach (Pawn pawn in selected)
                        {
                            //disappear colonist
                            TravelUtil.sendPrisoner(pawn, settlement);

                            foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map =>
                                map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>())
                            {
                                if (!Enumerable.Any(bed.OwnersForReading, found => found == pawn)) continue;
                                bed.ForPrisoners = false;
                                bed.ForPrisoners = true;
                            }
                        }
                    }))
                .ToList();

            FloatMenu floatMenu2 = new FloatMenu(settlementList);
            Find.WindowStack.Add(floatMenu2);
        }

        [DebugAction("Empire", "Reset All Military Squad Assignments", allowedGameStates = AllowedGameStates.Playing)]
        private static void resetAllMilitarySquads()
        {
            LogUtil.MessageForce("Debug - Reset All Military Squad Assignments");
            MilitaryCustomizationUtil util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
            for (int i = util.AllMercenaries.Count - 1; i >= 0; i--)
            {
                if (util.AllMercenaries[i].squad.hasLord)
                {
                    util.AllMercenaries[i].squad.map.lordManager.RemoveLord(util.AllMercenaries[i].squad.lord);
                }

                util.AllMercenaries[i].pawn.Destroy();
                util.AllMercenaries[i].squad.mercenaries.Remove(util.AllMercenaries[i]);
            }

            for (int k = util.mercenarySquads.Count() - 1; k >= 0; k--)
            {
                util.mercenarySquads[k].settlement.MilitaryComp.militarySquad = null;
                util.mercenarySquads.RemoveAt(k);
            }


            util.checkMilitaryUtilForErrors();
        }


        [DebugAction("Empire", "Make Random Event", allowedGameStates = AllowedGameStates.Playing)]
        private static void makeRandomEvent()
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
                            Find.World.GetComponent<FactionFC>().addEvent(evt);
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
        private static void procMilitaryTimeDue()
        {
            LogUtil.MessageForce("Debug - Proc MilitaryTimeDue");
            Find.World.GetComponent<FactionFC>().militaryTimeDue = Find.TickManager.TicksGame + 1;
        }

        [DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void attackPlayerSettlement()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
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
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
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
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                }
            }
        }

        [DebugAction("Empire", "Upgrade Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx1() => UpgradePlayerSettlement();

        [DebugAction("Empire", "Upgrade Player Settlement x5", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx5() => UpgradePlayerSettlement(5);

        private static void UpgradePlayerSettlement(int times = 1)
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
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

        [DebugAction("Empire", "Test Function", allowedGameStates = AllowedGameStates.Playing)]
        private static void testVariable()
        {
            LogUtil.MessageForce("Debug - Test Function - ");
            Find.World.GetComponent<FactionFC>().roadBuilder.FlagUpdateRoadQueues();
        }

        [DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void DelevelPlayerSettlement() => UpgradePlayerSettlement(-1);

        [DebugAction("Empire", "Reset Military Squads Cooldowns", allowedGameStates = AllowedGameStates.Playing)]
        private static void ResetMilitarySquads()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.mercenarySquads =
                new List<MercenarySquadFC>();
            LogUtil.MessageForce("Debug - Reset Military Squad Cooldowns");
            foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                settlement.MilitaryComp?.returnMilitary(false);
            }
        }

        [DebugAction("Empire", "Clear Old Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearOldBills()
        {
            Find.World.GetComponent<FactionFC>().OldBills = new List<BillFC>();
        }

        [DebugAction("Empire", "Clear All Events", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearAllEvents()
        {
            Find.World.GetComponent<FactionFC>().events = new List<FCEvent>();
        }

        [DebugAction("Empire", "Clear All Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearAllBills()
        {
            Find.World.GetComponent<FactionFC>().Bills = new List<BillFC>();
        }

        [DebugAction("Empire", "Place 500 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PlaceSilverFC() => SilverPlacer(500);

        [DebugAction("Empire", "Place 50000 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PlaceALotOfSilverFC() => SilverPlacer(50000);

        private static void SilverPlacer(int amount = 500)
        {
            DebugTool tool = null;
            IntVec3 DropPosition;
            Map map;
            tool = new DebugTool("Select Drop Position", delegate
            {
                DropPosition = UI.MouseCell();
                map = Find.CurrentMap;


                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = amount;
                GenPlace.TryPlaceThing(silver, DropPosition, map, ThingPlaceMode.Near);
            });
            DebugTools.curTool = tool;
        }

        /// <summary>
        /// Debug function. Calls in Allied Forces. Doesn't need translations
        /// </summary>
        private static void CallInAlliedForcesSelect()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (WorldSettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                if (settlement.MilitaryComp?.militarySquad != null)
                {
                    list.Add(new FloatMenuOption(settlement.Name, delegate
                    {
                        IncidentParms parms = new IncidentParms();
                        parms.target = Find.CurrentMap;
                        parms.faction = ColonyUtil.getPlayerColonyFaction();
                        parms.podOpenDelay = 140;
                        parms.points = 999;
                        parms.raidArrivalModeForQuickMilitaryAid = true;
                        parms.raidNeverFleeIndividual = true;
                        //parms.raidForceOneIncap = true;
                        parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                        parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
                        parms.raidArrivalModeForQuickMilitaryAid = true;

                        settlement.MilitaryComp.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


                        DebugTool tool = null;
                        IntVec3 DropPosition;
                        tool = new DebugTool("Select Drop Position", delegate
                        {
                            DropPosition = UI.MouseCell();
                            parms.spawnCenter = DropPosition;

                            //List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
                            //parms.raidArrivalMode.Worker.Arrive(list2, parms);
                            settlement.MilitaryComp.militarySquad.isDeployed = true;
                            settlement.MilitaryComp.militarySquad.orderLocation = DropPosition;
                            settlement.MilitaryComp.militarySquad.timeDeployed = Find.TickManager.TicksGame;


                            PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, settlement.MilitaryComp.militarySquad.AllEquippedMercenaryPawns);
                            settlement.MilitaryComp.militarySquad.AllEquippedMercenaryPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
                            settlement.MilitaryComp.militarySquad.isDeployed = true;
                            DebugTools.curTool = null;
                        });
                        DebugTools.curTool = tool;

                        //UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
                    }
                    ));
                }
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }


        [DebugAction("Empire", "Call In Allied Forces", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CallInAlliedForcesDebug() => CallInAlliedForcesSelect();


        [DebugAction("Empire", "Level Up Faction", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LevelUpFaction()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            faction.addExperienceToFactionLevel(faction.factionXPGoal);
        }
    }
}
