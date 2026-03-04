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
        public int taxTimeDue = Find.TickManager.TicksGame;
        public int timeStart = Find.TickManager.TicksGame;
        public int uiTimeUpdate;
        public int militaryTimeDue;
        public bool factionCreated;

        private int foundingTick = 0;
        public int FoundingTick => foundingTick;
        private Vector2 startingLongLat = new Vector2();
        public Vector2 StartingLongLat => startingLongLat;

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
        private List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<FCTraitEffectDef> Traits => traits;
        private Dictionary<(string, Operation), double> cachedTraitValues = new Dictionary<(string, Operation), double>();
        private Dictionary<(string, Operation), string> cachedTraitDescs = new Dictionary<(string, Operation), string>();

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

        //Road builder
        public FCRoadBuilder roadBuilder = new FCRoadBuilder();

        //Settlement Leveling
        public int factionLevel = 1;
        public float factionXPCurrent = 0;
        public float factionXPGoal = 100;

        //Random Event
        public float randomEventLastAdded = 0f;

        //Caching
        private bool dirtyGrandThingListFlag = true;
        private List<ThingDef> grandThingList = null;

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
                    map = Find.WorldObjects.SettlementAt(FactionCache.FactionComp.capitalLocation)?.Map;
                    if (map is null)
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
        /// <summary>
        /// Called when the Empire faction is created.
        /// </summary>
        public void OnCreation()
        {
            foundingTick = Find.TickManager.TicksGame;
        }
        public string GetFoundingDate(bool full = true)
        {
            if (full)
            {
                return GenDate.DateFullStringAt(foundingTick, startingLongLat);
            }
            else
            {
                return GenDate.DateShortStringAt(foundingTick, startingLongLat);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref foundingTick, "foundingTick", defaultValue: 0);
            Scribe_Values.Look(ref startingLongLat, "foundingLongLat");
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

            Scribe_Values.Look(ref taxTimeDue, "taxTimeDue");
            Scribe_Values.Look(ref timeStart, "timeStart", -1);
            Scribe_Values.Look(ref uiTimeUpdate, "uiTimeUpdate");
            Scribe_Values.Look(ref militaryTimeDue, "militaryTimeDue", -1);
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

            //Update
            Scribe_Values.Look(ref nextSettlementFCID, "nextSettlementFCID");

            //Military Customization Util
            Scribe_Deep.Look(ref militaryCustomizationUtil, "militaryCustomizationUtil");
            Scribe_Values.Look(ref nextMilitaryFireSupportID, "nextMilitaryFireSupportID", 1);
            Scribe_Values.Look(ref nextUnitId, "nextUnitID", 1);
            Scribe_Values.Look(ref nextSquadId, "nextSquadID", 1);
            Scribe_Values.Look(ref nextMercenaryID, "nextMercenaryID", 1);
            Scribe_Values.Look(ref nextMercenarySquadID, "nextMercenarySquadID", 1);
            Scribe_Values.Look(ref nextPrisonerID, "nextPrisonerID", 1);

            //New Tax Stuff
            Scribe_Values.Look(ref nextTaxID, "nextTaxID", 1);
            Scribe_Values.Look(ref nextBillID, "nextBillID", 1);
            Scribe_Values.Look(ref nextEventID, "nextEventID", 1);


            Scribe_Collections.Look(ref Bills, "Bills", LookMode.Deep);
            Scribe_Collections.Look(ref OldBills, "OldBills", LookMode.Deep);
            Scribe_Values.Look(ref autoResolveBills, "autoResolveBills");

            //Road builder
            Scribe_Deep.Look(ref roadBuilder, "roadBuilder");

            // Legacy trait Scribe_Values removed — state is now in FCPolicyState subclasses,
            // serialized via FCPolicy.ExposeData -> FCPolicyState.ExposeData.

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
                raceFilter.FinalizeInit(this);
            }

            // Initialize xenotype filter
            // The xenotype filter isn't properly loaded until after this function is called, so we don't *actually* want to finalize it yet.
            //   Only finalize it if it doesn't even exist
            if (xenotypeFilter == null)
            {
                LogUtil.Warning("Null xenotypeFilter detected - Creating new one");
                xenotypeFilter = new XenotypeFilter(this);
                xenotypeFilter.FinalizeInit(this);
            }

            // Rebuilt on each load from DefDatabase — intentional, ensures defs stay in sync
            factionResources.Clear();
            foreach (ResourceTypeDef resourceTypeDef in DefDatabase<ResourceTypeDef>.AllDefs)
            {
                factionResources.Add(new ResourceDisplay(resourceTypeDef));
                LogUtil.Message($"Added ResourceDisplay for resourceTypeDef {resourceTypeDef} to FactionFC.factionResources");
            }
            factionResources.Sort(ResourceDisplay.sortForUI);
        }
        /// <summary>
        /// Returns a list of *all* things that this faction can produce.
        /// </summary>
        /// <returns></returns>
        public List<ThingDef> getGrandThingList()
        {
            if (dirtyGrandThingListFlag)
            {
                grandThingList = new List<ThingDef>();
                foreach (WorldSettlementFC settlement in settlements)
                {
                    grandThingList.AddRange(settlement.getGrandThingList());
                }
                grandThingList = grandThingList.Distinct().ToList();
                dirtyGrandThingListFlag = false;
            }
            return grandThingList;
        }
        public void dirtyGrandThingList()
        {
            dirtyGrandThingListFlag = true;
        }
        public List<ThingDef> getStuffListForThingDef(ThingDef thing)
        {
            return CraftUtil.getThingStuffs(thing, getGrandThingList());
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
            FCTraitEffectModExtension traitExt = trait.GetModExtension<FCTraitEffectModExtension>();
            if (traitExt != null)
            {
                try { traitExt.OnAppliedToFaction(this); }
                catch (Exception e) { LogUtil.Error($"FactionFC.addTrait: OnAppliedToFaction threw for '{trait.defName}': {e}"); }
            }
            InvalidateTraitCache();
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
                FCTraitEffectModExtension traitExt = trait.GetModExtension<FCTraitEffectModExtension>();
                if (traitExt != null)
                {
                    try { traitExt.OnRemovedFromFaction(this); }
                    catch (Exception e) { LogUtil.Error($"FactionFC.removeTrait: OnRemovedFromFaction threw for '{trait.defName}': {e}"); }
                }
                InvalidateTraitCache();
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
                FCTraitEffectModExtension traitExt = trait.GetModExtension<FCTraitEffectModExtension>();
                if (traitExt != null)
                {
                    try { traitExt.OnRemovedFromFaction(this); }
                    catch (Exception e) { LogUtil.Error($"FactionFC.clearTraits: OnRemovedFromFaction threw for '{trait.defName}': {e}"); }
                }
            }
            traits.Clear();
            InvalidateTraitCache();
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
            InvalidateTraitCache();
        }
        public double getFieldValue(string field, Operation addOrMultiply)
        {
            double value = 0;
            if (!cachedTraitValues.TryGetValue((field, addOrMultiply), out value))
            { 
                value = TraitUtilsFC.cycleTraits(field, traits, addOrMultiply);
                cachedTraitValues.Add((field, addOrMultiply), value);
            }
            return value;
        }
        public string getFieldDesc(string field, Operation addOrMultiply, bool invert = false, bool hardinvert = false)
        {
            string desc = "";
            if (!cachedTraitDescs.TryGetValue((field, addOrMultiply), out desc))
            {
                TraitUtilsFC.cycleTraits(field, traits, addOrMultiply, true, ref desc, invert, hardinvert);
                cachedTraitDescs.Add((field, addOrMultiply), desc);
            }
            return desc;
        }
        public void InvalidateTraitCache()
        {
            cachedTraitDescs.Clear();
            cachedTraitValues.Clear();
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

        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            Faction faction = FactionCache.PlayerColonyFaction;
            if (firstTick)
            {
                FCSettings.UpdateChanges();

                roadBuilder.FirstTick();

                if (!(faction is null))
                {
                    faction.def.techLevel = TechLevel.Undefined;
                    factionIcon = TexLoad.factionIcons.FirstOrFallback(obj => obj.name == factionIconPath,
                        TexLoad.factionIcons.First());
                    updateFactionIcon(ref faction, "FactionIcons/" + factionIcon.name);
                    factionIconPath = factionIcon.name;
                }

                militaryCustomizationUtil.checkMilitaryUtilForErrors();

                /* Get the longlat of the player's starting location. This will be used when calculating founding dates. */
                Map playerHome = Find.AnyPlayerHomeMap;
                if (playerHome is null)
                {
                    LogUtil.Warning("Found NULL for player map on first tick. This probably shouldn't happen...");
                    startingLongLat = default(Vector2);
                }
                else
                {
                    startingLongLat = Find.WorldGrid.LongLatOf(playerHome.Tile);
                }

                firstTick = false;
            }

            FCEventMaker.ProcessEvents(in events);
            billUtility.processBills();

            FireSupportTick();

            /* Check on the leader */
            //This check used to exist in updateTechLevel(), but it doesn't really seem appropriate there. So, moved it here.
            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
            {
                if (faction != null && (faction.leader == null || faction.leader.Dead))
                {
                    ColonyUtil.CreatePlayerFactionLeader(faction);
                }
            }
            TaxTick(faction);
            UITick(faction);
            StatTick(faction);
            MilitaryTick(faction);
            if (!(faction is null))
            {
                roadBuilder.RoadTick();
                TickActions();
            }
        }

        public void TickActions()
        {
            int tick = Find.TickManager.TicksGame;

            // Dispatch Tick to all active policy/trait extensions
            // (feudal mercenary cooldown, expansionist fee reduction cooldown, mercantile caravans, etc.)
            ForEachPolicyExtension((ext, policy) => ext.Tick(this, policy));
        }

        public void FireSupportTick()
        {
            if (militaryCustomizationUtil.fireSupport == null)
            {
                militaryCustomizationUtil.fireSupport = new List<MilitaryFireSupport>();
            }

            //Other functions
            militaryCustomizationUtil.fireSupport.RemoveAll(support => support.ShouldBeOver);
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
            return traits.ToList();
        }


        public void setStartTime()
        {
            taxTimeDue = Find.TickManager.TicksGame + FCSettings.timeBetweenTaxes;
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

        public void updateTechLevel(ResearchManager researchManager, Faction faction = null)
        {
            bool medievalOnly = FCSettings.medievalTechOnly;
            TechLevel curTechLevel = techLevel;


            if (!medievalOnly && FactionCache.TechLevelBarrierUltra != null &&
                researchManager.GetProgress(FactionCache.TechLevelBarrierUltra) == FactionCache.TechLevelBarrierUltra.baseCost && techLevel < TechLevel.Ultra)
            {
                techLevel = TechLevel.Ultra;
                LogUtil.Message("updateTechLevel: Ultra");
            }
            else if (!medievalOnly && FactionCache.TechLevelBarrierSpacer != null &&
                     researchManager.GetProgress(FactionCache.TechLevelBarrierSpacer) == FactionCache.TechLevelBarrierSpacer.baseCost &&
                     techLevel < TechLevel.Spacer)
            {
                techLevel = TechLevel.Spacer;
                LogUtil.Message("updateTechLevel: Spacer");
            }
            else if (!medievalOnly && FactionCache.TechLevelBarrierIndustrial != null &&
                     researchManager.GetProgress(FactionCache.TechLevelBarrierIndustrial) == FactionCache.TechLevelBarrierIndustrial.baseCost &&
                     techLevel < TechLevel.Industrial)
            {
                techLevel = TechLevel.Industrial;
                LogUtil.Message("updateTechLevel: Industrial");
            }
            else if (FactionCache.TechLevelBarrierMedieval != null &&
                     researchManager.GetProgress(FactionCache.TechLevelBarrierMedieval) == FactionCache.TechLevelBarrierMedieval.baseCost &&
                     techLevel < TechLevel.Medieval)
            {
                techLevel = TechLevel.Medieval;
                LogUtil.Message("updateTechLevel: Medieval");
            }
            else
            {
                if (techLevel < TechLevel.Neolithic)
                {
                    LogUtil.Message("updateTechLevel: Neolithic");
                    techLevel = TechLevel.Neolithic;
                }
            }

            if (techLevel != curTechLevel)
            {
                raceFilter.FinalizeInit(this);
                xenotypeFilter.FinalizeInit(this);
                DirtyAllTitheCaches();
            }

            Faction playerColonyfaction = faction ?? FactionCache.PlayerColonyFaction;
            if (playerColonyfaction != null && playerColonyfaction.def.techLevel < techLevel)
            {
                LogUtil.Message("Updating Tech Level");
                updateFactionDef(techLevel, ref playerColonyfaction);
            }
            else if (playerColonyfaction != null && playerColonyfaction.def.techLevel >= techLevel)
            {
                //LogUtil.Message("Tech Level already matches");
            }
        }

        public void DirtyAllTitheCaches()
        {
            foreach (WorldSettlementFC settlement in settlements)
            {
                foreach (ResourceFC resource in settlement.Resources)
                {
                    resource.setDirtyRandomTitheCache();
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
            if (policies.Count < 2)
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

        // ── Policy Extension Cache ────────────────────────────────

        private List<(FCPolicyModExtension ext, FCPolicy policy)> _cachedPolicyExtensions;
        private List<(FCPolicyModExtension ext, FCPolicy policy)> cachedPolicyExtensions
        {
            get
            {
                if (_cachedPolicyExtensions == null)
                {
                    RebuildPolicyExtensionCache();
                }
                return _cachedPolicyExtensions;
            }
        }

        /// <summary>
        /// Rebuilds the flat cached list of active policy extensions. Call this whenever
        /// policies or factionTraits change (faction creation, level-up trait assignment).
        /// </summary>
        public void RebuildPolicyExtensionCache()
        {
            _cachedPolicyExtensions = new List<(FCPolicyModExtension, FCPolicy)>();
            foreach (FCPolicy p in policies)
            {
                if (p?.def == null) continue;
                foreach (FCPolicyModExtension ext in p.def.PolicyExtensions)
                    _cachedPolicyExtensions.Add((ext, p));
            }
            foreach (FCPolicy p in factionTraits)
            {
                if (p?.def == null || p.def == FCPolicyDefOf.empty) continue;
                foreach (FCPolicyModExtension ext in p.def.PolicyExtensions)
                    _cachedPolicyExtensions.Add((ext, p));
            }
        }

        /// <summary>
        /// Iterates all active policy/trait extensions, calling the action on each.
        /// Uses a cached flat list — no GetModExtension overhead per call.
        /// </summary>
        public void ForEachPolicyExtension(Action<FCPolicyModExtension, FCPolicy> action)
        {
            foreach (var (ext, policy) in cachedPolicyExtensions)
            {
                try
                {
                    action(ext, policy);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"Policy extension error for '{policy.def?.defName}': {e}");
                }
            }
        }

        /// <summary>
        /// Aggregation helper: applies a modifier chain across all active policy extensions.
        /// </summary>
        public double ApplyPolicyModifier(double baseValue, Func<FCPolicyModExtension, double, double> modifier)
        {
            double result = baseValue;
            foreach (var (ext, _) in cachedPolicyExtensions)
            {
                try
                {
                    result = modifier(ext, result);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"Policy modifier error: {e}");
                }
            }
            return result;
        }

        /// <summary>
        /// Aggregation helper for int modifiers.
        /// </summary>
        public int ApplyPolicyModifier(int baseValue, Func<FCPolicyModExtension, int, int> modifier)
        {
            int result = baseValue;
            foreach (var (ext, _) in cachedPolicyExtensions)
            {
                try
                {
                    result = modifier(ext, result);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"Policy modifier error: {e}");
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if any active policy extension blocks the given action.
        /// </summary>
        public bool AnyPolicyBlocks(FCActionType action)
        {
            foreach (var (ext, _) in cachedPolicyExtensions)
                if (ext.BlocksAction(action))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if any active policy extension enables the given action.
        /// </summary>
        public bool AnyPolicyEnables(FCActionType action)
        {
            foreach (var (ext, _) in cachedPolicyExtensions)
                if (ext.EnablesAction(action))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if any active policy extension prevents building destruction on battle loss.
        /// </summary>
        public bool AnyPolicyPreventsBuildingDestruction()
        {
            foreach (var (ext, _) in cachedPolicyExtensions)
                if (ext.PreventBuildingDestruction())
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if any active policy extension suppresses member death penalties.
        /// </summary>
        public bool AnyPolicySuppressesMemberDeathPenalty()
        {
            foreach (var (ext, _) in cachedPolicyExtensions)
                if (ext.SuppressMemberDeathPenalty())
                    return true;
            return false;
        }

        /// <summary>
        /// Returns the state of the first active policy/trait whose state is of type T, or null.
        /// </summary>
        public T GetPolicyState<T>() where T : FCPolicyState
        {
            foreach (var (_, policy) in cachedPolicyExtensions)
                if (policy.state is T state)
                    return state;
            return null;
        }

        public bool sendDiplomaticEnvoy(Faction faction)
        {
            if (faction.def.permanentEnemy)
            {
                Messages.Message("FCCannotImproveRelationsWithType".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            bool handled = false;
            ForEachPolicyExtension((ext, policy) =>
            {
                if (!handled)
                    handled = ext.HandleDiplomaticEnvoy(this, policy, faction);
            });
            return handled;
        }

        public void updateAverages()
        {
            int averageHappinessTmp = 0;
            int averageLoyaltyTmp = 0;
            int averageUnrestTmp = 0;
            int averageProsperityTmp = 0;

            if (settlements.Count > 0)
            {
                foreach (WorldSettlementFC settlement in settlements)
                {
                    averageHappinessTmp += Convert.ToInt32(settlement.happiness);
                    averageLoyaltyTmp += Convert.ToInt32(settlement.loyalty);
                    averageUnrestTmp += Convert.ToInt32(settlement.unrest);
                    averageProsperityTmp += Convert.ToInt32(settlement.prosperity);
                }

                averageHappinessTmp /= settlements.Count;
                averageLoyaltyTmp /= settlements.Count;
                averageUnrestTmp /= settlements.Count;
                averageProsperityTmp /= settlements.Count;
            }

            averageHappiness = averageHappinessTmp;
            averageLoyalty = averageLoyaltyTmp;
            averageUnrest = averageUnrestTmp;
            averageProsperity = averageProsperityTmp;


            if (settlements.Any() && FactionCache.PlayerColonyFaction != null)
            {
                FactionCache.PlayerColonyFaction.TryAffectGoodwillWith(Find.FactionManager.OfPlayer,
                    (Convert.ToInt32(averageHappiness) - FactionCache.PlayerColonyFaction.PlayerGoodwill));
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
            return income;
        }
        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            return upkeep;
        }
        public double getTotalProfit()
        {
            return profit;
        }
        public void updateTotalProfit()
        {
            income = settlements.Sum(s => s.getTotalIncome());
            upkeep = settlements.Sum(s => s.getTotalUpkeep());
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
        public void setDirtyResourceDisplayCache(ResourceTypeDef rdef)
        {
            ResourceDisplay rdisplay = factionResources.Find((ResourceDisplay rd) => rd.resourceDef == rdef);
            if (rdisplay != null)
            {
                rdisplay.setDirtyCache();
            }
        }
        public double getFactionTitheBonusAdditivePerWorker(ResourceTypeDef rdef)
        {
            double bonus = 0;

            return bonus;
        }
        public double getFactionTitheBonusAdditiveForTotal(ResourceTypeDef rdef)
        {
            double bonus = 0;

            return bonus;
        }
        public double getFactionTitheBonusMultPerWorker(ResourceTypeDef rdef)
        {
            double bonus = 1;

            return bonus;
        }
        public double getFactionTitheBonusMultForTotal(ResourceTypeDef rdef)
        {
            return ApplyPolicyModifier(1d, (ext, val) => ext.ModifyTitheMultiplier(val));
        }


        public void addTax()
        {
            foreach (ResourcePool pool in resourcePools)
            {
                if (pool.resource.poolResourceResetsAtTaxTime())
                {
                    pool.pool = 0;
                }
            }

            if (settlements.Count != 0)
            {
                foreach (WorldSettlementFC settlement in settlements)
                {
                    addExperienceToFactionLevel(2f);

                    List<Thing> list = new List<Thing>();
                    int silverAmount = 0;
                    list = settlement.createTax(out silverAmount);
                    List<ResourcePool> resourcePools = settlement.createResourcePools();

                    BillFC bill = new BillFC(settlement);
                    bill.taxes.resourcePools = resourcePools;
                    bill.taxes.itemTithes.AddRange(list);
                    bill.taxes.silverAmount = silverAmount;

                    Bills.Add(bill);

                    TextUtil.GetTownTitle(settlement);
                    TaxTickPrisoner(settlement);
                }

                Find.LetterStack.ReceiveLetter("TaxesBilledShort".Translate(), "TaxesBilledDesc".Translate(),
                    LetterDefOf.PositiveEvent);
                uiUpdate();
            }
            else
            {
                Messages.Message("NoSettlementsToTax".Translate(), MessageTypeDefOf.NeutralEvent);
            }
        }

        public float updateFactionLevelGoalXP(int currentLevel)
        {
            return SettlementFormulas.CalculateFactionLevelGoalXP(currentLevel);
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
            for (int i = 0; i < settlementCaravansList.Count; i++)
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
            ResourceDisplay res = factionResources.Find((ResourceDisplay rfc) => rfc.resourceDef.defName == name);
            if (res == null)
            {
                /* This should never happen! */
                LogUtil.Error($"Requested resource {name} is not in the list of faction resources!");
            }
            return res;
        }

        public ResourceDisplay returnResource(ResourceTypeDef resourceTypeDef)
        {
            ResourceDisplay res = factionResources.Find((ResourceDisplay rfc) => rfc.resourceDef == resourceTypeDef);
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
                Messages.Message(
                    "FCCapitalAlreadyEstablished".Translate(activeCapitalSpot.Map.Parent.LabelCap),
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
                Messages.Message(
                    "FCUnableToSetCapitalHere".Translate(),
                    MessageTypeDefOf.NegativeEvent);
            }
        }

        public int returnCapitalMapId()
        {
            for (int i = 0; i < Find.Maps.Count; i++)
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
            for (int i = 0; i < Find.Maps.Count; i++)
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
            for (int i = 0; i < settlements.Count; i++)
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
            if (faction == null || Find.TickManager.TicksGame < taxTimeDue)
                return;

            addTax();
            taxTimeDue = Find.TickManager.TicksGame + FCSettings.timeBetweenTaxes;

            if (autoResolveBills)
                PaymentUtil.autoresolveBills(Bills);
        }

        public void TaxTickPrisoner(WorldSettlementFC settlement)
        {
            int i = 0;
            while (i < settlement.prisonerList.Count)
            {
                FCPrisoner prisoner = settlement.prisonerList[i];
                bool dead = false;

                switch (prisoner.workload)
                {
                    case FCWorkLoad.Heavy:
                        if (prisoner.AdjustHealth(-20))
                            dead = true;
                        break;
                    case FCWorkLoad.Medium:
                        if (prisoner.AdjustHealth(-10))
                            dead = true;
                        break;
                    case FCWorkLoad.Light:
                        if (prisoner.AdjustHealth(4))
                            dead = true;
                        break;
                }

                /* Only increment if the prisoner hasn't died.
                 * If they *did* die, then AdjustHealth() will have removed them from the list already. So if we increment, then we'll actually skip the next prisoner. */
                if (!dead) i++;
            }
        }

        // resetTraitMercantileCaravanTime removed — mercantile caravan scheduling
        // is now handled by FCPolicyExt_Mercantile.Tick/OnEnacted via FCPolicyState_Mercantile.

        private bool CanMakeRandomEventNow()
        {
            if ((FCSettings.maxDaysTillRandomEvent - FCSettings.minDaysTillRandomEvent) == 0)
            {
                return randomEventLastAdded - FCSettings.minDaysTillRandomEvent <= 0;
            }
            else
            {
                return Rand.Chance((randomEventLastAdded - FCSettings.minDaysTillRandomEvent) / (FCSettings.maxDaysTillRandomEvent - FCSettings.minDaysTillRandomEvent));
            }
        }

        private bool RandomEventsDisabledOrNoSettlements() => FactionCache.FactionComp.settlements.Count == 0 || FCSettings.disableRandomEvents;

        private void MakeRandomEvent()
        {
            if (RandomEventsDisabledOrNoSettlements()) return;

            if (CanMakeRandomEventNow())
            {
                FCEvent tmpEvt = FCEventMaker.MakeRandomEvent(FCEventMaker.returnRandomEvent(), null);
                if (tmpEvt != null)
                {
                    FactionCache.FactionComp.addEvent(tmpEvt);
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
            if (faction == null || Find.TickManager.TicksGame % GenDate.TicksPerDay != 0)
                return;

            updateSettlementStats();
            updateAverages();
            RelationsUtilFC.resetPlayerColonyRelations();
            updateDailyResourcePools();
            MakeRandomEvent();
        }

        public void MilitaryTick(Faction faction)
        {
            if (Find.TickManager.TicksGame >= militaryTimeDue)
            {
                if (faction != null &&
                    FCSettings.disableHostileMilitaryActions == false &&
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
                                    MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemy, true), settlement, enemy);
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
            return !(GetActiveCapitalSpot() is null);
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