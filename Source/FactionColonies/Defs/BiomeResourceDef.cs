using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class BiomeResourceDef : Def
    {
        //public List<double> BaseProductionAdditive = new List<double>();
        //public List<double> BaseProductionMultiplicative = new List<double>();
        public List<ResourceBonuses> resources = new List<ResourceBonuses>();
        public bool canSettle;

        public ResourceBonuses getBiomeResource(ResourceTypeDef resourceTypeDef)
        {
            return resources.Where((ResourceBonuses b) => b.resourceDef == resourceTypeDef).FirstOrDefault();
        }
        public List<ResourceTypeDef> getBiomeResourceTypes()
        {
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
