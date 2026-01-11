using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public static class CompareUtil
    {
        public static int CompareFloatMenuOption(FloatMenuOption x, FloatMenuOption y)
        {
            return String.Compare(x.Label, y.Label);
        }

        public static int CompareBuildingDef(BuildingFCDef x, BuildingFCDef y)
        {
            return string.Compare(x.label, y.label);
        }

        public static int CompareSettlementName(SettlementFC x, SettlementFC y)
        {
            return string.Compare(x.name, y.name);
        }

        public static int CompareSettlementLevel(SettlementFC x, SettlementFC y)
        {
            return y.settlementLevel.CompareTo(x.settlementLevel);
        }

        public static int CompareSettlementMilitaryLevel(SettlementFC x, SettlementFC y)
        {
            return y.settlementMilitaryLevel.CompareTo(x.settlementMilitaryLevel);
        }

        public static int CompareSettlementFreeWorkers(SettlementFC x, SettlementFC y)
        {
            return ((y.workersUltraMax - y.getTotalWorkers()).CompareTo((x.workersUltraMax - x.getTotalWorkers())));
        }

        public static int CompareSettlementUnrest(SettlementFC x, SettlementFC y)
        {
            return x.unrest.CompareTo(y.unrest);
        }

        public static int CompareSettlementLoyalty(SettlementFC x, SettlementFC y)
        {
            return y.loyalty.CompareTo(x.loyalty);
        }

        public static int CompareSettlementHappiness(SettlementFC x, SettlementFC y)
        {
            return y.happiness.CompareTo(x.happiness);
        }

        public static int CompareSettlementProsperity(SettlementFC x, SettlementFC y)
        {
            return y.prosperity.CompareTo(x.prosperity);
        }

        public static int CompareSettlementProfit(SettlementFC x, SettlementFC y)
        {
            return y.getTotalProfit().CompareTo(x.getTotalProfit());
        }
    }
}
