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

        public static string GetTownTitle(SettlementFC settlement)
        {
            double highest = 0;
            ResourceType? resourceKey = null;
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

            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = settlement.getResource(resourceType);
                if (resource.endProduction > highest)
                {
                    highest = resource.endProduction;
                    resourceKey = resourceType;
                }
            }

            return ("FCTitle_" + resourceKey + "_" + level).Translate();
        }
    }
}
