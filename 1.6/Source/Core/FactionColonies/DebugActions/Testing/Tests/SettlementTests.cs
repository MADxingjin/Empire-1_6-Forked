using System.Linq;
using FactionColonies.util;

namespace FactionColonies
{
    public static class SettlementTests
    {
        private static WorldSettlementFC GetFirstSettlement()
        {
            var settlements = FactionCache.FactionComp?.settlements;
            if (settlements == null || settlements.Count == 0)
                return null;
            return settlements.First();
        }

        [EmpireTest("Settlement")]
        public static void Settlement_HappinessGain_IsFiniteNumber()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            double gain = settlement.getHappinessGain();
            TestAssert.IsFalse(double.IsNaN(gain), "Happiness gain should not be NaN");
            TestAssert.IsFalse(double.IsInfinity(gain), "Happiness gain should not be infinite");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_LoyaltyGain_IsFiniteNumber()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            double gain = settlement.getLoyaltyGain();
            TestAssert.IsFalse(double.IsNaN(gain), "Loyalty gain should not be NaN");
            TestAssert.IsFalse(double.IsInfinity(gain), "Loyalty gain should not be infinite");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_TotalUpkeep_IsNonNegative()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            double upkeep = settlement.getTotalUpkeep();
            TestAssert.IsTrue(upkeep >= 0, $"Total upkeep should be >= 0, got {upkeep}");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_BuildingSlots_MatchesFormula()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }
            if (settlement.BuildingsComp == null) { LogUtil.Message("SKIP: No BuildingsComp"); return; }

            int actual = settlement.BuildingsComp.NumBuildingSlots;
            int expected = SettlementFormulas.CalculateBuildingSlots(
                settlement.settlementLevel,
                settlement.settlementDef.maxBuildingCount);
            TestAssert.AreEqual(expected, actual, "NumBuildingSlots should match SettlementFormulas");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_Happiness_IsClamped()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            TestAssert.IsTrue(settlement.happiness >= 1 && settlement.happiness <= 100,
                $"Happiness should be in [1, 100], got {settlement.happiness}");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_Loyalty_IsClamped()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            TestAssert.IsTrue(settlement.loyalty >= 1 && settlement.loyalty <= 100,
                $"Loyalty should be in [1, 100], got {settlement.loyalty}");
        }

        [EmpireTest("Settlement")]
        public static void Settlement_Prosperity_IsClamped()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) { LogUtil.Message("SKIP: No settlements"); return; }

            TestAssert.IsTrue(settlement.prosperity >= 1 && settlement.prosperity <= 100,
                $"Prosperity should be in [1, 100], got {settlement.prosperity}");
        }
    }
}
