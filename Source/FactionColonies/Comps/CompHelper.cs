using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Verse.KeyPrefs;

namespace FactionColonies
{
    public static class CompHelper
    {
        public static void SettlementBuilding_Construct(WorldSettlementFC settlement, Type compClass, int buildingSlot)
        {
            WorldObjectComp comp = settlement.GetComponent(compClass);
            if (comp is WorldObjectComp_SettlementBuilding buildingcomp)
            {
                buildingcomp.OnConstruct(buildingSlot);
            }
        }
        public static void SettlementBuilding_Deconstruct(WorldSettlementFC settlement, Type compClass, int buildingSlot)
        {
            WorldObjectComp comp = settlement.GetComponent(compClass);
            if (comp is WorldObjectComp_SettlementBuilding buildingcomp)
            {
                buildingcomp.OnDeconstruct(buildingSlot);
            }
        }
    }
}
