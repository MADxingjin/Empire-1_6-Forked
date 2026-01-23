using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class BiomeResourceDef : Def
    {
        /* The SettlementDef should determine what resources are available to the settlement, not the biome.
         * Biomes should support all resources by default.
         * If a resource isn't specified in the BiomeResourceDef, then treat it as a default 1 additive, 1 multiplier resource. */
        public List<ResourceBonuses> resources = new List<ResourceBonuses>();
        public bool canSettle;
        public List<ResourceTypeDef> resourceBlockList = new List<ResourceTypeDef>();

        public ResourceBonuses getBiomeResource(ResourceTypeDef resourceTypeDef)
        {
            /* First check if the resource is even allowed in this biome */
            if (resourceBlockList.Contains(resourceTypeDef))
            {
                return null;
            }
            if (!resourceTypeDef.resourceAllowedForBiome(this))
            {
                return null;
            }
            ResourceBonuses res = resources.Find((ResourceBonuses rb) => rb.resourceDef == resourceTypeDef);
            if (res == null)
            {
                /* If the resource isn't explicitly mentioned in the BiomeResourceDef, and it isn't on the blocklist (and this biome
                 * isn't on that resource's biome blocklist -- handled by the resourceAllowedForBiome check), then we'll add the resource
                 * to this biome with default production values. */
                /* Meant to let people add new resources without having to include that resource in *every* BiomeResourceDef */
                res = new ResourceBonuses
                {
                    resourceDef = resourceTypeDef,
                    additive = 1,
                    multiplier = 1
                };
                resources.Add(res);
            }
            return res;
        }
    }


    [DefOf]
    public class BiomeResourceDefOf
    {
        public static BiomeResourceDef defaultBiome;
        static BiomeResourceDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BiomeResourceDefOf));
        }
    }
}
