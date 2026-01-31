using FactionColonies.util;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static System.Collections.Specialized.BitVector32;

namespace FactionColonies
{
    public class FactionFC : WorldComponent
    {
        public int eventTimeDue;
        public int taxTimeDue = Find.TickManager.TicksGame;
        public int timeStart = Find.TickManager.TicksGame;
        public int uiTimeUpdate;
        public int dailyTimer = Find.TickManager.TicksGame;
        public int militaryTimeDue;
        public int mercenaryTick;
        public bool factionCreated;

        private int nextUnitId;
        private int nextSquadId;

        public int NextUnitID => ++nextUnitId;
        public int NextSquadID => ++nextSquadId;
        /// <summary>
        /// Used by other mods to find our settlements. Move, rename, or otherwise modify at your own peril
        /// </summary>
        public List<WorldSettlementFC> settlements = new List<WorldSettlementFC>();
        public string name = "PlayerFaction".Translate();
        public string title = "Bastion".Translate();
        public double averageHappiness = 100;
        public double averageLoyalty = 100;
        public double averageUnrest;
        public double averageProsperity = 100;
        public double income;
        public double upkeep;
        public double profit;
        public PlanetTile capitalLocation = PlanetTile.Invalid;
        public string capitalPlanet;
        public Map taxMap;
        public TechLevel techLevel = TechLevel.Undefined;
        private bool firstTick = true;
        public bool updateProcessed = false;
        public Texture2D factionIcon = TexLoad.factionIcons[0];
        public string factionIconPath = TexLoad.factionIcons[0].name;


        //New Types of Productions
        public float researchPointPool = 0;
        public List<ResourcePool> resourcePools = new List<ResourcePool>();
        public ThingWithComps powerOutput;

        public List<FCEvent> events = new List<FCEvent>();
        public List<PlanetTile> settlementCaravansList = new List<PlanetTile>(); //list of locations caravans already sent to

        public List<BillFC> OldBills = new List<BillFC>();
        public List<BillFC> Bills = new List<BillFC>();
        public bool autoResolveBills;
        public bool autoResolveBillsChanged = false;

        public List<FCPolicy> policies = new List<FCPolicy>();
        //TODO: nothing should try to modify the traits list directly. Should always go through addTrait/removeTrait/clearTraits/assignNewTraits
        private List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<FCTraitEffectDef> Traits => traits;
        public List<int> militaryTargets = new List<int>();
        public RaceThingFilter raceFilter; // Deprecated, keeping for backwards compatibility
        public XenotypeFilter xenotypeFilter;

        public List<ResourceDisplay> factionResources = new List<ResourceDisplay>();
        public List<ResourceDisplay> FactionResources => factionResources;

        public List<PlanetLayerDef> layersForTilePicker = null;

        //Update
        public int nextSettlementFCID = 1;
        public int nextMercenarySquadID = 1;
        public int nextMercenaryID = 1;
        public int nextTaxID = 1;
        public int nextBillID = 1;
        public int nextEventID = 1;
        public int nextPrisonerID = 1;

        //Military 
        public int nextMilitaryFireSupportID = 1;

        //Military Customization
        public MilitaryCustomizationUtil militaryCustomizationUtil = new MilitaryCustomizationUtil();

        //Sos2 Compatibility
        /*public Faction factionBackup;
        public int travelTime = 0;
        public string planetName;
        public bool boolChangedPlanet;
        public bool factionUpdated;
        public bool SoSMoving = false;
        public bool SoSShipTaxMap;
        public bool SoSShipCapital;
        public bool SoSShipCapitalMoving = false;
        public List<SettlementSoS2Info> createSettlementQueue = new List<SettlementSoS2Info>();
        public List<SettlementSoS2Info> deleteSettlementQueue = new List<SettlementSoS2Info>();*/

        //Road builder
        public FCRoadBuilder roadBuilder = new FCRoadBuilder();

        public int traitMilitaristicTickLastUsedExtraSquad = -1;

        //Traits
        public int traitPacifistTickLastUsedDiplomat = -1;
        public int traitExpansionistTickLastUsedSettlementFeeReduction = -1;
        public bool traitExpansionistBoolCanUseSettlementFeeReduction = true;
        public int traitFeudalTickLastUsedMercenary = -1;
        public bool traitFeudalBoolCanUseMercenary = true;
        public int traitMercantileTradeCaravanTickDue = -1;

        //Settlement Leveling
        public int factionLevel = 1;
        public float factionXPCurrent = 0;
        public float factionXPGoal = 100;

        //Random Event
        public float randomEventLastAdded = 0f;

        public List<FCPolicy> factionTraits = new List<FCPolicy>
        {
            new FCPolicy(FCPolicyDefOf.empty),
            new FCPolicy(FCPolicyDefOf.empty),
            new FCPolicy(FCPolicyDefOf.empty),
            new FCPolicy(FCPolicyDefOf.empty),
            new FCPolicy(FCPolicyDefOf.empty)
        };

        public Map TaxMap
        {
            get
            {
                Map map;
                if (taxMap == null)
                {
                    if (Find.WorldObjects.SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation)?.Map == null)
                    {
                        //if no tax map or no capital map is valid
                        map = Find.CurrentMap.IsPlayerHome ? Find.CurrentMap : Find.AnyPlayerHomeMap;

                        LogUtil.MessageForce(
                            "Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " +
                            map.Parent.LabelCap);
                    }
                    else
                    {
                        //if no tax map or no capital map is valid
                        map = Find.CurrentMap.IsPlayerHome ? Find.CurrentMap : Find.AnyPlayerHomeMap;

                        LogUtil.MessageForce(
                            "Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " +
                            map.Parent.LabelCap);
                    }
                }
                else
                {
                    map = taxMap;
                }

                return map;
            }
        }

        //Research Trading
        public float tradedAmount = 0;

        //Call for aid
        //
        //
        // typeof(FactionDialogMaker), "CallForAid")]
        // class WorldObjectGizmos
        //{
        //    static void Prefix(Map map, Faction faction)
        //    {

        //    }
        // }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref capitalLocation, "capitalLocation");
            Scribe_Values.Look(ref capitalPlanet, "capitalPlanet");
            Scribe_References.Look(ref taxMap, "taxMap");
            Scribe_Values.Look(ref factionCreated, "factionCreated");

