using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Def for WorldSettlementFC objects.
    /// </summary>
    public class WorldSettlementDef : WorldObjectDef
    {
        public List<ResourceBonuses> resources = new List<ResourceBonuses>();
        public int workersMaxBase = 0;
        public int workersMaxMult = 3;
        public int workersUltraMaxBase = 5;
        public int workersUltraMaxMult = 0;
        public List<BiomeDef> blockedBiomes = new List<BiomeDef>();
        public List<BiomeDef> allowedBiomes = new List<BiomeDef>();

        public List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        /// <summary>
        /// If a biomeResourceOverride is specified, then the settlement will use the resources of the given override rather than the resources
        /// of the biome of the tile that it's on.
        /// </summary>
        public BiomeResourceDef biomeResourceOverride;

        public List<ResearchProjectDef> researchProjects = new List<ResearchProjectDef>();
        public TechLevel techLevel = TechLevel.Undefined;

        /// <summary>
        /// Entirely flavor. Determines whether time to create is labeled in menus as "Construction Time" or "Travel Time".
        /// </summary>
        public bool isConstructed = false;

        public ResourceBonuses getSettlementResource(ResourceTypeDef resourceTypeDef)
        {
            return resources.Where((ResourceBonuses b) => b.resourceDef == resourceTypeDef).FirstOrDefault();
        }
        public List<ResourceTypeDef> getResourceDefs()
        {
            List<ResourceTypeDef> list = new List<ResourceTypeDef>();
            if (resources != null)
            {
                foreach (ResourceBonuses rb in resources)
                {
                    list.Add(rb.resourceDef);
                }
            }
            return list;
        }
        public List<string> getResourceDefNames()
        {
            List<string> list = new List<string>();
            if (resources != null)
            {
                foreach (ResourceBonuses rb in resources)
                {
                    list.Add(rb.resourceDef.defName);
                }
            }
            return list;
        }
        public SettlementTypeExtension getSettlementTypeExtension()
        {
            return GetModExtension<SettlementTypeExtension>();
        }

        public bool isUnlocked()
        {
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
                Faction faction = ColonyUtil.getPlayerColonyFaction();
                if (faction.def.techLevel < techLevel)
                {
                    return false;
                }
            }
            return true;
        }

        public int getCreationTime(PlanetTile tile)
        {
            return GetModExtension<SettlementTypeExtension>().getCreationTime(tile);
        }
        public int getCreationCost()
        {
            return GetModExtension<SettlementTypeExtension>().getCreationCost();
        }
        public PlanetTile getTileForSettlement(PlanetTile tile)
        {
            return GetModExtension<SettlementTypeExtension>().getTileForSettlement(tile);
        }
        public TaxDeliveryMode getTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)
        {
            return GetModExtension<SettlementTypeExtension>().getTaxDeliveryMode(canUseShuttle, sourceTile);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            IEnumerable<string> errors = base.ConfigErrors();
            if (errors != null)
            {
                foreach(string error in errors)
                {
                    yield return error;
                }
            }

            if (resources != null)
            {
                foreach (ResourceBonuses rb in resources)
                {
                    if (resources.Any((ResourceBonuses b) => b != rb && b.resourceDef == rb.resourceDef))
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
