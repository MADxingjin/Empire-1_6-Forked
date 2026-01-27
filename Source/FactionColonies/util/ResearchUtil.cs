using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public static class ResearchUtil
    {
        public static bool returnIsResearched(ResearchProjectDef def)
        {
            if (def == null)
            {
                return false;
            }

            return Math.Abs(Find.ResearchManager.GetProgress(def) - def.baseCost) < .1;
        }
    }
}
