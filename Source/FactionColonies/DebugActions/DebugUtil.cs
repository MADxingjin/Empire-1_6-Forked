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
                Log.Message(e.def.defName + " with cooldown: " + (e.timeTillTrigger - Find.TickManager.TicksGame));
            });
        }

        [DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeFiveDays()
        {
            //Log.Message("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
        }

        [DebugAction("Empire", "Increment Time 1 Year", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeOneYear()
        {
            //Log.Message("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + GenDate.TicksPerYear);
        }

        [DebugAction("Empire", "Print Races", allowedGameStates = AllowedGameStates.Playing)]
        private static void PrintRaces()
        {
            ColonyUtil.getPlayerColonyFaction().def.pawnGroupMakers.ForEach(maker =>
            {
                Log.Message("Traders: " + maker.traders.Count);
                foreach (PawnGenOption option in maker.options)
                {
                    Log.Message("Race: " + option.kind.race.defName + ", " + option.kind.defName + ", " +
                                option.kind.isFighter + ", " + option.kind.trader + " for " + maker.kindDef);
                }
            });
        }

        [DebugAction("Empire", "Reset All Military Squad Assignments", allowedGameStates = AllowedGameStates.Playing)]
        private static void resetAllMilitarySquads()
        {
            Log.Message("Debug - Reset All Military Squad Assignments");
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
                util.mercenarySquads[k].settlement.militarySquad = null;
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
                        Log.Message("Debug - Make Random Event - " + evtDef.label);
                        FCEvent evt = FCEventMaker.MakeRandomEvent(evtDef, null);
                        if (evtDef.activateAtStart == false)
                        {
                            FCEventMaker.MakeRandomEvent(evtDef, null);
                            Find.World.GetComponent<FactionFC>().addEvent(evt);
                        }

                        //letter code
                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.name}", "\n");

                        if (!settlementString.NullOrEmpty()) Find.LetterStack.ReceiveLetter("Random Event", $"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                    }
                    ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Proc MilitaryTimeDue", allowedGameStates = AllowedGameStates.Playing)]
        private static void procMilitaryTimeDue()
        {
            Log.Message("Debug - Proc MilitaryTimeDue");
            Find.World.GetComponent<FactionFC>().militaryTimeDue = Find.TickManager.TicksGame + 1;
        }

        [DebugAction("Empire", "Fix Missing Settlements", allowedGameStates = AllowedGameStates.Playing)]
        private static void checkForMissingSettlements()
        {
            Log.Message("Debug - Proc MilitaryTimeDue");

            FactionFC factionfc = Find.World.GetComponent<FactionFC>();

            foreach (SettlementFC settlement in factionfc.settlementsOnPlanet)
            {
                if (Find.WorldObjects.AnyWorldObjectAt(settlement.mapLocation) == false)
                {
                    ColonyUtil.createPlayerColonySettlement(settlement.mapLocation, true, Find.World.info.name);
                }
            }
        }


        [DebugAction("Empire", "Reset Faction Leaders", allowedGameStates = AllowedGameStates.Playing)]
        private static void resetFactionLeadeers()
        {
            Log.Message("Debug - Reset Faction Leaders");
            SoS2HarmonyPatches.ResetFactionLeaders();
        }

        [DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void attackPlayerSettlement()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate
                {
                    Log.Message("Debug - Attack Player Settlement - " + settlement.name);
                    Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
                    MilitaryUtilFC.attackPlayerSettlement(
                        militaryForce.createMilitaryForceFromFaction(enemyFaction, true), settlement, enemyFaction);
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
                        worldcomp.returnSettlementByLocation(evt.location, evt.planetName).name,
                        DebugMenuOptionMode.Action, delegate
                        {
                            //when event is selected, select defending force to replace it with

                            List<DebugMenuOption> list2 = new List<DebugMenuOption>();
                            foreach (SettlementFC settlement in worldcomp.settlements)
                            {
                                if (settlement.isMilitaryValid() && settlement.name != evt.settlementFCDefending.name)
                                {
                                    list2.Add(new DebugMenuOption(
                                        settlement.name + " - " + settlement.settlementMilitaryLevel + " - Busy: " +
                                        settlement.isMilitaryBusySilent(), DebugMenuOptionMode.Action, delegate
                                        {
                                            if (settlement.isMilitaryBusy() == false)
                                            {
                                                Log.Message("Debug - Change Player Settlement - " +
                                                            evt.militaryForceDefending.homeSettlement.name + " to " +
                                                            settlement.name);
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
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate
                {
                    if (times > 0)
                    {
                        Log.Message("Debug - Upgrade Player Settlement x" + times + "- " + settlement.name);
                    }
                    else
                    {
                        Log.Message("Debug - Downgrade Player Settlement x" + times + "- " + settlement.name);
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
            Log.Message("Debug - Test Function - ");
            Find.World.GetComponent<FactionFC>().roadBuilder.FlagUpdateRoadQueues();
        }

        [DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void DelevelPlayerSettlement() => UpgradePlayerSettlement(-1);

        [DebugAction("Empire", "Reset Military Squads Cooldowns", allowedGameStates = AllowedGameStates.Playing)]
        private static void ResetMilitarySquads()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.mercenarySquads =
                new List<MercenarySquadFC>();
            Log.Message("Debug - Reset Military Squad Cooldowns");
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                settlement.returnMilitary(false);
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
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                if (settlement.militarySquad != null)
                {
                    list.Add(new FloatMenuOption(settlement.name, delegate
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

                        settlement.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


                        DebugTool tool = null;
                        IntVec3 DropPosition;
                        tool = new DebugTool("Select Drop Position", delegate
                        {
                            DropPosition = UI.MouseCell();
                            parms.spawnCenter = DropPosition;

                            //List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
                            //parms.raidArrivalMode.Worker.Arrive(list2, parms);
                            settlement.militarySquad.isDeployed = true;
                            settlement.militarySquad.orderLocation = DropPosition;
                            settlement.militarySquad.timeDeployed = Find.TickManager.TicksGame;


                            PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms,
                                settlement.militarySquad.AllEquippedMercenaryPawns);
                            settlement.militarySquad.AllEquippedMercenaryPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
                            settlement.militarySquad.isDeployed = true;
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
