using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Def for WorldSettlementFC objects.
    /// </summary>
    public class WorldSettlementDef : WorldObjectDef
    {
        public List<ResourceAvailability> resources = new List<ResourceAvailability>();
        /// <summary>
        /// If true, all ResourceTypeDefs with isDefaultResource set to true are automatically added to this settlement's resources list
        /// (unless already explicitly listed). Explicit entries take priority over defaults.
        /// </summary>
        public bool defaultResources = false;
        public int workersMaxBase = 0;
        public int workersMaxMult = 3;
        public int workersUltraMaxBase = 5;
        public int workersUltraMaxMult = 0;
        public List<BiomeDef> blockedBiomes = new List<BiomeDef>();
        public List<BiomeDef> allowedBiomes = new List<BiomeDef>();

        public List<FCStatModifier> statModifiers = new List<FCStatModifier>();
        /// <summary>
        /// If a biomeResourceOverride is specified, then the settlement will use the resources of the given override rather than the resources
        /// of the biome of the tile that it's on.
        /// </summary>
        public BiomeResourceDef biomeResourceOverride;

        public List<ResearchProjectDef> researchProjects = new List<ResearchProjectDef>();
        public TechLevel techLevel = TechLevel.Undefined;

        public List<PlanetLayerDef> planetLayers = new List<PlanetLayerDef>();

        /// <summary>
        /// Optional parent settlement type for inheritance-aware allow/block list checks on buildings.
        /// When a BuildingFCDef's allow/block list is checked, the chain of baseSettlementType references
        /// is walked upward, so subtypes automatically match their parent type.
        /// </summary>
        public WorldSettlementDef baseSettlementType;

        public int maxSettlementLevel = 99;
        public int maxBuildingCount = 99;

        /// <summary>
        /// Optional key used for settlement-type-specific town titles.
        /// When set, GetTownTitle tries FCTitle_{titleKey}_{resourceDefName}_{level} first,
        /// falling back to FCTitle_{resourceDefName}_{level} if the type-specific key doesn't exist.
        /// </summary>
        public string titleKey;

        /// <summary>
        /// Entirely flavor. Determines whether time to create is labeled in menus as "Construction Time" or "Travel Time".
        /// </summary>
        public bool isConstructed = false;

        public Color? accentColor;

        /// <summary>
        /// If false, this settlement type will not appear in the settlement type picker.
        /// Submods can XML-patch this to false to hide settlement types.
        /// </summary>
        public bool available = true;

        public ResourceAvailability GetSettlementResource(ResourceTypeDef resourceTypeDef)
        {
            return resources.FirstOrDefault((ResourceAvailability b) => b.resourceDef == resourceTypeDef);
        }
        public List<ResourceTypeDef> GetResourceDefs()
        {
            List<ResourceTypeDef> list = new List<ResourceTypeDef>();
            if (resources != null)
            {
                foreach (ResourceAvailability rb in resources)
                {
                    list.Add(rb.resourceDef);
                }
            }
            return list;
        }
        public List<string> GetResourceDefNames()
        {
            List<string> list = new List<string>();
            if (resources != null)
            {
                foreach (ResourceAvailability rb in resources)
                {
                    list.Add(rb.resourceDef.defName);
                }
            }
            return list;
        }
        public SettlementTypeExtension GetSettlementTypeExtension()
        {
            return GetModExtension<SettlementTypeExtension>();
        }

        public bool IsUnlocked()
        {
            if (!available) return false;
            if (researchProjects?.Count > 0)
            {
                foreach (ResearchProjectDef researchProject in researchProjects)
                {
                    if (!researchProject.IsFinished)
                    {
                        return false;
                    }
                }
            }
            if (techLevel != TechLevel.Undefined)
            {
                Faction faction = FactionCache.PlayerColonyFaction;
                if (faction.def.techLevel < techLevel)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetCreationTime(PlanetTile tile)
        {
            return GetModExtension<SettlementTypeExtension>().GetCreationTime(tile);
        }
        public int GetCreationCost()
        {
            return GetModExtension<SettlementTypeExtension>().GetCreationCost();
        }
        public PlanetTile GetTileForSettlement(PlanetTile tile)
        {
            return GetModExtension<SettlementTypeExtension>().GetTileForSettlement(tile);
        }
        public TaxDeliveryMode GetTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)
        {
            return GetModExtension<SettlementTypeExtension>().GetTaxDeliveryMode(canUseShuttle, sourceTile);
        }
        public bool IsInList(List<WorldSettlementDef> deflist)
        {
            if (deflist.Contains(this))
            {
                return true;
            }
            else if (!(baseSettlementType is null))
            {
                return baseSettlementType.IsInList(deflist);
            }
            return false;
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (defaultResources)
            {
                foreach (ResourceTypeDef rtd in DefDatabase<ResourceTypeDef>.AllDefs)
                {
                    if (rtd.isDefaultResource && !resources.Any(rb => rb.resourceDef == rtd))
                    {
                        resources.Add(new ResourceAvailability { resourceDef = rtd });
                    }
                }
            }
            foreach (ResourceAvailability rb in resources)
            {
                if (double.IsNaN(rb.additive))
                {
                    rb.additive = 0;
                }
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            IEnumerable<string> errors = base.ConfigErrors();
            if (errors != null)
            {
                foreach (string error in errors)
                {
                    yield return error;
                }
            }

            if (resources != null)
            {
                foreach (ResourceAvailability rb in resources)
                {
                    if (resources.Any((ResourceAvailability b) => b != rb && b.resourceDef == rb.resourceDef))
                    {
                        yield return "ResourceTypeDef " + rb.resourceDef.defName + " is listed multiple times in WorldSettlmentDef " + defName;
                    }
                }
            }
            if (blockedBiomes?.Count > 0 && allowedBiomes?.Count > 0)
            {
                yield return "WorldSettlementDef " + defName + "specifies both blocked Biomes and allowed Biomes. Only one list should be specified";
            }
            if (GetModExtension<SettlementTypeExtension>() == null)
            {
                yield return "WorldSettlementDef " + defName + " does not specify a SettlementTypeExtension_Base";
            }
            if (baseSettlementType != null)
            {
                HashSet<WorldSettlementDef> visited = new HashSet<WorldSettlementDef> { this };
                WorldSettlementDef current = baseSettlementType;
                while (current != null)
                {
                    if (!visited.Add(current))
                    {
                        yield return "WorldSettlementDef " + defName + " has a circular baseSettlementType reference involving " + current.defName;
                        break;
                    }
                    current = current.baseSettlementType;
                }
            }
            foreach (string err in FCStatModifier.ConfigErrors(statModifiers, defName))
                yield return err;
        }
    }

    [DefOf]
    public static class WorldSettlementDefOf
    {
        public static WorldSettlementDef WorldSettlementDef_Surface;
        public static WorldSettlementDef WorldSettlementDef_Orbital;
        static WorldSettlementDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WorldSettlementDefOf));
        }
    }
}
