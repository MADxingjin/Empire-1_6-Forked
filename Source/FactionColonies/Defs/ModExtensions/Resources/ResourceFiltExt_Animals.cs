using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public class ResourceFilterExtension_Animals : ResourceFilterExtension
    {
        public override void SetFilter(ThingFilter filter, TechLevel techlevel)
        {
            List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
            foreach (PawnKindDef def in allAnimalDefs)
            {
                if (def.IsAnimalAndAllowed())
                {
                    filter.SetAllow(def.race, true);
                }
            }
        }
        public override ThingSetMaker getThingSetMaker(out TechLevel tlevel)
        {
            tlevel = TechLevel.Undefined;
            return new ThingSetMaker_Animals();
        }
    }
}
