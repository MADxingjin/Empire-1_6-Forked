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
        private static Faction playerFactionRef = null;
        public static Faction GetVanillaPlayerFaction()
        {
            if (playerFactionRef == null)
            {
                playerFactionRef = Find.FactionManager.AllFactions.ToList().Find(faction => faction.IsPlayer);
            }

            return playerFactionRef;
        }

        public static Faction getPlayerColonyFaction()
        {
            return Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PColony"));
        }


        //<DevAdd>   Create new seperate function to create a faction
        public static WorldSettlementFC createPlayerColonySettlement(int tile, bool createWorldObject, string planetName)
        {
            //Log.Message("boop");
            StringBuilder reason = new StringBuilder();
            if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
            {
                //Log.Message("Invalid Tile");
                //Alert Error to User
                Messages.Message(reason.ToString(), MessageTypeDefOf.NegativeEvent);


                return null;
                //create alert with reason
                //AlertsReadout alert = new AlertsReadout()
            }

            //Log.Message("Colony is being created");
            Faction faction = getPlayerColonyFaction();

            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            if (!worldcomp.settlements.Any())
            {
                Find.World.GetComponent<FactionFC>().timeStart = Find.TickManager.TicksGame;
            }

            //Log.Message(faction.Name);

            SettlementFC settlementfc;
            WorldSettlementFC settlement = null;
            if (createWorldObject)
            {
                settlementfc = new SettlementFC(getName(faction), tile);
                settlement = (WorldSettlementFC)WorldObjectMaker.MakeWorldObject(
                    DefDatabase<WorldObjectDef>.GetNamed("FactionBaseGenerator"));
                settlement.Tile = tile;

                List<String> used = new List<string>();
                List<Settlement> settlements = Find.WorldObjects.Settlements;
                foreach (Settlement found in settlements)
                {
                    used.Add(found.Name);
                }

                settlement.settlement = settlementfc;
                settlement.Name =
                    NameGenerator.GenerateName(faction.def.factionNameMaker, used, true);

                settlement.SetFaction(faction);
                Find.WorldObjects.Add(settlement);
                settlementfc.worldSettlement = settlement;
            }
            else
            {
                settlementfc = new SettlementFC("Settlement", tile);
            }

            //create settlement data for world object
            settlementfc.power.isTithe = true;
            settlementfc.power.isTitheBool = true;
            settlementfc.research.isTithe = true;
            settlementfc.research.isTitheBool = true;
            settlementfc.planetName = planetName;
            if (worldcomp.hasPolicy(FCPolicyDefOf.militaristic))
                settlementfc.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
            if (worldcomp.hasPolicy(FCPolicyDefOf.authoritarian))
                settlementfc.loyalty = 70;
            if (worldcomp.hasPolicy(FCPolicyDefOf.egalitarian))
                settlementfc.happiness = 60;
            if (worldcomp.hasPolicy(FCPolicyDefOf.expansionist) && settlementfc.settlementLevel == 1)
                settlementfc.upgradeSettlement();

            worldcomp.addSettlement(settlementfc);
            if (createWorldObject)
            {
                worldcomp.roadBuilder.FlagUpdateRoadQueues();
            }

            Find.LetterStack.ReceiveLetter("FCSettlementFormed".Translate(),
                "TheSettlement".Translate() + " " + settlementfc.name + "HasBeenFormed".Translate() + "!",
                LetterDefOf.PositiveEvent);

            //Example to grab settlement data from FC
            //Log.Message(settlementfc.ReturnFCSettlement().Name.ToString());


            return settlement;
        }

        private static readonly List<string> usedNames = new List<string>();

        private static string getName(Faction faction)
        {
            if (faction?.def.settlementNameMaker == null)
            {
                return "Settlement";
            }

            RulePackDef rulePack = faction.def.settlementNameMaker;
            usedNames.Clear();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int index = 0; index < settlements.Count; ++index)
            {
                Settlement settlement = settlements[index];
                if (settlement.Name != null)
                    usedNames.Add(settlement.Name);
            }

            return NameGenerator.GenerateName(rulePack, usedNames, true);
        }

        public static void removePlayerSettlement(SettlementFC settlement)
        {
            settlement.PrepareDestroyWorldObject();
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            faction.settlements.Remove(settlement);
            Messages.Message("SettlementRemoved".Translate(settlement.name), MessageTypeDefOf.NegativeEvent);

            if (Find.World.info.name == settlement.planetName)
            {
                Find.WorldObjects.Remove(Find.World.worldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>
                    .GetNamed("FactionBaseGenerator"), settlement.mapLocation));
            }
            else
            {
                faction.deleteSettlementQueue.Add(new SettlementSoS2Info(settlement.planetName,
                    settlement.mapLocation));
            }

            //clear military events
            settlement.returnMilitary(false);

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
                        evt.militaryForceDefending.homeSettlement.cooldownMilitary();

                        toRemove.Add(evt);
                    }
                }


                //settlement event removal
                if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy ||
                    evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
                {
                    if (evt.source == settlement.mapLocation)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def.isRandomEvent && evt.settlementTraitLocations.Count() > 0)
                {
                    if (evt.settlementTraitLocations.Contains(settlement))
                    {
                        evt.settlementTraitLocations.Remove(settlement);
                        if (evt.settlementTraitLocations.Count() == 0)
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

        public static Faction copyPlayerColonyFaction()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();

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

            //faction.GenerateNewLeader();
            faction.TryGenerateNewLeader();

            //Log.Message(Find.FactionManager.AllFactions.Contains(faction).ToString());

            //Find.FactionManager.Add(faction);

            //check if SoS2 is enabled
            if (FCSettings.IsModLoaded("kentington.saveourship2"))
            {
                Log.Message("SoS2 running - planet changed");
                //SoS2 is loaded

                Type typ = GenUtil.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
                Type typ2 = GenUtil.returnUnknownTypeFromName("SaveOurShip2.WorldFactionList");

                // Check if SoS2 classes were found
                // Preview debug - Remove once confirmed ok!
                if (typ == null || typ2 == null)
                {
                    Log.Warning("Empire - SoS2 compatibility: Could not find required SoS2 classes. SoS2 may not be loaded or has a different version.");
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
                        //Log.Message("Added faction to world list");
                        // Preview debug - Remove once confirmed ok!
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Empire - SoS2 compatibility: Error adding faction to SoS2 world list: " + ex.Message);
                    }
                }

                foreach (Faction other in Find.FactionManager.AllFactionsVisibleInViewOrder)
                {
                    faction.TryMakeInitialRelationsWith(other);
                }

                Find.FactionManager.Add(faction);
            }


            return faction;
        }

        public static Faction createPlayerColonyFaction()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            if (worldcomp == null)
            {
                Log.Error("FactionFC world component is missing! Cannot create player colony faction.");
                return null;
            }
            //Log.Message("Creating new faction");
            //Set start time for world component to start tracking your faction;
            worldcomp.setCapital();

            //Log.Message("Faction is being created");
            FactionDef facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction
            {
                def = facDef
            };
            faction.def.techLevel = Faction.OfPlayer.def.techLevel;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
            faction.Name = "PlayerColony".Translate();
            //faction.centralMelanin = Rand.Value;
            faction.def.classicIdeo = Faction.OfPlayer.def.classicIdeo;
            faction.ideos = Faction.OfPlayer.ideos;
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }
            // Set starting goodwill to Player
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 200);

            // Generate Leader
            if (!faction.TryGenerateNewLeader())
            {
                Log.Message("Generating Leader failed! Manually Generating . . .");
                faction.leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: Faction.OfPlayer.RandomPawnKind(),
                faction: faction, context: PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false, worldPawnFactionDoesntMatter: false));
                if (faction.leader == null)
                {
                    Log.Warning("That failed, too! Contacting " + faction.Name + " won't work!");
                }
            }
            worldcomp.factionBackup = faction;
            Find.FactionManager.Add(faction);

            Find.World.GetComponent<FactionFC>().updateTechLevel(Find.ResearchManager);
            return faction;
        }

        public static void ChangePlayerColonyFaction(Faction faction)
        {
            faction = createPlayerColonyFaction();
            Log.Message("Faction was updated - " + faction.Name);
        }
    }
}
