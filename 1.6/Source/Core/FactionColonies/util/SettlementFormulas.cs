using System;

namespace FactionColonies.util
{
    /// <summary>
    /// Pure calculation methods for settlement stats and economy.
    /// These methods have zero RimWorld dependencies, making them unit-testable.
    /// </summary>
    public static class SettlementFormulas
    {
        /// <summary>
        /// Clamps a stat value after applying a change, rounding to 1 decimal place.
        /// </summary>
        public static double ClampStat(double current, double change, double min = 0, double max = 100)
        {
            return Math.Round(Math.Clamp(current + change, min, max), 1);
        }

        /// <summary>
        /// Calculates worker upkeep including overwork penalty.
        /// Overwork occurs when workers exceed workersMax, adding a penalty of (overwork / 20) * base upkeep.
        /// </summary>
        public static double CalculateWorkerUpkeep(double workers, double workersMax, double baseWorkerCost)
        {
            double overWork = workers > workersMax ? (int)(workers - workersMax) : 0;
            return (workers * baseWorkerCost) + ((workers * baseWorkerCost) * (overWork / 20));
        }

        /// <summary>
        /// Calculates the number of building slots available at a given settlement level.
        /// </summary>
        public static int CalculateBuildingSlots(int settlementLevel, int maxBuildingCount)
        {
            return Math.Min(3 + (int)Math.Floor(settlementLevel / 2f), maxBuildingCount);
        }

        /// <summary>
        /// Calculates building upkeep, applying militaristic policy discount for military buildings.
        /// Military buildings under militaristic policy get a 100 silver discount (minimum 0).
        /// </summary>
        public static int CalculateBuildingUpkeep(int baseUpkeep, bool isMilitary, bool hasMilitaristicPolicy)
        {
            if (baseUpkeep != 0 && (!isMilitary || !hasMilitaristicPolicy))
                return baseUpkeep;
            return Math.Max(0, baseUpkeep - 100);
        }

        /// <summary>
        /// Calculates the XP goal for the next faction level.
        /// </summary>
        public static float CalculateFactionLevelGoalXP(int currentLevel)
        {
            return 100 + (currentLevel * 150);
        }

        /// <summary>
        /// Calculates base stat penalties when a settlement loses a battle.
        /// Policy-specific modifiers (e.g. feudal, resilient) are applied via FCStatDef stats.
        /// </summary>
        public static (double prosperity, double happiness, double loyalty) CalculateBattleLossPenalties(
            double happinessLostMultiplier, double loyaltyLostMultiplier)
        {
            return (20, 25 * happinessLostMultiplier, 15 * loyaltyLostMultiplier);
        }

        /// <summary>
        /// Calculates the silver cost to upgrade a settlement to the next level.
        /// </summary>
        public static int CalculateUpgradeCost(int settlementLevel, int baseUpgradeCost)
        {
            return baseUpgradeCost + (settlementLevel * 1000);
        }

        /// <summary>
        /// Calculates the duration in ticks for a settlement upgrade to complete.
        /// </summary>
        public static int CalculateUpgradeTime(int settlementLevel, double buildTimeMultiplier)
        {
            return (int)((settlementLevel + 1) * 60000 * 2 * buildTimeMultiplier);
        }
    }
}
