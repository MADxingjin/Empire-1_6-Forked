using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies.util
{
    public static class ColonyUtil
    {
        //<DevAdd>   Create new seperate function to create a faction
        public static WorldSettlementFC createPlayerColonySettlement(PlanetTile tile, WorldSettlementDef settlementType)
        {
            if (settlementType == null)
            {
                LogUtil.Error($"Tried to create a settlement with null WorldSettlementDef! Using default WorldSettlementDef.");
                settlementType = WorldSettlementDefOf.WorldSettlementDef_Surface;
            }

            /* Do any pre-settlement-creation demanded of the settlement type */
            settlementType.GetModExtension<SettlementTypeExtension>().preCreation(ref tile, ref settlementType);

            LogUtil.Message($"Creating settlement of type {settlementType.defName}");
            Faction faction = FactionCache.PlayerColonyFaction;

            FactionFC worldcomp = FactionCache.FactionComp;
            if (!worldcomp.settlements.Any())
            {
                FactionCache.FactionComp.timeStart = Find.TickManager.TicksGame;
            }

            WorldSettlementFC settlement = (WorldSettlementFC)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldSettlementDef>.GetNamed(settlementType.defName));
            settlement.PostPostMake(tile);

            settlement.SetFaction(faction);
            Find.WorldObjects.Add(settlement);

            if (worldcomp.hasPolicy(FCPolicyDefOf.militaristic))
                settlement.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
            if (worldcomp.hasPolicy(FCPolicyDefOf.authoritarian))
                settlement.loyalty = 70;
            if (worldcomp.hasPolicy(FCPolicyDefOf.egalitarian))
                settlement.happiness = 60;
            if (worldcomp.hasPolicy(FCPolicyDefOf.expansionist) && settlement.settlementLevel == 1)
                settlement.upgradeSettlement();

            worldcomp.addSettlement(settlement);
            worldcomp.roadBuilder.FlagUpdateRoadQueues();

            /* Do any post-settlement-creation demanded of the settlement type */
            settlementType.GetModExtension<SettlementTypeExtension>().postCreation(settlement);

            Find.LetterStack.ReceiveLetter("FCSettlementFormed".Translate(), "TheSettlement".Translate() + " " + settlement.Name + "HasBeenFormed".Translate() + "!", LetterDefOf.PositiveEvent);

            return settlement;
        }

        public static void removePlayerSettlement(WorldSettlementFC settlement)
        {
            settlement.PrepareDestroyWorldObject();
            FactionFC faction = FactionCache.FactionComp;
            faction.settlements.Remove(settlement);
            Messages.Message("SettlementRemoved".Translate(settlement.Name), MessageTypeDefOf.NegativeEvent);

            Find.WorldObjects.Remove(Find.World.worldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed(settlement.def.defName), settlement.Tile));

            //clear military events
            settlement.MilitaryComp?.returnMilitary(false);

            HashSet<FCEvent> toRemove = new HashSet<FCEvent>();

            foreach (FCEvent evt in faction.events)
            {
                //military event removal
                if (evt.def == FCEventDefOf.captureEnemySettlement || evt.def == FCEventDefOf.raidEnemySettlement)
                {
                    if (evt.militaryForceAttacking.homeSettlement == settlement)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def == FCEventDefOf.settlementBeingAttacked)
                {
                    if (evt.militaryForceDefending.homeSettlement == settlement)
                    {
                        if (evt.settlementFCDefending == settlement)
                        {
                            toRemove.Add(evt);
                        }

                        //if not defending settlement
                        MilitaryUtilFC.changeDefendingMilitaryForce(evt, evt.settlementFCDefending);
                    }
                    else
                    {
                        //if force belongs to other settlement
                        evt.militaryForceDefending.homeSettlement.MilitaryComp?.cooldownMilitary();

                        toRemove.Add(evt);
                    }
                }


                //settlement event removal
                if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy ||
                    evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
                {
                    if (evt.source == settlement.Tile)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def.isRandomEvent && evt.settlementTraitLocations.Count > 0)
                {
                    if (evt.settlementTraitLocations.Contains(settlement))
                    {
                        evt.settlementTraitLocations.Remove(settlement);
                        if (evt.settlementTraitLocations.Count == 0)
                        {
                            toRemove.Add(evt);
                        }
                    }
                }
            }

            foreach (FCEvent evt in toRemove)
            {
                faction.events.Remove(evt);
            }
        }
        //only used with the obsolte SOS2 patch.
        // commenting out for now. Should remove for good eventually
        /*
        public static Faction copyPlayerColonyFaction()
        {
            FactionFC worldcomp = FactionCache.FactionComp;

            worldcomp.setCapital();

            FactionDef facDef = new FactionDef();


            facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction();
            faction.def = facDef;
            faction.def.techLevel = worldcomp.factionBackup.def.techLevel;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = worldcomp.factionBackup.colorFromSpectrum;
            faction.Name = worldcomp.factionBackup.Name;
            //faction. = worldcomp.factionBackup.centralMelanin;
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }

            CreatePlayerFactionLeader(faction);

            //LogUtil.Message(Find.FactionManager.AllFactions.Contains(faction).ToString());

            //Find.FactionManager.Add(faction);

            //check if SoS2 is enabled
            if (FCSettings.IsModLoaded("kentington.saveourship2"))
            {
                LogUtil.MessageForce("SoS2 running - planet changed");
                //SoS2 is loaded

                Type typ = GenUtil.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
                Type typ2 = GenUtil.returnUnknownTypeFromName("SaveOurShip2.WorldFactionList");

                // Check if SoS2 classes were found
                // Preview debug - Remove once confirmed ok!
                if (typ == null || typ2 == null)
                {
                    LogUtil.Warning("SoS2 compatibility: Could not find required SoS2 classes. SoS2 may not be loaded or has a different version.");
                }
                else
                {
                    try
                    {
                        var mainclass = Traverse.CreateWithType(typ.ToString());
                        var dict = mainclass.Property("PastWorldTracker").Field("WorldFactions").GetValue();

                        var planetfactiondict = Traverse.Create(dict);
                        var unknownclass = planetfactiondict.Property("Item", new object[] { Find.World.info.name }).GetValue();

                        var factionlist = Traverse.Create(unknownclass);
                        var list = factionlist.Field("myFactions").GetValue();
                        List<String> modifiedlist = (List<String>)list;
                        modifiedlist.Add(faction.GetUniqueLoadID());
                        factionlist.Field("myFactions").SetValue(modifiedlist);
                        //LogUtil.Message("Added faction to world list");
                        // Preview debug - Remove once confirmed ok!
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Warning("SoS2 compatibility: Error adding faction to SoS2 world list: " + ex.Message);
                    }
                }

                foreach (Faction other in Find.FactionManager.AllFactionsVisibleInViewOrder)
                {
                    faction.TryMakeInitialRelationsWith(other);
                }

                Find.FactionManager.Add(faction);
            }


            return faction;
        }*/

        public static Faction createPlayerColonyFaction()
        {
            FactionFC worldcomp = FactionCache.FactionComp;
            if (worldcomp == null)
            {
                LogUtil.Error("FactionFC world component is missing! Cannot create player colony faction.");
                return null;
            }
            LogUtil.Message("Creating new player faction");
            worldcomp.setCapital();

            FactionDef facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction
            {
                def = facDef
            };
            faction.def.techLevel = Faction.OfPlayer.def.techLevel;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
            faction.Name = "PlayerColony".Translate();
            faction.def.classicIdeo = Faction.OfPlayer.def.classicIdeo;
            faction.ideos = Faction.OfPlayer.ideos;

            worldcomp.updateTechLevel(Find.ResearchManager, faction);
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }
            // Set starting goodwill to Player
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 200);

            // Generate Leader
            if (faction.leader == null || faction.leader.Dead)
            {
                CreatePlayerFactionLeader(faction);
            }

            Find.FactionManager.Add(faction);
            worldcomp.OnCreation();
            return faction;
        }

        public static bool CreatePlayerFactionLeader(Faction faction)
        {
            bool success = true;
            if (!faction.TryGenerateNewLeader())
            {
                LogUtil.Message("Generating Leader failed! Manually Generating . . .");
                faction.leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: Faction.OfPlayer.RandomPawnKind(),
                faction: faction, context: PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false, worldPawnFactionDoesntMatter: false));
                if (faction.leader == null)
                {
                    LogUtil.Warning("That failed, too! Contacting " + faction.Name + " won't work!");
                    success = false;
                }
                else
                {
                    if (!Find.WorldPawns.Contains(faction.leader))
                    {
                        Find.WorldPawns.PassToWorld(faction.leader, PawnDiscardDecideMode.KeepForever);
                    }
                    LogUtil.Message($"Created pawn {faction.leader.Name} ({faction.leader.ThingID}) to lead faction {faction.Name}");
                }
            }

            return success;
        }
    }
}
