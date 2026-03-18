using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Represents an entry in a flattened upgrade tree.
    /// </summary>
    public struct BuildingUpgradeEntry
    {
        public BuildingFCDef def;
        public int depth;
        public BuildingFCDef parent;
    }

    public class BuildingFCDef : Def
    {
        public string desc;
        public double cost;
        public int constructionDuration;
        public TechLevel techLevel = TechLevel.Undefined;
        public List<FCStatModifier> statModifiers = new List<FCStatModifier>();
        public List<string> applicableBiomes = new List<string>();
        public int upkeep;
        public string iconPath = "GUI/unrest";
        public Texture2D iconLoaded;
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
        /// <summary>
        /// Buildings that must already be built in the settlement before this building can be constructed.
        /// </summary>
        public List<BuildingFCDef> requiredBuildings = new List<BuildingFCDef>();

        private bool didCacheBuildingAttributeDesc = false;
        private TaggedString cachedBuildingAttributeDesc = "";

        public TaggedString AttributeDesc
        {
            get
            {
                if (!didCacheBuildingAttributeDesc)
                {
                    cachedBuildingAttributeDesc = FCStatModifier.GetDescription(statModifiers);
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
            if (HasCycle(this, d => d.upgrades))
            {
                yield return $"BuildingFCDef {defName} has a circular reference in its upgrades chain";
            }
            if (HasCycle(this, d => d.requiredBuildings))
            {
                yield return $"BuildingFCDef {defName} has a circular reference in its requiredBuildings chain";
            }
            foreach (string err in FCStatModifier.ConfigErrors(statModifiers, defName))
                yield return err;
        }

        private static bool HasCycle(BuildingFCDef start, Func<BuildingFCDef, List<BuildingFCDef>> getChildren)
        {
            HashSet<BuildingFCDef> visited = new HashSet<BuildingFCDef>();
            Stack<BuildingFCDef> stack = new Stack<BuildingFCDef>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                BuildingFCDef current = stack.Pop();
                if (!visited.Add(current) && current == start) return true;
                List<BuildingFCDef> children = getChildren(current);
                if (children == null) continue;
                foreach (BuildingFCDef child in children)
                {
                    if (child == start) return true;
                    if (!visited.Contains(child)) stack.Push(child);
                }
            }
            return false;
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
