using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    /// <summary>
    /// Utility class for specialized value-to-string functions.
    /// </summary>
    public static class TextUtil
    {
        public static string FloorStat(double stat)
        {
            return Convert.ToString(Math.Floor((stat * 100)) / 100);
        }

        public static string GetTownTitle(WorldSettlementFC settlement)
        {
            double highest = 0;
            string resourceKey = "";
            int level;
            if (settlement.settlementLevel <= 3)
            {
                level = 1;
            }
            else if (settlement.settlementLevel <= 6)
            {
                level = 2;
            }
            else
            {
                level = 3;
            }

            foreach (ResourceFC resource in settlement.Resources)
            {
                if (resource.production > highest)
                {
                    highest = resource.production;
                    resourceKey = resource.def.defName;
                }
            }
            //TODO: find these localization keys and make sure they line up with the new def resources
            //      maybe even find a better way to assmelbe these town titles
            return ("FCTitle_" + resourceKey + "_" + level).Translate();
        }
    }
}
