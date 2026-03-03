using System;
using FactionColonies.util;

namespace FactionColonies
{
    public static class FormulaTests
    {
        // --- CalculateStatChange ---

        [EmpireTest("Formula")]
        public static void StatChange_BaseGainOnly_ReturnsBaseGain()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 1.0, baseLoss: 0,
                gainTraitAdditive: 0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 1.0);
            TestAssert.AreEqual(1.0, result);
        }

        [EmpireTest("Formula")]
        public static void StatChange_BaseLossOnly_ReturnsNegative()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 0, baseLoss: 2.0,
                gainTraitAdditive: 0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 1.0);
            TestAssert.AreEqual(-2.0, result);
        }

        [EmpireTest("Formula")]
        public static void StatChange_GainAndLoss_ReturnsNetDifference()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 3.0, baseLoss: 1.0,
                gainTraitAdditive: 0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 1.0);
            TestAssert.AreEqual(2.0, result);
        }

        [EmpireTest("Formula")]
        public static void StatChange_WithTraitAdditives_AddsToBase()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 1.0, baseLoss: 0,
                gainTraitAdditive: 2.0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 1.0);
            TestAssert.AreEqual(3.0, result);
        }

        [EmpireTest("Formula")]
        public static void StatChange_WithMultiplier_MultipliesGainSum()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 1.0, baseLoss: 0,
                gainTraitAdditive: 1.0, lossTraitAdditive: 0,
                gainMultiplier: 1.5, lossMultiplier: 1.0);
            TestAssert.AreEqual(3.0, result); // 1.5 * (1 + 1) = 3
        }

        [EmpireTest("Formula")]
        public static void StatChange_WithPolicyBonus_AddsToGain()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 1.0, baseLoss: 0,
                gainTraitAdditive: 0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 1.0,
                policyBonus: 2.0);
            TestAssert.AreEqual(3.0, result);
        }

        [EmpireTest("Formula")]
        public static void StatChange_LossMultiplierAffectsLossOnly()
        {
            double result = SettlementFormulas.CalculateStatChange(
                baseGain: 1.0, baseLoss: 1.0,
                gainTraitAdditive: 0, lossTraitAdditive: 0,
                gainMultiplier: 1.0, lossMultiplier: 2.0);
            TestAssert.AreEqual(-1.0, result); // 1*1 - 2*1 = -1
        }

        // --- ClampStat ---

        [EmpireTest("Formula")]
        public static void ClampStat_WithinRange_ReturnsSum()
        {
            TestAssert.AreEqual(50.0, SettlementFormulas.ClampStat(48.0, 2.0));
        }

        [EmpireTest("Formula")]
        public static void ClampStat_ExceedsMax_ClampsToMax()
        {
            TestAssert.AreEqual(100.0, SettlementFormulas.ClampStat(99.0, 5.0));
        }

        [EmpireTest("Formula")]
        public static void ClampStat_BelowMin_ClampsToMin()
        {
            TestAssert.AreEqual(1.0, SettlementFormulas.ClampStat(2.0, -5.0));
        }

        [EmpireTest("Formula")]
        public static void ClampStat_RoundsToOneDecimal()
        {
            TestAssert.AreEqual(50.3, SettlementFormulas.ClampStat(50.0, 0.333));
        }

        // --- CalculateWorkerUpkeep ---

        [EmpireTest("Formula")]
        public static void WorkerUpkeep_NoWorkers_ReturnsZero()
        {
            TestAssert.AreEqual(0.0, SettlementFormulas.CalculateWorkerUpkeep(
                workers: 0, workersMax: 10, baseWorkerCost: 100));
        }

        [EmpireTest("Formula")]
        public static void WorkerUpkeep_UnderMax_IsLinear()
        {
            TestAssert.AreEqual(500.0, SettlementFormulas.CalculateWorkerUpkeep(
                workers: 5, workersMax: 10, baseWorkerCost: 100));
        }

        [EmpireTest("Formula")]
        public static void WorkerUpkeep_AtMax_NoPenalty()
        {
            TestAssert.AreEqual(1000.0, SettlementFormulas.CalculateWorkerUpkeep(
                workers: 10, workersMax: 10, baseWorkerCost: 100));
        }

        [EmpireTest("Formula")]
        public static void WorkerUpkeep_OverMax_IncludesOverworkPenalty()
        {
            // base: 12 * 100 = 1200, overWork: 2, penalty: 1200 * (2/20) = 120, total: 1320
            TestAssert.AreEqual(1320.0, SettlementFormulas.CalculateWorkerUpkeep(
                workers: 12, workersMax: 10, baseWorkerCost: 100));
        }

        [EmpireTest("Formula")]
        public static void WorkerUpkeep_LargeOverwork_DoublesTheCost()
        {
            // base: 30 * 100 = 3000, overWork: 20, penalty: 3000 * (20/20) = 3000, total: 6000
            TestAssert.AreEqual(6000.0, SettlementFormulas.CalculateWorkerUpkeep(
                workers: 30, workersMax: 10, baseWorkerCost: 100));
        }

        // --- CalculateBuildingSlots ---

        [EmpireTest("Formula")]
        public static void BuildingSlots_Level0_Returns3()
        {
            TestAssert.AreEqual(3, SettlementFormulas.CalculateBuildingSlots(0, 10));
        }

        [EmpireTest("Formula")]
        public static void BuildingSlots_Level4_Returns5()
        {
            TestAssert.AreEqual(5, SettlementFormulas.CalculateBuildingSlots(4, 10));
        }

        [EmpireTest("Formula")]
        public static void BuildingSlots_Level10_Returns8()
        {
            TestAssert.AreEqual(8, SettlementFormulas.CalculateBuildingSlots(10, 10));
        }

        [EmpireTest("Formula")]
        public static void BuildingSlots_CappedByMaxCount()
        {
            TestAssert.AreEqual(4, SettlementFormulas.CalculateBuildingSlots(10, 4));
        }

        // --- CalculateBuildingUpkeep ---

        [EmpireTest("Formula")]
        public static void BuildingUpkeep_NonMilitary_ReturnsBase()
        {
            TestAssert.AreEqual(200, SettlementFormulas.CalculateBuildingUpkeep(200, false, false));
        }

        [EmpireTest("Formula")]
        public static void BuildingUpkeep_MilitaryWithPolicy_Gets100Discount()
        {
            TestAssert.AreEqual(100, SettlementFormulas.CalculateBuildingUpkeep(200, true, true));
        }

        [EmpireTest("Formula")]
        public static void BuildingUpkeep_MilitaryWithPolicy_MinimumZero()
        {
            TestAssert.AreEqual(0, SettlementFormulas.CalculateBuildingUpkeep(50, true, true));
        }

        // --- CalculateBattleLossPenalties ---

        [EmpireTest("Formula")]
        public static void BattleLoss_NoTraits_ReturnsBaseValues()
        {
            var (prosperity, happiness, loyalty) = SettlementFormulas.CalculateBattleLossPenalties(
                happinessLostMultiplier: 1.0, loyaltyLostMultiplier: 1.0,
                hasFeudalPolicy: false, hasResilientTrait: false);
            TestAssert.AreEqual(20.0, prosperity);
            TestAssert.AreEqual(25.0, happiness);
            TestAssert.AreEqual(15.0, loyalty);
        }

        [EmpireTest("Formula")]
        public static void BattleLoss_Feudal_DoublesLoyaltyLoss()
        {
            var (_, _, loyalty) = SettlementFormulas.CalculateBattleLossPenalties(
                happinessLostMultiplier: 1.0, loyaltyLostMultiplier: 1.0,
                hasFeudalPolicy: true, hasResilientTrait: false);
            TestAssert.AreEqual(30.0, loyalty); // 15 * 1 * 2
        }

        [EmpireTest("Formula")]
        public static void BattleLoss_Resilient_HalvesProsperityLoss()
        {
            var (prosperity, _, _) = SettlementFormulas.CalculateBattleLossPenalties(
                happinessLostMultiplier: 1.0, loyaltyLostMultiplier: 1.0,
                hasFeudalPolicy: false, hasResilientTrait: true);
            TestAssert.AreEqual(10.0, prosperity); // 20 * 0.5
        }
    }
}
