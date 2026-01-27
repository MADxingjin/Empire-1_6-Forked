using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using static Mono.Security.X509.X520;

namespace FactionColonies
{
    public class WorldObjectCompProperties_SettlementMilitary : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementMilitary()
        {
            compClass = typeof(WorldObjectComp_SettlementMilitary);
        }
        public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
            {
                yield return parentDef.defName + " has WorldObjectCompProperties_SettlementMilitary but it's not MapParent.";
            }
        }
    }

    public class WorldObjectComp_SettlementMilitary : WorldObjectComp
    {
        private WorldSettlementFC cachedWorldSettlementParent = null;
        public WorldSettlementFC WorldSettlement
        {
            get
            {
                if (cachedWorldSettlementParent != null)
                {
                    return cachedWorldSettlementParent;
                }
                if (parent is WorldSettlementFC ws)
                {
                    cachedWorldSettlementParent = ws;
                }
                else
                {
                    cachedWorldSettlementParent = null;
                    LogUtil.ErrorOnce($"WorldObjectComp_SettlementMilitary has a non-WorldSettlementFC parent: {parent.Label}", 93512108);
                }
                return cachedWorldSettlementParent;
            }
        }
        public Map Map => WorldSettlement.Map;

        public militaryForce attackerForce;
        public List<Pawn> attackers = new List<Pawn>();
        public militaryForce defenderForce;
        public List<Pawn> defenders = new List<Pawn>();
        public List<CaravanSupporting> supporting = new List<CaravanSupporting>();
        //TODO all code referencing isUnderAttack needs to point to this comp
        //     also need to make it so that WorldSettlementFC's without a defense comp don't get targeted
        //     for attacks
        public bool isUnderAttack;
        public bool militaryBusy;
        public int militaryLocation = -1;
        public MilitaryJob militaryJob = MilitaryJob.Undefined;
        public Faction militaryEnemy;
        public MercenarySquadFC militarySquad;
        public int artilleryTimer = 0;
        public bool autoDefend = false;
        public int settlementMilitaryLevel;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref attackers, "attackers", LookMode.Reference);
            Scribe_Collections.Look(ref defenders, "defenders", LookMode.Reference);
            Scribe_Collections.Look(ref supporting, "supporting", LookMode.Reference);
            Scribe_Deep.Look(ref defenderForce, "defenderForce");
            Scribe_Deep.Look(ref attackerForce, "attackerForce");
            Scribe_Values.Look(ref isUnderAttack, "isUnderAttack");
            Scribe_Values.Look(ref militaryBusy, "militaryBusy");
            Scribe_Values.Look(ref militaryLocation, "militaryLocation");
            Scribe_Values.Look(ref militaryJob, "militaryJob");
            Scribe_References.Look(ref militaryEnemy, "militaryEnemy");
            Scribe_References.Look(ref militarySquad, "militarySquad");
            Scribe_Values.Look(ref artilleryTimer, "artilleryTimer");
            Scribe_Values.Look(ref autoDefend, "autoDefend");
        }

        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);

            attackers = new List<Pawn>();
            defenders = new List<Pawn>();
            supporting = new List<CaravanSupporting>();
        }

        private string FoundSettlementString()
        {
            return WorldSettlement.Name + " " + "ShortMilitary".Translate() + " " + WorldSettlement.settlementMilitaryLevel +
                   " - " + "FCAvailable".Translate() + ": " + (!isMilitaryBusySilent()).ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (isUnderAttack)
            {
                yield return DefendColonyAction();
            }
            if (isUnderAttack && !attackers.Any())
            {
                FCEvent evt = MilitaryUtilFC.returnMilitaryEventByLocation(WorldSettlement.Tile);
                if (evt != null)
                {
                    yield return ChangeDefenderAction(evt);
                }
                else
                {
                    LogUtil.Warning($"Settlment {WorldSettlement.Name} is under attack, but found no valid associated event");
                }
            }
        }

        private Command DefendColonyAction()
        {
            Command_Action defendColony = new Command_Action
            {
                defaultLabel = "DefendColony".Translate(),
                defaultDesc = "DefendColonyDesc".Translate(),
                icon = TexLoad.iconMilitary,
                action = delegate
                {
                    startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(WorldSettlement.Tile), () => { });
                }
            };
            /* If auto-battle is enabled, then disable the button. We leave it visible, though, so that the player knows that this is an option if
             * they change their settings. (Once manual fighting becomes an option the player can use, at least) */
            AcceptanceReport canUse = CanDoManualFight();
            if (!canUse.Accepted)
            {
                defendColony.Disable(canUse.Reason);
            }

            return defendColony;
        }

        private Command ChangeDefenderAction(FCEvent evt)
        {
            Command_Action changeDefender = new Command_Action
            {
                defaultLabel = "DefendSettlement".Translate(),
                defaultDesc = "",
                icon = TexLoad.iconCustomize,
                action = delegate
                {
                    var list = new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("SettlementDefendingInformation".Translate(evt.militaryForceDefending.homeSettlement.Name,
                                                                                       evt.militaryForceDefending.militaryLevel),
                                            null, MenuOptionPriority.High),
                        new FloatMenuOption("ChangeDefendingForce".Translate(), () => ChangeDefendingForceAction(evt))
                    };

                    var floatMenu = new FloatMenu(list)
                    {
                        vanishIfMouseDistant = true
                    };
                    Find.WindowStack.Add(floatMenu);
                }
            };

            return changeDefender;
        }

        private void ChangeDefendingForceAction(FCEvent evt)
        {
            var faction = Find.World.GetComponent<FactionFC>();
            var settlementList = new List<FloatMenuOption>
            {
                new FloatMenuOption
                (
                    "ResetToHomeSettlement".Translate(WorldSettlement.settlementMilitaryLevel),
                    delegate { MilitaryUtilFC.changeDefendingMilitaryForce(evt, WorldSettlement); },
                    MenuOptionPriority.High
                )
            };


            settlementList.AddRange
            (
                from foundSettlement in faction.settlements
                where foundSettlement != WorldSettlement && foundSettlement.MilitaryComp?.isMilitaryValid() == true
                select new FloatMenuOption
                (
                    FoundSettlementString(),
                    delegate
                    {
                        if (foundSettlement.MilitaryComp?.isMilitaryBusy() != true)
                            MilitaryUtilFC.changeDefendingMilitaryForce(evt, WorldSettlement);
                    }
                )
            );

            if (settlementList.Count == 0)
                settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));

            var floatMenu2 = new FloatMenu(settlementList)
            {
                vanishIfMouseDistant = true
            };
            Find.WindowStack.Add(floatMenu2);
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (isUnderAttack)
            {
                yield return defendColonyCaravan(caravan);
            }
        }

        private Command defendColonyCaravan(Caravan caravan)
        {
            Command_Action defendColonyCaravan = new Command_Action
            {
                defaultLabel = "DefendColony".Translate(),
                defaultDesc = "DefendColonyDesc".Translate(),
                icon = TexLoad.iconMilitary,
                action = () =>
                {
                    startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(WorldSettlement.Tile), () => CaravanDefend(caravan));
                }
            };
            /* If auto-battle is enabled, then disable the button. We leave it visible, though, so that the player knows that this is an option if
             * they change their settings. (Once manual fighting becomes an option the player can use, at least) */
            AcceptanceReport canUse = CanDoManualFight();
            if (!canUse.Accepted)
            {
                defendColonyCaravan.Disable(canUse.Reason);
            }

            return defendColonyCaravan;
        }

        private AcceptanceReport CanDoManualFight()
        {
            if (FCSettings.settlementsAutoBattle)
            {
                return new AcceptanceReport("autoBattleEnabledNoManualFight".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }

        //TOOD: All following methods were yoinked from WorldSettlementFC. parameters and variables need to be adjusted accordingly
        public void CaravanDefend(Caravan caravan)
        {
            var pawns = caravan.pawns.InnerListForReading.ListFullCopy();
            AddToDefenceFromList(pawns, caravan.Tile);

            if (!caravan.Destroyed) caravan.Destroy();
            var enterCell = FindNearEdgeCell(Map);
            foreach (var pawn in pawns)
            {
                var loc =
                    CellFinder.RandomSpawnCellForPawnNear(enterCell, Map);
                GenSpawn.Spawn(pawn, loc, Map, Rot4.Random);
            }
        }

        public void AddToDefenceFromList(List<Pawn> pawns, int destinationTile)
        {
            if (pawns.NullOrEmpty())
            {
                LogUtil.Error("Tried to add an empty list of pawns to an FCEvent");
                return;
            }

            startDefence(
                MilitaryUtilFC.returnMilitaryEventByLocation(destinationTile), () =>
                {
                    foreach (var pawn in pawns)
                    {
                        if (defenders.Contains(pawn)) return;
                        if (defenders.Any())
                            defenders[0].GetLord().AddPawn(pawn);
                        else
                            LordMaker.MakeNewLord(ColonyUtil.getPlayerColonyFaction(), new LordJob_ColonistsIdle(),
                                WorldSettlement.Map, pawns);
                    }

                    var caravanSupporting = new CaravanSupporting
                    {
                        pawns = pawns
                    };

                    supporting.Add(caravanSupporting);

                    defenders.AddRange(caravanSupporting.pawns);
                });
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            if (isUnderAttack)
                foreach (var option in WorldSettlementDefendAction.GetFloatMenuOptions(caravan, WorldSettlement))
                    yield return option;
        }

        private void deleteMap()
        {
            if (Map == null) return;
            Map.lordManager.lords.Clear();

            CameraJumper.TryJump(WorldSettlement.Tile);
            //Prevent player from zooming back into the settlement
            Current.Game.CurrentMap = Find.AnyPlayerHomeMap;

            //Ignore any empty caravans
            var AllDowned = supporting.All(supporting => supporting.pawns.All(pawn => !pawn.Downed || !pawn.Dead));
            foreach (var caravanSupporting in supporting.Where(supporting => supporting.pawns.Any(
                pawn => !pawn.Downed && !pawn.Dead)))
                CaravanFormingUtility.FormAndCreateCaravan(caravanSupporting.pawns.Where(pawn => pawn.Spawned), Faction.OfPlayer, WorldSettlement.Tile, WorldSettlement.Tile, -1);

            if (AllDowned && defenders.Any())
            {
                var pawns = new HashSet<Thing>();
                foreach (var caravanSupporting in supporting)
                    foreach (var pawn in caravanSupporting.pawns)
                        if (!pawn.Dead)
                        {
                            pawn.DeSpawn();
                            pawns.Add(pawn);
                        }

                foreach (Pawn pawn in pawns)
                    if (!pawn.Dead)
                    {
                        var num2 = 0;
                        while (pawn.health.HasHediffsNeedingTend())
                        {
                            num2++;
                            if (num2 > 10000)
                            {
                                LogUtil.Error("WorldSettlementFC.deleteMap: Too many iterations.");
                                return;
                            }

                            TendUtility.DoTend(null, pawn, null);
                        }
                    }

                var eventParams = new FCEvent
                {
                    location = Find.AnyPlayerHomeMap.Tile,
                    source = WorldSettlement.Tile,
                    goods = pawns.ToList(),
                    customDescription = DeliveryEvent.ShuttleEventInjuredString,
                    timeTillTrigger = Find.TickManager.TicksGame +
                                      TravelUtil.ReturnTicksToArrive(WorldSettlement.Tile, Find.AnyPlayerHomeMap.Tile)
                };

                if (pawns.Any()) DeliveryEvent.CreateDeliveryEvent(eventParams);
            }

            if (Map.mapPawns?.AllPawnsSpawned == null) return;

            //Despawn removes them from AllPawnsSpawned, so we copy it
            //foreach (var pawn in Map.mapPawns.AllPawnsSpawned.ListFullCopy()) pawn.DeSpawn();
        }

        public void startDefence(FCEvent evt, Action after)
        {
            if (FCSettings.settlementsAutoBattle)
            {
                var won = SimulateBattleFc.FightBattle(evt.militaryForceAttacking, evt.militaryForceDefending) == 1;
                endBattle(won, (int)evt.militaryForceDefending.forceRemaining);
                return;
            }

            if (defenderForce == null)
            {
                endBattle(false, 0);
                return;
            }

            LongEventHandler.QueueLongEvent(() =>
            {
                if (Map == null)
                    MapGenerator.GenerateMap(new IntVec3(70 + WorldSettlement.settlementLevel * 10, 1, 70 + WorldSettlement.settlementLevel * 10),
                                             WorldSettlement, WorldSettlement.MapGeneratorDef, WorldSettlement.ExtraGenStepDefs).mapDrawer.RegenerateEverythingNow();

                zoomIntoTile(evt);
                after.Invoke();
            },
                "GeneratingMap", false, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
        }

        private void zoomIntoTile(FCEvent evt)
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            if (Current.Game.CurrentMap != Map && !defenders.Any())
            {
                if (evt == null)
                {
                    LogUtil.Warning("Aborting defense, null FCEvent!");
                    return;
                }

                evt.timeTillTrigger = Find.TickManager.TicksGame;
                var force = MilitaryUtilFC.returnDefendingMilitaryForce(evt);
                if (force == null) return;

                if (force.homeSettlement.MilitaryComp != null)
                    force.homeSettlement.MilitaryComp.militaryBusy = true;

                foreach (var building in Map.listerBuildings.allBuildingsColonist)
                    FloodFillerFog.FloodUnfog(building.InteractionCell, Map);

                generateFriendlies(force);
            }

            if (Current.Game.CurrentMap == Map && Find.World.renderer.wantedMode != WorldRenderMode.Planet) return;

            if (defenders.Any())
                CameraJumper.TryJump(new GlobalTargetInfo(defenders[0]));
            else if (Map.mapPawns.AllPawnsSpawned.Any())
                CameraJumper.TryJump(new GlobalTargetInfo(Map.mapPawns.AllPawnsSpawned[0]));
            else
                CameraJumper.TryJump(new IntVec3(Map.Size.x / 2, 0, Map.Size.z / 2), Map);
        }

        public static IntVec3 FindNearEdgeCell(Map map)
        {
            bool BaseValidator(IntVec3 x)
            {
                return x.Standable(map) && !x.Fogged(map);
            }

            var hostFaction = map.ParentFaction;
            if (CellFinder.TryFindRandomEdgeCellWith(x =>
            {
                if (!BaseValidator(x))
                    return false;
                if (hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction))
                    return true;
                return hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(x);
            }, map, CellFinder.EdgeRoadChance_Neutral, out var result))
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            if (CellFinder.TryFindRandomEdgeCellWith(BaseValidator, map, CellFinder.EdgeRoadChance_Neutral, out result))
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            LogUtil.Warning("Could not find any valid edge cell.");
            return CellFinder.RandomCell(map);
        }

        private void generateFriendlies(militaryForce force)
        {
            var points = (float)(force.militaryLevel * force.militaryEfficiency * 100);
            List<Pawn> friendlies;
            var riders = new Dictionary<Pawn, Pawn>();
            if (force.homeSettlement.MilitaryComp?.militarySquad != null &&
                force.homeSettlement.MilitaryComp.militarySquad.mercenaries.Any())
            {
                var squad = force.homeSettlement.MilitaryComp.militarySquad;

                squad.OutfitSquad(squad.settlement.MilitaryComp.militarySquad.outfit);
                squad.updateSquadStats(squad.settlement.settlementMilitaryLevel);
                squad.resetNeeds();

                friendlies = squad.AllEquippedMercenaryPawns.ToList();

                foreach (var animal in squad.animals) riders.Add(animal.handler.pawn, animal.pawn);
            }
            else
            {
                var parms = new IncidentParms
                {
                    target = Map,
                    faction = ColonyUtil.getPlayerColonyFaction(),
                    generateFightersOnly = true,
                    raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
                };
                parms.points = IncidentWorker_Raid.AdjustedRaidPoints(points,
                    PawnsArrivalModeDefOf.EdgeWalkIn, parms.raidStrategy,
                    parms.faction, PawnGroupKindDefOf.Combat,
                    parms.target // new required parameter
                );
                friendlies = PawnGroupMakerUtility.GeneratePawns(
                    IncidentParmsUtility.GetDefaultPawnGroupMakerParms(
                        PawnGroupKindDefOf.Combat, parms, true)).ToList();
                if (!friendlies.Any()) LogUtil.Error("Got no pawns spawning raid from parms " + parms);
            }

            void tryFindLoc(out IntVec3 loc, Pawn friendly)
            {
                var min = (70 + WorldSettlement.settlementLevel * 10) / 2 - 5 - 5 * WorldSettlement.settlementLevel;
                var size = 10 + WorldSettlement.settlementLevel * 10;
                CellFinder.TryFindRandomCellInsideWith(new CellRect(min, min, size, size),
                    testing => testing.Standable(Map) && Map.reachability.CanReachMapEdge(testing,
                        TraverseParms.For(TraverseMode.PassDoors)), out loc);
                if (loc.x == -1000)
                {
                    LogUtil.Message("Failed with " + friendly + ", " + loc);
                    CellFinder.TryFindRandomCellNear(new IntVec3(min + 10 + WorldSettlement.settlementLevel, 1,
                            min + 10 + WorldSettlement.settlementLevel), Map, 75,
                        testing => testing.Standable(Map), out loc);
                }
            }

            foreach (var friendly in friendlies)
            {
                if (friendly.IsWildMan()) continue;

                friendly.ApplyIdeologyRitualWounds();

                IntVec3 loc;
                if (friendly.AnimalOrWildMan())
                {
                    if (riders.Count > 0)
                    {
                        try
                        {
                            var owner = riders.First(pair => pair.Value.thingIDNumber == friendly.thingIDNumber).Key;
                            CellFinder.TryFindRandomCellInsideWith(new CellRect((int)owner.DrawPos.x - 5,
                                    (int)owner.DrawPos.z - 5, 10, 10),
                                testing => testing.Standable(Map) && Map.reachability.CanReachMapEdge(testing,
                                    TraverseParms.For(TraverseMode.PassDoors)), out loc);
                        }
                        catch
                        {
                            var isAnimal = friendly.RaceProps.Animal ? "animal" : "human";
                            LogUtil.Error("No pair found for " + isAnimal + ": " + friendly.thingIDNumber +
                                      ", and riders dictionary is not empty!");
                            continue;
                        }
                    }
                    else
                    {
                        LogUtil.Error("Rider Dictionary is empty but animal was still generated?");
                        continue;
                    }
                }
                else
                {
                    tryFindLoc(out loc, friendly);
                }

                GenSpawn.Spawn(friendly, loc, Map, new Rot4());
                friendly.drafter = new Pawn_DraftController(friendly);


                Map.mapPawns.RegisterPawn(friendly);
                friendly.drafter.Drafted = true;
            }

            LordMaker.MakeNewLord(ColonyUtil.getPlayerColonyFaction(), new LordJob_DefendColony(riders), Map,
                friendlies);

            defenders = friendlies;
        }

        public void endBattle(bool won, int remaining)
        {
            var faction = Find.World.GetComponent<FactionFC>();

            LogUtil.Message("WorldSettlementFC.endBattle: Handling combat resolution...");
            try
            {
                if (won)
                {
                    WinBattle(faction);
                }
                else
                {
                    LoseBattle(faction);
                }
                LogUtil.Message("WorldSettlementFC.endBattle: Handling foreign defenders...");
                CooldownMilitary(remaining);
            }
            catch (Exception e)
            {
                LogUtil.Error($"Encountered an error while trying to resolve combat in Empire{Environment.NewLine}{e}");
            }
            isUnderAttack = false;
        }

        private void CooldownMilitary(int remaining)
        {
            if (defenderForce?.homeSettlement == WorldSettlement)
            {
                defenderForce?.homeSettlement?.MilitaryComp?.cooldownMilitary();
            }
            else if (defenderForce == null)
            {
                LogUtil.Message("Defending force not set-- if the attack came from another mod, this is fine.");
            }
            else
            {
                // if not the home settlement defending
                if (remaining >= 7)
                {
                    Find.LetterStack.ReceiveLetter("OverwhelmingVictory".Translate(),
                        "OverwhelmingVictoryDesc".Translate(), LetterDefOf.PositiveEvent);
                    defenderForce.homeSettlement.MilitaryComp?.returnMilitary(true);
                }
                else
                {
                    defenderForce.homeSettlement.MilitaryComp?.cooldownMilitary();
                }
            }
        }

        private void LoseBattle(FactionFC faction)
        {
            var happinessLostMultiplier = TraitUtilsFC.cycleTraits("happinessLostMultiplier", WorldSettlement.Traits, Operation.Multiplication); ;
            var loyaltyLostMultiplier = TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", WorldSettlement.Traits, Operation.Multiplication); ;

            var muliplier = 1;
            if (faction.hasPolicy(FCPolicyDefOf.feudal))
                muliplier = 2;
            float prosperityMultiplier = 1;
            var canDestroyBuildings = true;
            if (faction.hasTrait(FCPolicyDefOf.resilient))
            {
                prosperityMultiplier = .5f;
                canDestroyBuildings = false;
            }

            // LogUtil.Message("Determined Multipliers for loss penalty");
            // if winner are enemies
            WorldSettlement.prosperity -= 20 * prosperityMultiplier;
            WorldSettlement.happiness -= 25 * happinessLostMultiplier;
            WorldSettlement.loyalty -= 15 * loyaltyLostMultiplier * muliplier;

            string str = "DefenseFailureFull".Translate(WorldSettlement.Name);

            if (canDestroyBuildings && WorldSettlement?.BuildingsComp != null)
            {
                for (var k = 0; k < 4; k++)
                {
                    var deconstructRoll = new IntRange(0, 10).RandomInRange;
                    var deconstructChance = 7;
                    if (deconstructRoll < deconstructChance ||
                        !WorldSettlement.BuildingsComp.buildingSlotIsBuilding(k))
                    {
                        continue;
                    }
                    str += "\n" + "BuildingDestroyedInRaid".Translate(WorldSettlement.BuildingsComp.buildingLabel(k));
                    WorldSettlement.deconstructBuilding(k);
                }
            }

            // LogUtil.Message("Building deconstruction handled");
            // level remover checker
            if (WorldSettlement.settlementLevel > 1 && canDestroyBuildings)
            {
                var num = new IntRange(0, 10).RandomInRange;
                if (num >= 7)
                {
                    str += "\n\n" + "SettlementDeleveledRaid".Translate();
                    WorldSettlement.delevelSettlement();
                }
            }

            // LogUtil.Message("Settlement deleveling handled");
            Find.LetterStack.ReceiveLetter("DefenseFailure".Translate(), str, LetterDefOf.Death,
                new LookTargets(WorldSettlement));
        }

        private void WinBattle(FactionFC faction)
        {
            faction.addExperienceToFactionLevel(5f);
            Find.LetterStack.ReceiveLetter("DefenseSuccessful".Translate(),
                "DefenseSuccessfulFull".Translate(WorldSettlement.Name),
                LetterDefOf.PositiveEvent, new LookTargets(WorldSettlement));
        }

        private void endAttack()
        {
            endBattle(defenders.Any(), defenders.Count);
            deleteMap();

            supporting.Clear();
            defenders.Clear();
            defenderForce = null;
            attackers.Clear();
            attackerForce = null;
        }

        public void removeAttacker(Pawn downed)
        {
            attackers.Remove(downed);
            if (attackers.Any()) return;
            LongEventHandler.QueueLongEvent(endAttack,
                "EndingAttack", false, error =>
                {
                    DelayedErrorWindowRequest.Add("ErrorEndingAttack".Translate(),
                        "ErrorEndingAttackDescription".Translate());
                    LogUtil.Error(error.Message);
                });
        }

        public void removeDefender(Pawn defender)
        {
            defenders.Remove(defender);
            if (defenders.Any()) return;
            LongEventHandler.QueueLongEvent(endAttack,
                "EndingAttack", false, error =>
                {
                    DelayedErrorWindowRequest.Add("ErrorEndingAttack".Translate(),
                        "ErrorEndingAttackDescription".Translate());
                    LogUtil.Error(error.Message);
                });
        }

        //TODO needs to override PostCaravanFormed instead (since this is a comp). Functionally there isn't much difference,
        //     as MapParent.Notify_CaravanFormed calls PostCaravanFormed on all comps.
        public override void PostCaravanFormed(Caravan caravan)
        {
            var foundCaravan = new List<CaravanSupporting>();
            foreach (var found in caravan.pawns)
            {
                if (found.GetLord() != null) found.GetLord().ownedPawns.Remove(found);

                foreach (var caravanSupporting in
                    supporting.Where(caravanSupporting => caravanSupporting.pawns.Contains(found)))
                {
                    foundCaravan.Add(caravanSupporting);
                    caravanSupporting.pawns.Remove(found);
                    break;
                }
            }

            foreach (var caravanSupporting in foundCaravan.Where(caravanSupporting =>
                    caravanSupporting.pawns.Find(pawn => !pawn.Downed &&
                                                         !pawn.Dead && !pawn.AnimalOrWildMan()) == null))
                //Prevent removing while creating end battle caravans
                if (isUnderAttack)
                    supporting.Remove(caravanSupporting);
            /*It appears vanilla handles this automatically
                foreach (Pawn animal in caravanSupporting.supporting.FindAll(pawn => pawn.AnimalOrWildMan()))
                {
                    animal.holdingOwner = null;
                    animal.DeSpawn();
                    Find.WorldPawns.PassToWorld(animal);
                    caravan.pawns.TryAdd(animal);
                }*/

            //Appears to not happen sometimes, no clue why
            foreach (var pawn in caravan.pawns) Map.reservationManager.ReleaseAllClaimedBy(pawn);

            base.PostCaravanFormed(caravan);
        }

        public void SendMilitary(PlanetTile location, MilitaryJob job, int timeToFinish, Faction enemy)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            if (isMilitaryBusy() || isTargetOccupied(location)) return;

            militaryBusy = true;
            militaryJob = job;
            militaryLocation = location;

            if (enemy != null) militaryEnemy = enemy;
            if (job != MilitaryJob.Deploy) Find.World.GetComponent<FactionFC>().militaryTargets.Add(location);

            FCEvent evt;
            switch (militaryJob)
            {
                case MilitaryJob.RaidEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.raidEnemySettlement);
                    evt.customDescription = "settlementMilitaryForcesRaiding".Translate(WorldSettlement.Name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentRaid".Translate(WorldSettlement.Name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, WorldSettlement.Tile, timeToFinish);
                    break;

                case MilitaryJob.EnslaveEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.enslaveEnemySettlement);
                    evt.customDescription = "settlementMilitaryForcesEnslave".Translate(WorldSettlement.Name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentEnslave".Translate(WorldSettlement.Name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, WorldSettlement.Tile, timeToFinish);
                    break;

                case MilitaryJob.CaptureEnemySettlement:
                    evt = FCEventMaker.MakeEvent(FCEventDefOf.captureEnemySettlement);
                    evt.customDescription = "settlementMilitaryForcesCapturing".Translate(WorldSettlement.Name, returnMilitaryTarget().Label);
                    Find.LetterStack.ReceiveLetter("FCMilitaryAction".Translate(), "FCMilitarySentCapture".Translate(WorldSettlement.Name, Find.WorldObjects.SettlementAt(location)), LetterDefOf.NeutralEvent);
                    evt.DefineEvent(factionfc, WorldSettlement.Tile, timeToFinish);
                    break;

                default:
                    break;
            }
        }

        public Settlement returnMilitaryTarget()
        {
            return militaryLocation == -1 ? null : Find.WorldObjects.SettlementAt(militaryLocation);
        }

        public void processMilitaryEvent()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            //calculate success and all of that shit

            //Debug by setting faction automatically
            //returnMilitaryTarget().SetFaction(FactionColonies.getPlayerColonyFaction());
            if (faction.militaryTargets.Contains(militaryLocation))
            {
                faction.militaryTargets.Remove(militaryLocation);
            }
            //LogUtil.Message(winner + " job = " + militaryJob);
            //Process end result here
            //attacker == 0; defender == 1;

            switch (militaryJob)
            {
                case MilitaryJob.RaidEnemySettlement:
                    {
                        int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(WorldSettlement, true),
                            militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                        if (winner == 0)
                        {
                            //if won
                            faction.addExperienceToFactionLevel(5f);

                            TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                            int lootLevel;
                            bool getSlaves = true;


                            switch (tech)
                            {
                                case TechLevel.Archotech:
                                case TechLevel.Ultra:
                                case TechLevel.Spacer:
                                    lootLevel = 4;
                                    break;
                                case TechLevel.Industrial:
                                    lootLevel = 3;
                                    break;
                                case TechLevel.Medieval:
                                case TechLevel.Neolithic:
                                    lootLevel = 2;
                                    break;
                                default:
                                    lootLevel = 1;
                                    break;
                            }

                            if (Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.defName == "VFEI_Insect")
                            {
                                lootLevel = 3;
                                getSlaves = false;
                            }

                            List<Thing> loot = PaymentUtil.generateRaidLoot(lootLevel, tech);

                            string text = "settlementDeliveringLoot".Translate();
                            text = loot.Aggregate(text, (current, thing) => current + thing.LabelCap + " " + thing.stackCount + "x\n ");

                            int num = new IntRange(0, 10).RandomInRange;
                            if (num <= 4 && getSlaves)
                            {
                                Pawn prisoner = PaymentUtil.generatePrisoner(militaryEnemy);
                                text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), WorldSettlement.Name);
                                WorldSettlement.addPrisoner(prisoner);
                            }

                            Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                                "RaidEnemySettlementSuccess".Translate(
                                    Find.WorldObjects.SettlementAt(militaryLocation).LabelCap) + "\n" + text,
                                LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));

                            //deliver

                            FCEvent eventParams = new FCEvent()
                            {
                                location = Find.AnyPlayerHomeMap.Tile,
                                source = WorldSettlement.Tile,
                                goods = loot,
                                customDescription = text,
                                timeTillTrigger = Find.TickManager.TicksGame + TravelUtil.ReturnTicksToArrive(WorldSettlement.Tile, Find.AnyPlayerHomeMap.Tile)
                            };

                            DeliveryEvent.CreateDeliveryEvent(eventParams);
                        }
                        else
                        {
                            //if lost
                            Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                                "RaidEnemySettlementFailure".Translate(
                                    Find.WorldObjects.SettlementAt(militaryLocation).LabelCap), LetterDefOf.NegativeEvent,
                                new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }

                        break;
                    }
                case MilitaryJob.EnslaveEnemySettlement:
                    {
                        int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(WorldSettlement, true),
                            militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                        if (winner == 0)
                        {
                            //if won
                            faction.addExperienceToFactionLevel(5f);

                            string text = "";

                            int num = new IntRange(1, 3).RandomInRange;
                            for (int i = 0; i <= num; i++)
                            {
                                Pawn prisoner = PaymentUtil.generatePrisoner(militaryEnemy);
                                text += "PrisonerCaptureInfo".Translate(prisoner.Name.ToString(), WorldSettlement.Name) + "\n";
                                WorldSettlement.addPrisoner(prisoner);
                            }

                            Find.LetterStack.ReceiveLetter("RaidLoot".Translate(),
                                "RaidEnemySettlementSuccess".Translate(
                                    Find.WorldObjects.SettlementAt(militaryLocation).LabelCap) + "\n" + text,
                                LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }
                        else if (winner == 1)
                        {
                            //if lost
                            Find.LetterStack.ReceiveLetter("RaidFailure".Translate(),
                                "RaidEnemySettlementFailure".Translate(
                                    Find.WorldObjects.SettlementAt(militaryLocation).LabelCap), LetterDefOf.NegativeEvent,
                                new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }

                        break;
                    }
                case MilitaryJob.CaptureEnemySettlement:
                    {
                        int winner = SimulateBattleFc.FightBattle(militaryForce.createMilitaryForceFromSettlement(WorldSettlement, true),
                            militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                        if (winner == 0)
                        {
                            faction.addExperienceToFactionLevel(5f);

                            string tmpName = Find.WorldObjects.SettlementAt(militaryLocation).LabelCap;
                            TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                            Faction tempFactionLink = Find.WorldObjects.SettlementAt(militaryLocation).Faction;
                            Find.WorldObjects.SettlementAt(militaryLocation).Destroy();
                            WorldSettlementFC worldsettlement = ColonyUtil.createPlayerColonySettlement(militaryLocation, WorldSettlementDefOf.WorldSettlementDef_Surface);
                            worldsettlement.Name = tmpName;

                            int upgradeTimes;

                            switch (tech)
                            {
                                case TechLevel.Archotech:
                                case TechLevel.Ultra:
                                case TechLevel.Spacer:
                                    upgradeTimes = 2;
                                    break;
                                case TechLevel.Industrial:
                                    upgradeTimes = 1;
                                    break;
                                default:
                                    upgradeTimes = 0;
                                    break;
                            }

                            WorldSettlement.upgradeSettlement(upgradeTimes);

                            WorldSettlement.loyalty = 15;
                            WorldSettlement.happiness = 25;
                            WorldSettlement.unrest = 20;
                            WorldSettlement.prosperity = 70;

                            bool defeated = !Find.WorldObjects.Settlements.Any(settlement => settlement.Faction != null
                                && settlement.Faction == tempFactionLink);

                            if (defeated)
                            {
                                tempFactionLink.defeated = true;
                            }

                            Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                                "CaptureEnemySettlementSuccess".Translate(WorldSettlement.Name,
                                    Find.WorldObjects.SettlementAt(militaryLocation).Name, WorldSettlement.settlementLevel),
                                LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }
                        else if (winner == 1)
                        {
                            Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),
                                "CaptureEnemySettlementFailure".Translate(WorldSettlement.Name,
                                    Find.WorldObjects.SettlementAt(militaryLocation).Name), LetterDefOf.NegativeEvent,
                                new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                        }

                        break;
                    }
            }

            cooldownMilitary();
        }

        public void returnMilitary(bool alert)
        {
            militaryBusy = false;
            militaryJob = MilitaryJob.Undefined;
            militaryLocation = -1;
            militaryEnemy = null;

            if (alert)
            {
                Find.LetterStack.ReceiveLetter("Military Cooldown", "FCMilitaryCooldown".Translate(WorldSettlement.Name),
                    LetterDefOf.PositiveEvent);
            }
        }

        public void cooldownMilitary()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();

            int cooldownReduction = 0;
            if (faction.hasTrait(FCPolicyDefOf.raiders) && (militaryJob == MilitaryJob.RaidEnemySettlement || militaryJob == MilitaryJob.EnslaveEnemySettlement))
            {
                cooldownReduction += 60000;
            }
            else if (militaryJob == MilitaryJob.Deploy && FCSettings.deadPawnsIncreaseMilitaryCooldown)
            {
                List<string> policies = faction.policies.ConvertAll(policy => policy.def.defName);
                bool militarist = policies.Contains("militaristic");
                bool authoritarian = policies.Contains("authoritarian");
                bool pacifist = policies.Contains("pacifist");

                int deadMultiplier = (militarist || authoritarian ? militarist && authoritarian ? 7000 : 8000 : 10000) + (pacifist ? 2000 : 0);

                cooldownReduction -= militarySquad.dead * deadMultiplier;
            }

            militaryJob = MilitaryJob.Cooldown;
            militaryBusy = true;
            militaryLocation = WorldSettlement.Tile;
            militaryEnemy = null;

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.cooldownMilitary);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 180000 - cooldownReduction;
            tmp.location = WorldSettlement.Tile;
            tmp.customDescription = "MilitaryForcesReorganizing".Translate(WorldSettlement.Name); // + 
            Find.World.GetComponent<FactionFC>().addEvent(tmp);
        }

        public bool isMilitaryBusy(bool silent = false)
        {
            if (militaryBusy && !silent)
            {
                Messages.Message("militaryAlreadyAssigned".Translate(), MessageTypeDefOf.RejectInput);
            }

            return militaryBusy;
        }

        public bool isMilitarySquadValid()
        {
            if (militarySquad != null)
            {
                if (militarySquad.outfit != null)
                {
                    if (militarySquad.EquippedMercenaries.Count > 0)
                    {
                        return true;
                    }

                    Messages.Message("You can't deploy a squad with no equipped personnel!",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }

                Messages.Message("There is no squad loadout assigned to that settlement!",
                    MessageTypeDefOf.RejectInput);
                return false;
            }

            Messages.Message("There is no military squad assigned to that settlement!", MessageTypeDefOf.RejectInput);
            return false;
        }

        public bool isMilitarySquadValidSilent()
        {
            if (militarySquad != null)
            {
                return true;
            }

            return false;
        }

        public bool isMilitaryBusySilent()
        {
            return militaryBusy;
        }

        public bool isMilitaryValid()
        {
            if (WorldSettlement.settlementMilitaryLevel > 0)
            {
                //if settlement military is more than level 0
                return true;
            }

            return false;
        }

        public bool isTargetOccupied(int location)
        {
            if (Find.World.GetComponent<FactionFC>().militaryTargets.Contains(location))
            {
                Messages.Message("targetAlreadyBeingAttacked".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            }

            return false;
        }
    }
}
