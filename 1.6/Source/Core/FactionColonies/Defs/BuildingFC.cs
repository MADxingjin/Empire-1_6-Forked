using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class BuildingFCDef : Def
    {
        public string desc;
        public double cost;
        public int constructionDuration;
        public TechLevel techLevel = TechLevel.Undefined;
        public List<FCTraitEffectDef> traits;
        public List<string> applicableBiomes = new List<string>();
        public int upkeep;
        public string iconPath = "GUI/unrest";
        public Texture2D iconLoaded;
        public bool requiresRoyality = false;
        public bool requiresIdeology = false;
        public List<string> requiredModsID = new List<string>();
        public List<WorldSettlementDef> settlementTypeBlockList = new List<WorldSettlementDef>();
        public List<WorldSettlementDef> settlementTypeAllowList = new List<WorldSettlementDef>();
        public Hilliness minhilliness = Hilliness.Undefined;
        public Hilliness maxhilliness = Hilliness.Undefined;
        /// <summary>
        /// Determines if the building can be built directly from the building window, into an empty building slot.
        /// </summary>
        public bool baseBuilding = true;
        /// <summary>
        /// A list of buildings that this building can upgrade into.
        /// </summary>
        public List<BuildingFCDef> upgrades = new List<BuildingFCDef>();

        private bool didCacheBuildingAttributeDesc = false;
        private TaggedString cachedBuildingAttributeDesc = "";

        public TaggedString AttributeDesc
        {
            get
            {
                if (!didCacheBuildingAttributeDesc)
                {
                    cachedBuildingAttributeDesc = "";
                    if (traits?.Count > 0)
                    {
                        foreach (FCTraitEffectDef trait in traits)
                        {
                            cachedBuildingAttributeDesc += "\n" + trait.traitBonusDesc;
                        }
                    }
                    cachedBuildingAttributeDesc = cachedBuildingAttributeDesc.Trim();
                    didCacheBuildingAttributeDesc = true;
                }
                return cachedBuildingAttributeDesc;
            }
        }

        public Texture2D Icon
        {
            get
            {
                if (iconLoaded != null) return iconLoaded;
                
                if (!iconPath.NullOrEmpty()) 
                {
                    iconLoaded = ContentFinder<Texture2D>.Get(iconPath);
                } 
                else
                {
                    LogUtil.Error("Failed to load icon for building: " + LabelCap + " at " + (iconPath ?? "nullPath") + "!");
                    iconLoaded = TexLoad.questionmark;
                }
                return iconLoaded;
            }
        }

        public bool RequiredModsLoaded => (ModsConfig.RoyaltyActive || !requiresRoyality) && (ModsConfig.IdeologyActive || !requiresIdeology) && requiredModsID.TrueForAll(mod => ModsConfig.IsActive(mod));
        public bool CanBeBuiltForSettlementType(WorldSettlementDef settlement)
        {
            bool meetsSettlementTypeRequirement = true;
            if (settlementTypeBlockList?.Count > 0)
            {
                if (settlementTypeBlockList.Contains(settlement))
                {
                    meetsSettlementTypeRequirement = false;
                }
            }
            if (settlementTypeAllowList?.Count > 0)
            {
                //If we have an allowlist, then the default restriction is false
                meetsSettlementTypeRequirement = false;
                if (settlementTypeAllowList.Contains(settlement))
                {
                    meetsSettlementTypeRequirement = true;
                }
            }
            return meetsSettlementTypeRequirement;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
            {
                yield return err;
            }
            if (settlementTypeAllowList?.Count > 0 && settlementTypeBlockList?.Count > 0)
            {
                yield return $"BuildingFCDef {defName} has both a settlementTypeAllowList and a settlementTypeBlockList";
            }
        }
    }

    [DefOf]
    public class BuildingFCDefOf
    {
        public static BuildingFCDef Empty;
        public static BuildingFCDef Construction;
        public static BuildingFCDef artilleryOutpost;
        public static BuildingFCDef shuttlePort;

        static BuildingFCDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BuildingFCDefOf));
    }
    public static class FCRoadsDef
    {
        public static RoadDef DirtRoad;
        public static RoadDef DirtPath;        

        public static object RoadDef { get; internal set; }
    }

    public class BuildingFC : IExposable
    {
        public BuildingFCDef def;
        public BuildingFCDef underConstructionDef = BuildingFCDefOf.Empty;
        public int startedTick;
        public int completionTick;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "buildingdef");
            Scribe_Defs.Look(ref underConstructionDef, "underConstructionDef");
            Scribe_Values.Look(ref startedTick, "startedtick");
            Scribe_Values.Look(ref completionTick, "completionTick");
        }
    }
}