            Scribe_Values.Look(ref averageHappiness, "averageHappiness");
            Scribe_Values.Look(ref averageLoyalty, "averageLoyalty");
            Scribe_Values.Look(ref averageUnrest, "averageUnrest");
            Scribe_Values.Look(ref averageProsperity, "averageProsperity");

            Scribe_Values.Look(ref income, "income");
            Scribe_Values.Look(ref upkeep, "upkeep");
            Scribe_Values.Look(ref profit, "profit");

            Scribe_Values.Look(ref eventTimeDue, "eventTimeDue");
            Scribe_Values.Look(ref taxTimeDue, "taxTimeDue");
            Scribe_Values.Look(ref timeStart, "timeStart", -1);
            Scribe_Values.Look(ref uiTimeUpdate, "uiTimeUpdate");
            Scribe_Values.Look(ref militaryTimeDue, "militaryTimeDue", -1);
            Scribe_Values.Look(ref dailyTimer, "dailyTimer");
            Scribe_Values.Look(ref techLevel, "techLevel");
            Scribe_Values.Look(ref factionIconPath, "factionIconPath", "Base");

            Scribe_Collections.Look(ref settlements, "settlements", LookMode.Reference);
            Scribe_Collections.Look(ref policies, "factionPolicies", LookMode.Deep);
            Scribe_Collections.Look(ref events, "events", LookMode.Deep);
            Scribe_Collections.Look(ref settlementCaravansList, "settlementCaravansList", LookMode.Value);
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def);
            Scribe_Collections.Look(ref militaryTargets, "militaryTargets", LookMode.Value);

            //New Producitons types
            Scribe_Collections.Look(ref resourcePools, "resourcePools", LookMode.Deep);
            Scribe_References.Look(ref powerOutput, "powerOutput");

            //save resources
            Scribe_Collections.Look(ref factionResources, "factionResources", LookMode.Deep);

            Scribe_Deep.Look(ref raceFilter, "raceFilter");
            Scribe_Deep.Look(ref xenotypeFilter, "xenotypeFilter");
            Scribe_Values.Look(ref updateProcessed, "updateProcessed", false);
            Scribe_Deep.Look(ref xenotypeFilter, "xenotypeFilter");

            //Update
            Scribe_Values.Look(ref nextSettlementFCID, "nextSettlementFCID");

            //Military Customization Util
            Scribe_Deep.Look(ref militaryCustomizationUtil, "militaryCustomizationUtil");
            Scribe_Values.Look(ref nextMilitaryFireSupportID, "nextMilitaryFireSupportID", 1);
            Scribe_Values.Look(ref nextUnitId, "nextUnitID", 1);
            Scribe_Values.Look(ref nextSquadId, "nextSquadID", 1);
            Scribe_Values.Look(ref nextMercenaryID, "nextMercenaryID", 1);
            Scribe_Values.Look(ref nextMercenarySquadID, "nextMercenarySquadID", 1);
            Scribe_Values.Look(ref mercenaryTick, "mercenaryTick", -1);
            Scribe_Values.Look(ref nextPrisonerID, "nextPrisonerID", 1);

            //New Tax Stuff
            Scribe_Values.Look(ref nextTaxID, "nextTaxID", 1);
            Scribe_Values.Look(ref nextBillID, "nextBillID", 1);
            Scribe_Values.Look(ref nextEventID, "nextEventID", 1);


            Scribe_Collections.Look(ref Bills, "Bills", LookMode.Deep);
            Scribe_Collections.Look(ref OldBills, "OldBills", LookMode.Deep);
            Scribe_Values.Look(ref autoResolveBills, "autoResolveBills");

            //Sos2 compatibility
            //Scribe_Deep.Look<Faction>(ref factionBackup, "factionBackup");
            /*Scribe_Values.Look(ref SoSShipCapital, "SoSShipCapital");
            Scribe_Values.Look(ref SoSShipTaxMap, "SoSShipTaxMap");
            Scribe_Values.Look(ref planetName, "planetName");
            Scribe_Collections.Look(ref createSettlementQueue, "createSettlementQueue", LookMode.Deep);
            Scribe_Collections.Look(ref deleteSettlementQueue, "deleteSettlementQueue", LookMode.Deep);*/

            //Road builder
            Scribe_Deep.Look(ref roadBuilder, "roadBuilder");

            //Traits
            Scribe_Values.Look(ref traitMilitaristicTickLastUsedExtraSquad, "traitMilitaristicTickLastUsedExtraSquad");
            Scribe_Values.Look(ref traitPacifistTickLastUsedDiplomat, "traitPacifistTickLastUsedDiplomat");
            Scribe_Values.Look(ref traitExpansionistTickLastUsedSettlementFeeReduction,
                "traitExpansionistTickLastUsedSettlementFeeReduction");
            Scribe_Values.Look(ref traitExpansionistBoolCanUseSettlementFeeReduction,
                "traitExpansionistBoolCanUseSettlementReduction");
            Scribe_Values.Look(ref traitFeudalTickLastUsedMercenary, "traitFeudalTickLastUsedMercenary");
            Scribe_Values.Look(ref traitFeudalBoolCanUseMercenary, "traitFeudalBoolCanUseMercenary");
            Scribe_Values.Look(ref traitMercantileTradeCaravanTickDue, "traitMercantileTradeCaravanTickDue");

            //Settlement Leveling
            Scribe_Values.Look(ref factionLevel, "factionLevel");
            Scribe_Values.Look(ref factionXPCurrent, "factionXPCurrent");
            Scribe_Values.Look(ref factionXPGoal, "factionXPGoal");
            Scribe_Collections.Look(ref factionTraits, "factionTraits", LookMode.Deep);

            //Research Trading
            Scribe_Values.Look(ref tradedAmount, "tradedAmount");

            //Random Event
            Scribe_Values.Look(ref randomEventLastAdded, "randomEventLastAddedTick");
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            
            //Just in case null is saved somehow
            if (raceFilter == null)
            {
                LogUtil.Message("Null raceFilter detected - Recreating");
                raceFilter = new RaceThingFilter(this);
            }
            raceFilter.FinalizeInit(this);

            // Initialize xenotype filter
            if (xenotypeFilter == null)
            {
                LogUtil.Message("Null xenotypeFilter detected - Creating new one");
                xenotypeFilter = new XenotypeFilter(this);
            }
            xenotypeFilter.FinalizeInit(this);

            //TODO: seems this will refresh every time the game is loaded. Might be a problem. Keep an eye on this
            factionResources.Clear();
            foreach (ResourceTypeDef resourceTypeDef in DefDatabase<ResourceTypeDef>.AllDefs)
            {
                factionResources.Add(new ResourceDisplay(resourceTypeDef));
                LogUtil.Message($"Added ResourceDisplay for resourceTypeDef {resourceTypeDef} to FactionFC.factionResources");
            }
            factionResources.Sort(ResourceDisplay.sortForUI);
        }

        public void addTrait(FCTraitEffectDef trait, string id = "")
        {
            if (trait.appliesToSettlements())
            {
                foreach(WorldSettlementFC settlement in settlements)
                {
                    settlement.addTrait(trait, id);
                }
            }
            traits.Add(trait);
        }
        public void addTraits(List<FCTraitEffectDef> traits, string id = "")
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                addTrait(trait, id);
            }
        }
        public bool removeTrait(FCTraitEffectDef trait, string id = "")
        {
            if (traits.Contains(trait))
            {
                if (trait.appliesToSettlements())
                {
                    foreach(WorldSettlementFC settlement in settlements)
                    {
                        settlement.removeTrait(trait, id);
                    }
                }
                return traits.Remove(trait);
            }
            else
            {
                return false;
            }
        }
        public void removeTraits(List<FCTraitEffectDef> traits, string id = "")
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                removeTrait(trait, id);
            }
        }
        public void clearTraits()
        {
            foreach (FCTraitEffectDef trait in traits)
            {
                if (trait.appliesToSettlements())
                {
                    foreach(WorldSettlementFC settlement in settlements)
                    {
                        settlement.removeTrait(trait);
                    }
                }
            }
            traits.Clear();
        }
        /// <summary>
        /// This function completely replaces the faction's current list of traits with the provided list.
        /// </summary>
        /// <param name="traits"></param>
        /// <param name="id"></param>
        public void assignNewTraitList(List<FCTraitEffectDef> traits, string id = "")
        {
            clearTraits();
            addTraits(traits, id);
        }

        public void GainHappiness(double amount)
        {
            foreach (WorldSettlementFC settlement in settlements)
            {
                settlement.GainHappiness(amount);
            }
        }

        public void GainUnrestForReason(Message msg, double amount)
        {
            Messages.Message(msg);
            foreach (WorldSettlementFC settlement in settlements)
            {
                settlement.GainUnrest(amount);
            }
        }

        // Fix a crash related to a harmony bug on Linux
        // This gets all patches Empire makes, gets the ones that would crash on Linux, and fixes them
        static void FixLinuxHarmonyCrash(Harmony harmony)
        {
            bool WouldCrash(MethodInfo method)
            {
                if (method == null || !method.IsVirtual || method.IsAbstract || method.IsFinal)
                {
                    return false;
                }

                byte[] bytes = method.GetMethodBody()?.GetILAsByteArray();
                if (bytes == null || bytes.Length == 0 || (bytes.Length == 1 && bytes.First() == 0x2A))
                {
                    return true;
                }
                return false;
            }

            var methods = typeof(FactionFC).Assembly.GetTypes().Where(t0 => t0 != null && t0.IsClass && !typeof(Delegate).IsAssignableFrom(t0) && t0.GetCustomAttributes(typeof(HarmonyPatch)).Any()).SelectMany(t1 =>
            {
                HarmonyPatch patch = (HarmonyPatch)Attribute.GetCustomAttribute(t1, typeof(HarmonyPatch));
                MethodInfo[] m = patch?.info?.declaringType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (m == null) return new List<MethodInfo>();

                return m.Where(met => met.Name == patch.info.methodName);
            }).Where(WouldCrash);

            foreach (MethodInfo i in methods)
            {
                // Patching methods without any Prefixes/Postfixes before actually patching them fixes it. Idk why
                harmony.Patch(i);
            }
        }

        //CallForAid
        //Remove ability to attack colony.


        public FactionFC(World world) : base(world)
        {
            var harmony = new Harmony("com.Saakra.Empire");

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                FixLinuxHarmonyCrash(harmony);
            }

            harmony.PatchAll();

            //SOS2 patches are obsolete
            //TODO: are there even any harmony patches left? maybe just remove the harmony code entirely? Less code = less bugs, after all
            /*if (FCSettings.IsModLoaded("kentington.saveourship2"))
            {
                LogUtil.MessageForce("Starting SoS2 patch...");
                SoS2HarmonyPatches.Patch(harmony);
            }*/

            /*if (FCSettings.IsModLoaded("Krkr.AndroidTiers") || FCSettings.IsModLoaded("Atlas.AndroidTiers"))
            {
                //TODO: do we still need this patch?
                //Android_Tiers_Patches.Patch(harmony);
            }*/
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (firstTick)
            {
                FCSettings.UpdateChanges();

                roadBuilder.FirstTick();

                Faction FCf = ColonyUtil.getPlayerColonyFaction();
                if (FCf != null)
                {
                    FCf.def.techLevel = TechLevel.Undefined;
                    factionIcon = TexLoad.factionIcons.FirstOrFallback(obj => obj.name == factionIconPath,
                        TexLoad.factionIcons.First());
                    updateFactionIcon(ref FCf, "FactionIcons/" + factionIcon.name);
                    factionIconPath = factionIcon.name;
                }

                militaryCustomizationUtil.checkMilitaryUtilForErrors();

                firstTick = false;
            }

            FCEventMaker.ProcessEvents(in events);
            billUtility.processBills();

            FireSupportTick();


            //If Player Colony Faction does exists
            Faction faction = ColonyUtil.getPlayerColonyFaction();
            /* Always call the tick functions, but pass faction into them.
             * We always need to update the interval, even if the faction doesn't exist. Otherwise, if the player delays in creating the faction,
             * then we'll suddenly hit them with a billion back-taxes and back-events as the timers try to catch up.
             * Question: why even worry about skipping time? What's the point? Is it a debugging tool? Seems ripe for bugs and errors.
             */
            TaxTick(faction);
            UITick(faction);
            StatTick(faction);
            MilitaryTick(faction);
            if (faction != null)
            {
                roadBuilder.RoadTick();
                TickActions();
            }
        }

        public void TickActions()
        {
            int tick = Find.TickManager.TicksGame;
            // settlements are all worldobjects now, which tick automatically
            /*foreach (WorldSettlementFC settlement in settlements)
            {
                settlement.Tick(tick);
            }*/

            //Feudal
            if (traitFeudalBoolCanUseMercenary == false &&
                (traitFeudalTickLastUsedMercenary + GenDate.TicksPerSeason) <= Find.TickManager.TicksGame)
            {
                traitFeudalBoolCanUseMercenary = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionMercenaryRefreshed".Translate(), LetterDefOf.PositiveEvent);
            }

            //Expansionist
            if (traitExpansionistBoolCanUseSettlementFeeReduction == false &&
                (traitExpansionistTickLastUsedSettlementFeeReduction + GenDate.TicksPerYear) <=
                Find.TickManager.TicksGame)
            {
                traitExpansionistBoolCanUseSettlementFeeReduction = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionSettlementFeeReduction".Translate(), LetterDefOf.PositiveEvent);
            }

            //Mercantile
            if (hasTrait(FCPolicyDefOf.mercantile) && traitMercantileTradeCaravanTickDue <= Find.TickManager.TicksGame)
            {
                IncidentWorker_TraderCaravanArrival worker = new IncidentWorker_TraderCaravanArrival();
                worker.def = IncidentDefOf.TraderCaravanArrival;
                IncidentParms parms =
                    StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, returnCapitalMap());
                parms.faction = ColonyUtil.getPlayerColonyFaction();
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, (Map)parms.target,
                    CellFinder.EdgeRoadChance_Friendly);
                parms.spawnRotation = Rot4.FromAngleFlat((((Map)parms.target).Center - parms.spawnCenter).AngleFlat);
                if (parms.spawnCenter.IsValid)
                    worker.TryExecute(parms);
                else
                    LogUtil.Warning("Mercantile - Spawn Center not valid");


                resetTraitMercantileCaravanTime();
            }
        }

        public void FireSupportTick()
        {
            if (militaryCustomizationUtil.fireSupport == null)
            {
                militaryCustomizationUtil.fireSupport = new List<MilitaryFireSupport>();
            }

            //Other functions
            militaryCustomizationUtil.fireSupport = militaryCustomizationUtil.fireSupport.Where(support => !support.ShouldBeOver).ToList();
            militaryCustomizationUtil.fireSupport.ForEach(support => support.Process());
        }


        public int GetNextSettlementFCID()
        {
            nextSettlementFCID++;
            //LogUtil.Message("Returning next settlement FC ID " + nextSettlementFCID);

            return nextSettlementFCID;
        }

        public int GetNextMercenaryID()
        {
            nextMercenaryID++;
            //LogUtil.Message("Returning next mercenary ID " + nextMercenaryID);
            return nextMercenaryID;
        }

        public int GetNextMilitaryFireSupportID()
        {
            nextMilitaryFireSupportID++;
            //LogUtil.Message("Returning next MilitaryFireSupportID " + nextSquadID);

            return nextMilitaryFireSupportID;
        }

        public int GetNextMercenarySquadID()
        {
            nextMercenarySquadID++;
            //LogUtil.Message("Returning next MercenarySquadID " + nextMercenarySquadID);

            return nextMercenarySquadID;
        }

        public int GetNextTaxID()
        {
            nextTaxID++;
            return nextTaxID;
        }

        public int GetNextEventID()
        {
            nextEventID++;
            return nextEventID;
        }

        public int GetNextBillID()
        {
            nextBillID++;
            return nextBillID;
        }

        public int GetNextPrisonerID()
        {
            nextPrisonerID++;
            return nextPrisonerID;
        }

        public List<FCTraitEffectDef> returnListFactionTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }

            return tmpList;
        }


        public void setStartTime()
        {   
            taxTimeDue = Find.TickManager.TicksGame + FCSettings.timeBetweenTaxes;
            dailyTimer = Find.TickManager.TicksGame + 2000;
        }

        public int returnHighestMilitaryLevel()
        {
            int max = 1;
            foreach (WorldSettlementFC settlement in settlements)
            {
                max = Math.Max(max, settlement.settlementMilitaryLevel);
            }

            return max;
        }

        public void updateFactionRaces()
        {
            Faction faction = ColonyUtil.getPlayerColonyFaction();
            // TODO updateFactionRaces()
        }

        public string returnNextTechToLevel()
        {
            switch (techLevel)
            {
                case TechLevel.Ultra:
                    return "ReachedMaxLevel".Translate();
                case TechLevel.Spacer:
                    return "FCShipBasics".Translate();
                case TechLevel.Industrial:
                    return "FCFabrication".Translate();
                case TechLevel.Medieval:
                    return "FCElectricity".Translate();
                case TechLevel.Neolithic:
                    return "FCSmithing".Translate();
                default:
                    return "N/A";
            }
        }

        public void updateTechLevel(ResearchManager researchManager)
        {
            bool medievalOnly = FCSettings.medievalTechOnly;


            if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false) != null &&
                researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false)) ==
                DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false).baseCost && techLevel < TechLevel.Ultra)
            {
                techLevel = TechLevel.Ultra;
                LogUtil.Message("updateTechLevel: Ultra");
                raceFilter.FinalizeInit(this);
            }
            else if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false).baseCost &&
                     techLevel < TechLevel.Spacer)
            {
                techLevel = TechLevel.Spacer;
                LogUtil.Message("updateTechLevel: Spacer");
                raceFilter.FinalizeInit(this);
            }
            else if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false).baseCost &&
                     techLevel < TechLevel.Industrial)
            {
                techLevel = TechLevel.Industrial;
                LogUtil.Message("updateTechLevel: Industrial");
                raceFilter.FinalizeInit(this);
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false).baseCost &&
                     techLevel < TechLevel.Medieval)
            {
                techLevel = TechLevel.Medieval;
                LogUtil.Message("updateTechLevel: Medieval");
                raceFilter.FinalizeInit(this);
                xenotypeFilter.FinalizeInit(this);
            }
            else
            {
                if (techLevel < TechLevel.Neolithic)
                {
                    LogUtil.Message("updateTechLevel: Neolithic");
                    techLevel = TechLevel.Neolithic;
                    raceFilter.FinalizeInit(this);
                    xenotypeFilter.FinalizeInit(this);
                }
            }

            Faction playerColonyfaction = ColonyUtil.getPlayerColonyFaction();
            if (playerColonyfaction != null && playerColonyfaction.def.techLevel < techLevel)
            {
                LogUtil.Message("Updating Tech Level");
                updateFactionDef(techLevel, ref playerColonyfaction);
            }
            else if (playerColonyfaction.def.techLevel >= techLevel)
            {
                //LogUtil.Message("Tech Level already matches");
            }
            // Check Leader
            if (playerColonyfaction != null)
            {
                if (playerColonyfaction.leader == null || playerColonyfaction.leader.Dead)
                {
                    ColonyUtil.CreatePlayerFactionLeader(playerColonyfaction);
                }
            }
        }

        //TODO: this whole function is playing with defs. Doesn't seem great. Not sure if there's another way to set icons, though. Need to investigate
        public void updateFactionIcon(ref Faction faction, string iconPath)
        {
            LogUtil.Message("Updated Icon - " + iconPath);
            if (faction?.def != null)
            {
                faction.def.factionIconPath = iconPath;
            }
            if (settlements.Any() && settlements[0]?.def != null)
            {
                //TODO: not sure if this will interact wierdly with the new SettlementDef. Keep an eye on this
                WorldSettlementFC.traitCachedIcon.SetValue(settlements[0].def, ContentFinder<Texture2D>.Get(iconPath));
            }

            foreach (WorldSettlementFC settlement in settlements)
            {
                if (settlement?.def != null)
                {
                    settlement.def.expandingIconTexture = iconPath;
                }
                if (settlement?.Faction?.def != null)
                {
                    settlement.Faction.def.factionIconPath = iconPath;
                }
            }
        }

        public void updateFactionDef(TechLevel tech, ref Faction faction)
        {
            FactionDef replacingDef;
            ThingFilter apparelStuffFilter = new ThingFilter();
            FactionDef def = faction.def;

            switch (tech)
            {
                case TechLevel.Archotech:
                case TechLevel.Ultra:
                case TechLevel.Spacer:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");

                    break;
                case TechLevel.Industrial:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");
                    break;
                case TechLevel.Medieval:
                    if (FCSettings.IsModLoaded("OskarPotocki.VanillaFactionsExpanded.MedievalModule"))
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("VFEM_KingdomCivil");
                    }
                    else
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    }

                    break;
                default:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    break;
            }
            //LogUtil.Message("FactionFC.updateFactionDef - switch(tech) passed");
            def.caravanTraderKinds = replacingDef.caravanTraderKinds;
            if (replacingDef.backstoryFilters != null && replacingDef.backstoryFilters.Count != 0)
                def.backstoryFilters = replacingDef.backstoryFilters;
            def.techLevel = tech;
            def.basicMemberKind = replacingDef.basicMemberKind;
            def.visitorTraderKinds = replacingDef.visitorTraderKinds;
            def.baseTraderKinds = replacingDef.baseTraderKinds;
            if (replacingDef.apparelStuffFilter != null)
                def.apparelStuffFilter = replacingDef.apparelStuffFilter;


            if (tech >= TechLevel.Spacer && def.apparelStuffFilter != null)
            {
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Synthread"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Hyperweave"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Plasteel"), true);
            }
            updateFactionIcon(ref faction, "FactionIcons/" + factionIconPath);

            LogUtil.Message("FactionFC.updateFactionDef - Completed tech update");
        }

        public bool hasPolicy(FCPolicyDef def)
        {
            //Don't game the system
            if (policies.Count() < 2)
            {
                return false;
            }

            foreach (FCPolicy policy in policies)
            {
                if (policy.def == def)
                    return true;
            }

            return false;
        }

        public bool hasTrait(FCPolicyDef def)
        {
            foreach (FCPolicy trait in factionTraits)
            {
                if (trait.def == def)
                    return true;
            }

            return false;
        }

        public bool sendDiplomaticEnvoy(Faction faction)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();

            if (!faction.def.permanentEnemy)
            {
                if (Find.TickManager.TicksGame >=
                    (factionfc.traitPacifistTickLastUsedDiplomat + GenDate.TicksPerDay * 5))
                {
                    factionfc.traitPacifistTickLastUsedDiplomat = Find.TickManager.TicksGame;
                    int random = Rand.Range(1, 10);
                    if (random > 5)
                    {
                        int relationImprovement = Rand.Range(5, 15);
                        faction.TryAffectGoodwillWith(Find.FactionManager.OfPlayer, relationImprovement);
                        Find.LetterStack.ReceiveLetter("FCRelationImproved".Translate(),
                            "FCRelationImprovedText".Translate(faction.Name, relationImprovement),
                            LetterDefOf.PositiveEvent);
                    }
                    else
                    {
                        Find.LetterStack.ReceiveLetter("FCRelationNotImproved".Translate(),
                            "FCFailedToImproveRelationship".Translate(faction.Name), LetterDefOf.NeutralEvent);
                    }

                    return true;
                }

                Messages.Message(
                    "XDaysToSendDiplomat".Translate(Math.Round(
                        ((factionfc.traitPacifistTickLastUsedDiplomat + GenDate.TicksPerDay * 5) -
                         Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
                return false;
            }

            Messages.Message("FCCannotImproveRelationsWithType".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        public void resetRaceFilter()
        {
            raceFilter = new RaceThingFilter(this);
            raceFilter.FinalizeInit(this);
        }

        public void resetXenotypeFilter()
        {
            xenotypeFilter = new XenotypeFilter(this);
            xenotypeFilter.FinalizeInit(this);
        }

        public void updateAverages()
        {
            int averageHappinessTmp = 0;
            int averageLoyaltyTmp = 0;
            int averageUnrestTmp = 0;
            int averageProsperityTmp = 0;

            if (settlements.Count() > 0)
            {
                foreach (WorldSettlementFC settlement in settlements)
                {
                    averageHappinessTmp += Convert.ToInt32(settlement.happiness);
                    averageLoyaltyTmp += Convert.ToInt32(settlement.loyalty);
                    averageUnrestTmp += Convert.ToInt32(settlement.unrest);
                    averageProsperityTmp += Convert.ToInt32(settlement.prosperity);
                }

                averageHappinessTmp /= settlements.Count();
                averageLoyaltyTmp /= settlements.Count();
                averageUnrestTmp /= settlements.Count();
                averageProsperityTmp /= settlements.Count();
            }

            averageHappiness = averageHappinessTmp;
            averageLoyalty = averageLoyaltyTmp;
            averageUnrest = averageUnrestTmp;
            averageProsperity = averageProsperityTmp;


            if (settlements.Any() && ColonyUtil.getPlayerColonyFaction() != null)
            {
                ColonyUtil.getPlayerColonyFaction().TryAffectGoodwillWith(Find.FactionManager.OfPlayer,
                    (Convert.ToInt32(averageHappiness) - ColonyUtil.getPlayerColonyFaction().PlayerGoodwill));
            }
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void addSettlement(WorldSettlementFC settlement)
        {
            settlements.Add(settlement);
            uiUpdate();
        }

        public void uiUpdate()
        {
            //Pop UI updates
            // We cache the total amount now, and signal to dirty the cache anytime an underlying value is changed. No need to update regularly
            //updateTotalResources();
            updateTotalProfit();
            updateTechLevel(Find.ResearchManager);
        }

        public double getTotalIncome() //return total income of settlements       ####MAKE UPDATE PER HOUR TICK
        {
            double income = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                income += settlements[i].getTotalIncome();
            }

            return income;
        }


        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            double upkeep = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                upkeep += settlements[i].getTotalUpkeep();
            }

            return upkeep;
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of all settlements
        {
            return getTotalIncome() - getTotalUpkeep();
        }

        public void updateTotalProfit()
        {
            income = getTotalIncome();
            upkeep = getTotalUpkeep();
            profit = income - upkeep;
        }

        /* * * * *
         * Resource Pools
         * * * * * */
        public void addResourcePool(ResourcePool pool)
        {
            if (pool == null)
            {
                return;
            }
            else if (pool.pool == 0)
            {
                return;
            }
            /* If the pool wants to do any pre-adding-to-global-pool shenanigans, let it do so now. */
            pool.pool = pool.resource.preAddToGlobalPool(pool.pool);

            ResourcePool rpool = resourcePools.Find((ResourcePool p) => p.resource == pool.resource);
            if (rpool != null)
            {
                rpool.pool += pool.pool;
            }
            else
            {
                resourcePools.Add(pool);
            }
            pool.resource.addedToGlobalPool(pool.pool);
        }
        public void addResourcePools(List<ResourcePool> pools)
        {
            foreach(ResourcePool pool in pools)
            {
                addResourcePool(pool);
            }
        }
        public double getResourcePoolValue(ResourceTypeDef res)
        {
            ResourcePool rpool = resourcePools.Find((ResourcePool p) => p.resource == res);
            if (rpool == null)
            {
                LogUtil.Warning($"Tried to get resource pool value for ResourceTypeDef {res}, but there was no faction resource pool");
                return 0;
            }
            return rpool.pool;
        }

        public IEnumerable<FloatMenuOption> GetFactionMenuResourcePoolFloatMenuOptions()
        {
            foreach(ResourcePool pool in resourcePools)
            {
                IEnumerable<FloatMenuOption> options = pool.resource.GetFactionMenuFloatMenuOptions(pool);
                if (options != null)
                {
                    foreach (FloatMenuOption option in options)
                    {
                        yield return option;
                    }
                }
            }
        }
        public void updateDailyResourcePools()
        {
            foreach(ResourcePool pool in resourcePools)
            {
                LogUtil.Message($"Daily ResourcePool update for resourceTypeDef {pool.resource.defName}. Pool size: {pool.pool}");
                pool.resource.dailyUpdate(pool);
                LogUtil.Message($"Post-Daily ResourcePool update for resourceTypeDef {pool.resource.defName}. New Pool size: {pool.pool}");
            }
        }

        /* * * * *
         * End Resource Pool functions
         * * * * * */

        /*public void updateTotalResources()
        {
            foreach (ResourceDisplay rdisplay in factionResources)
            {
                int resource = 0;

                for (int k = 0; k < settlements.Count(); k++)
                {
                    resource += (int)(settlements[k].getResource(rdisplay.resourceDef)?.totalProduction ?? 0);
                }

                rdisplay.amount = resource;
            }
        }*/
        public void setDirtyResourceDisplayCache(ResourceTypeDef rdef)
        {
            ResourceDisplay rdisplay = factionResources.Find((ResourceDisplay rd) => rd.resourceDef == rdef);
            if (rdisplay != null)
            {
                rdisplay.setDirtyCache();
            }
        }


        public void addTax(bool isUpdating)
        {
            //if (capitalLocation == -1)
            //{
            //    setCapital();
            //}
            foreach (ResourcePool pool in resourcePools)
            {
                if (pool.resource.poolResourceResetsAtTaxTime())
                {
                    pool.pool = 0;
                }
            }

            if (settlements.Count != 0) //if settlements is not zero
            {
                foreach (WorldSettlementFC settlement in settlements)
                {
                    //Start Traits
                    addExperienceToFactionLevel(2f);


                    float trait_Industrious_TaxPercentageBoost = 1;
                    if (hasTrait(FCPolicyDefOf.industrious))
                    {
                        int num = Rand.RangeInclusive(1, 20);
                        if (num == 5)
                        {
                            trait_Industrious_TaxPercentageBoost = 1f + (Rand.RangeInclusive(20, 50) / 100f);
                            Find.LetterStack.ReceiveLetter("FCIdustriousTaxBoost".Translate(),
                                "FCIndustriousPop".Translate(settlement.Name,
                                    ((trait_Industrious_TaxPercentageBoost - 1f) * 100f) + "%"),
                                LetterDefOf.PositiveEvent);
                        }
                    }


                    //End Traits

                    List<Thing> list = new List<Thing>();
                    settlement.updateProfitAndProduction();
                    list = settlement.createTithe(trait_Industrious_TaxPercentageBoost);
                    List<ResourcePool> resourcePools = settlement.createResourcePools();

                    BillFC bill = new BillFC(settlement); //Create new bill connected to settlement
                    bill.taxes.resourcePools = resourcePools;
                    bill.taxes.itemTithes.AddRange(list); //Add tithe to bill's tithes
                    bill.taxes.silverAmount =
                        Convert.ToInt32((settlement.totalIncome * trait_Industrious_TaxPercentageBoost) -
                                        settlement.totalUpkeep) + settlement.returnSilverIncome(true);
                    Bills.Add(bill);

                    TextUtil.GetTownTitle(settlement);
                    TaxTickPrisoner(settlement);
                }


                if (!isUpdating) //if done updating (timeskip) then send goods/silver etc
                {
                    //Messages.Message("TaxesBilled".Translate() + "!", MessageTypeDefOf.PositiveEvent);
                    Find.LetterStack.ReceiveLetter("Taxes Billed", "Taxes from your settlements have been billed",
                        LetterDefOf.PositiveEvent);
                    uiUpdate();

                    //Messages.Message(Find.TickManager.TicksGame.ToString(), MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                Messages.Message("NoSettlementsToTax".Translate(), MessageTypeDefOf.NeutralEvent);
            }
        }

        public float updateFactionLevelGoalXP(int currentLevel)
        {
            float newGoal = 100 + (currentLevel * 150);
            return newGoal;
        }

        public bool addExperienceToFactionLevel(float xp)
        {
            bool leveled = false;
            factionXPCurrent += xp;

            while (factionXPCurrent >= factionXPGoal)
            {
                factionXPCurrent -= factionXPGoal;
                factionLevel += 1;
                Find.LetterStack.ReceiveLetter("FCFactionLevelUp".Translate(),
                    "FCFactionLevelUpDesc".Translate(name, factionLevel), LetterDefOf.PositiveEvent);
                leveled = true;
                factionXPGoal = updateFactionLevelGoalXP(factionLevel);
            }

            return leveled;
        }

        public void addEvent(FCEvent fcevent)
        {
            if (fcevent == null) return;
            //Add event to events
            events.Add(fcevent);

            LogUtil.Message($"addEvent: adding new fcevent {fcevent.def.defName}");

            //check if event has a location, if does, add traits to that specific location;
            if (fcevent.settlementTraitLocations.Count() > 0) //if has specific locations
            {
                foreach (WorldSettlementFC location in fcevent.settlementTraitLocations)
                {
                    location.addTraits(fcevent.def.traits);
                    foreach (FCTraitEffectDef trait in fcevent.def.traits)
                    {
                        //LogUtil.Message(trait.label);
                    }
                }
            }
            else
            {
                //if no specific location then faction wide
                addTraits(fcevent.traits);
            }
        }

        public bool checkSettlementCaravansList(PlanetTile location) //list of destinations caravans gone to
        {
            for (int i = 0; i < settlementCaravansList.Count(); i++)
            {
                if (location == settlementCaravansList[i] || Find.WorldGrid.IsNeighbor(location, settlementCaravansList[i]))
                {
                    return true; // is on list
                }
            }

            return false; //is not on list
        }

        public ResourceDisplay returnResource(string name) //used to return the correct resource based on string name
        {
            ResourceDisplay res = factionResources.Where((ResourceDisplay rfc) => rfc.resourceDef.defName == name).FirstOrDefault();
            if (res == null)
            {
                /* This should never happen! */
                LogUtil.Error($"Requested resource {name} is not in the list of faction resources!");
            }
            return res;
        }

        public ResourceDisplay returnResource(ResourceTypeDef resourceTypeDef)
        {
            ResourceDisplay res = factionResources.Where((ResourceDisplay rfc) => rfc.resourceDef == resourceTypeDef).FirstOrDefault();
            if (res == null)
            {
                /* This should never happen! */
                LogUtil.Error($"Requested resource {resourceTypeDef.defName} is not in the list of faction resources!");
            }
            return res;
        }

        public void setCapital()
        {
            // Check if there's an active capital spot first
            Building_CapitalSpot activeCapitalSpot = GetActiveCapitalSpot();
            if (activeCapitalSpot != null)
            {
                //TODO: Localization key
                Messages.Message(
                    $"Empire capital is already established at {activeCapitalSpot.Map.Parent.LabelCap}. Disable the capital seat there first if you want to move it.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            if (Find.CurrentMap != null && Find.CurrentMap.IsPlayerHome)
            {
                capitalLocation = Find.CurrentMap.Parent.Tile;

                Messages.Message("SetAsFactionCapital".Translate(Find.CurrentMap.Parent.LabelCap),
                    MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                //TODO: Localization key
                Messages.Message(
                    "Unable to set faction capital on this map. Please go to your capital map and use the Set Capital button or build a Capital Seat.",
                    MessageTypeDefOf.NegativeEvent);
            }
        }

        public int returnCapitalMapId()
        {
            for (int i = 0; i < Find.Maps.Count(); i++)
            {
                if (Find.Maps[i].Tile == capitalLocation)
                {
                    return i;
                }
            }

            LogUtil.Message("CouldNotFindMapOfCapital".Translate());
            return -1;
        }

        public Map returnCapitalMap()
        {
            for (int i = 0; i < Find.Maps.Count(); i++)
            {
                if (Find.Maps[i].Tile == capitalLocation)
                {
                    return Find.Maps[i];
                }
            }

            LogUtil.Message("CouldNotFindMapOfCapital".Translate());
            return null;
        }

        public WorldSettlementFC returnSettlementByLocation(PlanetTile location)
        {
            for (int i = 0; i < settlements.Count(); i++)
            {
                if (settlements[i].Tile == location)
                {
                    return settlements[i];
                }
            }

            return null;
        }

        public string getSettlementName(PlanetTile location)
        {
            return returnSettlementByLocation(location)?.Name ?? "Null";
        }

        public void updateSettlementStats()
        {
            foreach (WorldSettlementFC settlement in settlements)
            {
                settlement.updateHappiness();
                settlement.updateLoyalty();
                settlement.updateUnrest();
                settlement.updateProsperity();
            }
        }

        public void TaxTick(Faction faction)
        {
            if (Find.TickManager.TicksGame >= taxTimeDue) // taxTimeDue being used as set interval when skipping time
            {
                int maxIterations = 100; // Safety limit to prevent infinite loops
                int iterations = 0;

                if (faction == null)
                {
                    int timeBetweenTaxes = FCSettings.timeBetweenTaxes;
                    taxTimeDue += timeBetweenTaxes;
                    return;
                }


                while (Find.TickManager.TicksGame >= taxTimeDue && iterations < maxIterations) //while updating events
                {
                    iterations++;
                    //update events in this order: regular events: tax events.

                    if (Find.TickManager.TicksGame > taxTimeDue)
                    {
                        addTax(true);
                    }
                    else
                    {
                        LogUtil.Message(
                            "TaxTick - Catching Up - Did you skip time? Report this if you did not");
                        addTax(false);
                        //NOT WHERE FINAL UPDATE IS. Go to addTax Function
                    }
                    
                    taxTimeDue += FCSettings.timeBetweenTaxes;
                    //LogUtil.Message(Find.TickManager.TicksGame + " vs " + taxTimeDue + " - Taxing");
                }
                
                if (iterations >= maxIterations)
                {
                    LogUtil.Error($"TaxTick: Hit maximum iteration limit ({maxIterations}), breaking out of loop to prevent freeze. Current tick: {Find.TickManager.TicksGame}, taxTimeDue: {taxTimeDue}");
                    // Force advance taxTimeDue to break the loop
                    taxTimeDue = Find.TickManager.TicksGame + GenDate.TicksPerDay;
                }

                //if Autoresolve bills on, attempt to autoresolve
                switch (autoResolveBills)
                {
                    case true:
                        PaymentUtil.autoresolveBills(Bills);
                        break;
                    case false:
                        break;
                }
            }
        }

        public void TaxTickPrisoner(WorldSettlementFC settlement)
        {
        Reset:
            foreach (FCPrisoner prisoner in settlement.prisonerList)
            {
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Heavy:
                        if (prisoner.AdjustHealth(-20))
                            goto Reset;
                        break;
                    case FCWorkLoad.Medium:
                        if (prisoner.AdjustHealth(-10))
                            goto Reset;
                        break;
                    case FCWorkLoad.Light:
                        if (prisoner.AdjustHealth(4))
                            goto Reset;
                        break;
                }
            }
        }

        public void resetTraitMercantileCaravanTime()
        {
            float days = Rand.RangeInclusive(3, 5);
            traitMercantileTradeCaravanTickDue = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
        }

        private bool CanMakeRandomEventNow() => Rand.Chance((randomEventLastAdded - FCSettings.minDaysTillRandomEvent) / (FCSettings.maxDaysTillRandomEvent - FCSettings.minDaysTillRandomEvent));

        private bool RandomEventsDisabledOrNoSettlements() => Find.World.GetComponent<FactionFC>().settlements.Count == 0 || FCSettings.disableRandomEvents;

        private void MakeRandomEvent()
        {
            if (RandomEventsDisabledOrNoSettlements()) return;

            if (CanMakeRandomEventNow())
            {
                FCEvent tmpEvt = FCEventMaker.MakeRandomEvent(FCEventMaker.returnRandomEvent(), null);
                if (tmpEvt != null)
                {
                    Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);
                    randomEventLastAdded = 0f;

                    //letter code
                    string settlementString = tmpEvt.settlementTraitLocations.Join((settlement) => $" {settlement.Name}", "\n");

                    if (!settlementString.NullOrEmpty())
                    {
                        Find.LetterStack.ReceiveLetter("Random Event", $"{tmpEvt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                    }
                    else
                    {
                        Find.LetterStack.ReceiveLetter("Random Event", tmpEvt.def.desc,
                            LetterDefOf.NeutralEvent);
                    }
                }
                else
                {
                    randomEventLastAdded += 1f;
                }
            }
            else
            {
                randomEventLastAdded += 1f;
            }

        }

        public void StatTick(Faction faction)
        {
            if (Find.TickManager.TicksGame >= dailyTimer) // taxTimeDue being used as set interval when skipping time
            {
                while (Find.TickManager.TicksGame >= dailyTimer) //while updating events
                {
                    if (faction != null)
                    {
                        //update events in this order: regular events: tax events.
                        updateSettlementStats();
                        updateAverages();
                        RelationsUtilFC.resetPlayerColonyRelations();

                        updateDailyResourcePools();

                        //Random event creation
                        MakeRandomEvent();
                    }

                    dailyTimer += GenDate.TicksPerDay;
                }
            }
        }

        public void MilitaryTick(Faction faction)
        {
            if (Find.TickManager.TicksGame >= militaryTimeDue)
            {
                if (faction != null &&
                    FCSettings.disableHostileMilitaryActions == false &
                    Find.TickManager.TicksGame > (timeStart + GenDate.TicksPerSeason))
                {
                    //if military actions not disabled or game has not passed through the first season
                    //LogUtil.Message("Mil Action debug");


                    //if settlements exist

                    // get list of settlements

                    //if not underattack, add to list

                    //create weight list by settlement military level

                    //choose random

                    if (settlements.Any())
                    {
                        //if settlements exist
                        List<WorldSettlementFC> targets = new List<WorldSettlementFC>();
                        foreach (WorldSettlementFC settlement in settlements)
                        {
                            //create weight list of settlements
                            if (settlement.MilitaryComp?.isUnderAttack != true)
                            {
                                //if not underattack, add to list
                                //get weightvalue of target
                                int weightValue;
                                switch (settlement.settlementMilitaryLevel)
                                {
                                    case 0:
                                    case 1:
                                        weightValue = 10;
                                        break;
                                    case 2:
                                    case 3:
                                        weightValue = 7;
                                        break;
                                    case 4:
                                    case 5:
                                        weightValue = 3;
                                        break;
                                    default:
                                        weightValue = 1;
                                        break;
                                }

                                for (int k = 0; k < weightValue; k++)
                                {
                                    targets.Add(settlement);
                                }
                            }
                        }

                        if (targets.Any())
                        {
                            //List created, pick from list
                            Faction enemy = Find.FactionManager.RandomEnemyFaction();
                            if (enemy != null)
                            {
                                WorldSettlementFC settlement = targets.RandomElementWithFallback();

                                if (settlement != null)
                                {
                                    MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemy, true), targets.RandomElement(), enemy);
                                }
                            }

                        }
                    }
                }

                militaryTimeDue = Find.TickManager.TicksGame + (GenDate.TicksPerDay * FCSettings.minMaxDaysTillMilitaryAction.RandomInRange);
                //LogUtil.Message(militaryTimeDue + " - " + Find.TickManager.TicksGame);
                //LogUtil.Message((militaryTimeDue - Find.TickManager.TicksGame) / 60000 + " days till next military action");
                //militaryTimeDue =
            }
        }


        public void UITick(Faction faction)
        {
            if (uiTimeUpdate <= 0) //update per time?
            {
                uiTimeUpdate = FCSettings.updateUiTimer;

                if (faction != null)
                {
                    //already built in ui update -.-
                    Find.WindowStack.WindowsUpdate();

                    //Pop UI updates
                    uiUpdate();
                }
            }
            else
            {
                uiTimeUpdate -= 1;
            }
        }

        public bool HasActiveCapitalSpot()
        {
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_CapitalSpot capitalSpot && capitalSpot.IsActiveCapitalSpot)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Building_CapitalSpot GetActiveCapitalSpot()
        {
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome) continue;
                
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building is Building_CapitalSpot capitalSpot && capitalSpot.IsActiveCapitalSpot)
                    {
                        return capitalSpot;
                    }
                }
            }
            return null;
        }
    }
}