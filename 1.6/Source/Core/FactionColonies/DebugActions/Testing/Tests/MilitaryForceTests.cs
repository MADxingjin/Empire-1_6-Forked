using System;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public static class MilitaryForceTests
    {
        // ============================
        // Constructor / Pure Math
        // ============================

        [EmpireTest("MilitaryForce")]
        public static void Constructor_ForceRemaining_EqualsRoundedLevelTimesEfficiency()
        {
            var force = new militaryForce(7.0, 1.3, null, null);
            double expected = Math.Round(7.0 * 1.3); // 9.1 → 9
            TestAssert.AreEqual(expected, force.forceRemaining,
                $"forceRemaining should be Round(level * efficiency) = {expected}");
        }

        [EmpireTest("MilitaryForce")]
        public static void Constructor_FractionalResult_RoundsCorrectly()
        {
            var force = new militaryForce(5.0, 1.5, null, null);
            double expected = Math.Round(5.0 * 1.5); // 7.5 → 8 (banker's rounding)
            TestAssert.AreEqual(expected, force.forceRemaining);
        }

        [EmpireTest("MilitaryForce")]
        public static void DefensivePower_AppliesDefenderAdvantage()
        {
            var force = new militaryForce(10.0, 1.0, null, null);
            double expected = Math.Round(force.forceRemaining * FCSettings.defenderAdvantage);
            TestAssert.AreEqual(expected, force.DefensivePower,
                $"DefensivePower should be Round(forceRemaining * {FCSettings.defenderAdvantage})");
        }

        // ============================
        // Tech Level Mapping
        // ============================

        [EmpireTest("MilitaryForce")]
        public static void TechLevelMapping_Neolithic_Level2_Eff1()
        {
            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(
                TechLevel.Neolithic, out double level, out double eff);
            TestAssert.AreEqual(2.0, level, message: "Neolithic level");
            TestAssert.AreEqual(1.0, eff, message: "Neolithic efficiency");
        }

        [EmpireTest("MilitaryForce")]
        public static void TechLevelMapping_Spacer_Level6_Eff1Point3()
        {
            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(
                TechLevel.Spacer, out double level, out double eff);
            TestAssert.AreEqual(6.0, level, message: "Spacer level");
            TestAssert.AreEqual(1.3, eff, message: "Spacer efficiency");
        }

        [EmpireTest("MilitaryForce")]
        public static void TechLevelMapping_Archotech_HighestValues()
        {
            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(
                TechLevel.Archotech, out double level, out double eff);
            TestAssert.AreEqual(9.0, level, message: "Archotech level");
            TestAssert.AreEqual(1.5, eff, message: "Archotech efficiency");
        }

        [EmpireTest("MilitaryForce")]
        public static void TechLevelMapping_AllLevels_PositiveValues()
        {
            foreach (TechLevel tech in Enum.GetValues(typeof(TechLevel)))
            {
                militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(
                    tech, out double level, out double eff);
                TestAssert.GreaterThan(level, 0, $"TechLevel {tech}: level should be > 0");
                TestAssert.GreaterThan(eff, 0, $"TechLevel {tech}: efficiency should be > 0");
            }
        }

        // ============================
        // Factory Methods (game state)
        // ============================

        private static WorldSettlementFC GetSettlement()
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null || faction.settlements.Count == 0) return null;
            return faction.settlements.FirstOrDefault(s => s.MilitaryComp != null);
        }

        [EmpireTest("MilitaryForce")]
        public static void CreateFromSettlement_IsFiniteAndPositive()
        {
            WorldSettlementFC settlement = GetSettlement();
            if (settlement == null) TestAssert.Skip("No settlement with MilitaryComp");

            militaryForce force = militaryForce.CreateMilitaryForceFromSettlement(settlement);

            TestAssert.IsFalse(double.IsNaN(force.forceRemaining), "forceRemaining should not be NaN");
            TestAssert.IsFalse(double.IsInfinity(force.forceRemaining), "forceRemaining should not be infinite");
            TestAssert.IsTrue(force.forceRemaining >= 0, $"forceRemaining should be >= 0, got {force.forceRemaining}");
            TestAssert.GreaterThan(force.militaryLevel, 0, "militaryLevel should be > 0");
            TestAssert.GreaterThan(force.militaryEfficiency, 0, "militaryEfficiency should be > 0");
        }

        [EmpireTest("MilitaryForce")]
        public static void CreateFromSettlement_AttackingVsDefending_DifferByStats()
        {
            WorldSettlementFC settlement = GetSettlement();
            if (settlement == null) TestAssert.Skip("No settlement with MilitaryComp");

            militaryForce attacking = militaryForce.CreateMilitaryForceFromSettlement(settlement, isAttacking: true);
            militaryForce defending = militaryForce.CreateMilitaryForceFromSettlement(settlement, isAttacking: false);

            // Both should be valid; they may differ if attack/defense stat bonuses differ
            TestAssert.IsFalse(double.IsNaN(attacking.forceRemaining), "Attacking force should not be NaN");
            TestAssert.IsFalse(double.IsNaN(defending.forceRemaining), "Defending force should not be NaN");
        }

        [EmpireTest("MilitaryForce")]
        public static void CreateFromEnemySettlement_MatchesTechLevel()
        {
            Settlement enemy = Find.WorldObjects.Settlements
                .FirstOrDefault(s => s.Faction != null && s.Faction != Faction.OfPlayer
                    && s.Faction != FactionCache.PlayerColonyFaction);
            if (enemy == null) TestAssert.Skip("No enemy settlement on world map");

            militaryForce force = militaryForce.CreateMilitaryForceFromEnemySettlement(enemy);

            militaryForce.GetMilitaryLevelAndEfficiencyFromTechLevel(
                enemy.Faction.def.techLevel, out double expectedLevel, out double expectedEff);

            TestAssert.AreEqual(expectedLevel, force.militaryLevel,
                $"Level should match tech level {enemy.Faction.def.techLevel}");
            TestAssert.AreEqual(expectedEff, force.militaryEfficiency,
                $"Efficiency should match tech level {enemy.Faction.def.techLevel}");
            TestAssert.AreEqual(Math.Round(expectedLevel * expectedEff), force.forceRemaining,
                "forceRemaining should be Round(level * efficiency)");
        }
    }
}
