using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
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
        //public required research

        private bool didCacheBuildingDesc = false;
        private TaggedString cachedBuildingDesc = "";

        public TaggedString Desc
        {
            get
            {
                if (!didCacheBuildingDesc)
                {
                    cachedBuildingDesc += desc;
                    if (upkeep != 0)
                    {
                        cachedBuildingDesc += "\n\n" + "FCBuildingUpkeep".Translate(upkeep.ToString());
                    }
                    if (traits?.Count > 0)
                    {
                        cachedBuildingDesc += "\n\n--------------------\n";
                        foreach (FCTraitEffectDef trait in traits)
                        {
                            cachedBuildingDesc += "\n" + trait.traitBonusDesc;
                        }
                    }
                    didCacheBuildingDesc = true;
                }
                return cachedBuildingDesc;
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
    }

    public enum SettlementTypeRestriction
    {
        None,           // Available to all settlement types
        SurfaceOnly,    // Only available to surface settlements
        OrbitalOnly     // Only available to orbital platforms
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
