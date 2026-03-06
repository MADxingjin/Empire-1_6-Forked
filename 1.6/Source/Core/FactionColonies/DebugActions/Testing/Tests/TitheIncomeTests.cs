using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;

namespace FactionColonies
{
    public static class TitheIncomeTests
    {
        // ============================
        // Helpers
        // ============================

        private static WorldSettlementFC GetFirstSettlement()
        {
            var settlements = FactionCache.FactionComp?.settlements;
            if (settlements == null || settlements.Count == 0)
                return null;
            return settlements[0];
        }

        private static ResourceFC GetFirstNonPoolResource(WorldSettlementFC settlement)
        {
            return settlement.Resources.FirstOrDefault(r => !r.def.isPoolResource);
        }

        private static void WithFactionModifier(FCStatDef stat, double value, Action action)
        {
            FactionFC faction = FactionCache.FactionComp;
            var settlement = faction.settlements[0];
            var mods = new List<FCStatModifier> { new FCStatModifier { stat = stat, value = value } };
            settlement.addStatModifiers(mods, "titheTest");
            try
            {
                action();
            }
            finally
            {
                settlement.removeStatModifiersBySource("titheTest");
            }
        }

        // ============================
        // Tests
        // ============================

        [EmpireTest("TitheIncome")]
        public static void TitheModifierPerWorker_MatchesStatPlusSetting()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) TestAssert.Skip("No settlement");

            ResourceFC resource = GetFirstNonPoolResource(settlement);
            if (resource == null) TestAssert.Skip("No non-pool resource");

            double expected = settlement.getStatValue(FCStatDefOf.taxBaseRandomModifier)
                            + FCSettings.productionTitheMod;
            double actual = resource.getTitheModifierPerWorker();

            TestAssert.AreEqual(expected, actual,
                message: "getTitheModifierPerWorker should equal taxBaseRandomModifier stat + productionTitheMod setting");
        }

        [EmpireTest("TitheIncome")]
        public static void TitheIncome_ZeroWorkers_EqualsBaseTimesMultiplier()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) TestAssert.Skip("No settlement");

            ResourceFC resource = GetFirstNonPoolResource(settlement);
            if (resource == null) TestAssert.Skip("No non-pool resource");

            int savedWorkers = resource.assignedWorkers;
            try
            {
                resource.assignedWorkers = 0;
                resource.setDirtyCache();

                double multForTotal = FactionCache.FactionComp.GetStatValue(FCStatDefOf.titheValueMultiplier);
                double expected = resource.taxableProductionMarketValue * multForTotal;
                double actual = resource.getTitheIncome();

                TestAssert.AreEqual(expected, actual,
                    message: "With 0 workers, tithe income should equal taxableProductionMarketValue * titheValueMultiplier");
            }
            finally
            {
                resource.assignedWorkers = savedWorkers;
                resource.setDirtyCache();
            }
        }

        [EmpireTest("TitheIncome")]
        public static void TitheIncome_FormulaConsistency()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) TestAssert.Skip("No settlement");

            ResourceFC resource = GetFirstNonPoolResource(settlement);
            if (resource == null) TestAssert.Skip("No non-pool resource");

            // Manually compute using the same formula that getTitheIncome should use
            double workerMod = resource.getTitheModifierPerWorker() * resource.assignedWorkers;
            double multForTotal = FactionCache.FactionComp.GetStatValue(FCStatDefOf.titheValueMultiplier);
            double expected = (resource.taxableProductionMarketValue + workerMod) * multForTotal;
            double actual = resource.getTitheIncome();

            TestAssert.AreEqual(expected, actual,
                message: "getTitheIncome should equal (taxableMarketValue + workerMod) * titheValueMultiplier");
        }

        [EmpireTest("TitheIncome")]
        public static void TitheIncome_MultForTotal_ChangesWithStat()
        {
            var settlement = GetFirstSettlement();
            if (settlement == null) TestAssert.Skip("No settlement");

            ResourceFC resource = GetFirstNonPoolResource(settlement);
            if (resource == null) TestAssert.Skip("No non-pool resource");
            if (resource.assignedWorkers == 0) TestAssert.Skip("Resource has 0 workers");

            double incomeBefore = resource.getTitheIncome();

            // titheValueMultiplier is multiplicative, so adding 1.5 means multiplying by 1.5
            WithFactionModifier(FCStatDefOf.titheValueMultiplier, 1.5, () =>
            {
                resource.setDirtyCache();
                double incomeAfter = resource.getTitheIncome();

                if (incomeBefore != 0)
                {
                    TestAssert.GreaterThan(incomeAfter, incomeBefore,
                        $"Tithe income should increase with higher titheValueMultiplier (before={incomeBefore:F2}, after={incomeAfter:F2})");
                }
            });

            resource.setDirtyCache();
            double incomeRestored = resource.getTitheIncome();
            TestAssert.AreEqual(incomeBefore, incomeRestored,
                message: "Tithe income should restore after removing modifier");
        }
    }
}
