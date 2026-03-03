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
        /// Calculates the net stat change (gain - loss) for a settlement stat like happiness, loyalty, or unrest.
        /// </summary>
        public static double CalculateStatChange(
            double baseGain, double baseLoss,
            double gainTraitAdditive, double lossTraitAdditive,
            double gainMultiplier, double lossMultiplier,
            double policyBonus = 0)
        {
            double gain = gainMultiplier * (policyBonus + baseGain + gainTraitAdditive);
            double loss = lossMultiplier * (baseLoss + lossTraitAdditive);
            return gain - loss;
        }

        /// <summary>
        /// Clamps a stat value after applying a change, rounding to 1 decimal place.
        /// </summary>
        public static double ClampStat(double current, double change, double min = 1, double max = 100)
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
        /// Calculates stat penalties when a settlement loses a battle.
        /// Feudal policy doubles loyalty loss. Resilient trait halves prosperity loss.
        /// </summary>
        public static (double prosperity, double happiness, double loyalty) CalculateBattleLossPenalties(
            double happinessLostMultiplier, double loyaltyLostMultiplier,
            bool hasFeudalPolicy, bool hasResilientTrait)
        {
            int feudalMult = hasFeudalPolicy ? 2 : 1;
            float prosperityMult = hasResilientTrait ? 0.5f : 1f;
            return (20 * prosperityMult, 25 * happinessLostMultiplier, 15 * loyaltyLostMultiplier * feudalMult);
        }
    }
}
