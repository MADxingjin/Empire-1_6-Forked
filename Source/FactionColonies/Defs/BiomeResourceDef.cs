using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class BiomeResourceDef : Def
    {
        public List<ResourceBonuses> resources = new List<ResourceBonuses>();
        public bool canSettle;

        public ResourceBonuses getBiomeResource(ResourceTypeDef resourceTypeDef)
        {
            if (resources == null)
            {
                return null;
            }
            return resources.Where((ResourceBonuses b) => b.resourceDef == resourceTypeDef).FirstOrDefault();
        }
        public List<ResourceTypeDef> getBiomeResourceTypes()
        {
            if (resources == null)
            {
                return null;
            }
            List<ResourceTypeDef> list = new List<ResourceTypeDef>();
            foreach (ResourceBonuses resource in resources)
            {
                list.Add(resource.resourceDef);
            }
            return list;
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
