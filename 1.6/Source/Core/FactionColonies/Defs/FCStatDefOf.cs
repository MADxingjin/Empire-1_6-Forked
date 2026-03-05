using RimWorld;
using Verse;

namespace FactionColonies
{
    [DefOf]
    public class FCStatDefOf
    {
        // ── Military ──────────────────────────────────────────────
        public static FCStatDef militaryBaseLevel;
        public static FCStatDef militaryCombatEfficiency;
        public static FCStatDef militaryLevelBonusDefending;
        public static FCStatDef militaryLevelBonusAttacking;
        public static FCStatDef militaryEfficiencyBonusAttacking;
        public static FCStatDef militaryEfficiencyBonusDefending;
        public static FCStatDef militaryCooldownOffset;
        public static FCStatDef raidCooldownOffset;
        public static FCStatDef deadPawnCooldownOffset;

        // ── Battle Penalties ──────────────────────────────────────
        public static FCStatDef battleProsperityLossMultiplier;
        public static FCStatDef battleHappinessLossMultiplier;
        public static FCStatDef battleLoyaltyLossMultiplier;

        // ── Economy ───────────────────────────────────────────────
        public static FCStatDef taxBasePercentage;
        public static FCStatDef taxBaseRandomModifier;
        public static FCStatDef taxBonusFlat;
        public static FCStatDef titheValueMultiplier;
        public static FCStatDef lootMultiplier;
        public static FCStatDef settlementCostMultiplier;
        public static FCStatDef buildTimeMultiplier;
        public static FCStatDef createSettlementBaseCost;
        public static FCStatDef createSettlementMultiplier;
        public static FCStatDef researchContributionMultiplier;

        // ── Workers ───────────────────────────────────────────────
        public static FCStatDef workerBaseCost;
        public static FCStatDef workerBaseMax;
        public static FCStatDef workerBaseOverMax;
        public static FCStatDef extraWorkersSoftcap;
        public static FCStatDef overMaxWorkersAdjustment;

        // ── Prosperity ────────────────────────────────────────────
        public static FCStatDef prosperityBaseRecovery;

        // ── Happiness (base) ──────────────────────────────────────
        public static FCStatDef happinessLostBase;
        public static FCStatDef happinessGainedBase;

        // ── Happiness (multipliers) ───────────────────────────────
        public static FCStatDef happinessLostMultiplier;
        public static FCStatDef happinessGainedMultiplier;

        // ── Loyalty (base) ────────────────────────────────────────
        public static FCStatDef loyaltyLostBase;
        public static FCStatDef loyaltyGainedBase;

        // ── Loyalty (multipliers) ─────────────────────────────────
        public static FCStatDef loyaltyLostMultiplier;
        public static FCStatDef loyaltyGainedMultiplier;

        // ── Unrest (base) ─────────────────────────────────────────
        public static FCStatDef unrestLostBase;
        public static FCStatDef unrestGainedBase;

        // ── Unrest (multipliers) ──────────────────────────────────
        public static FCStatDef unrestLostMultiplier;
        public static FCStatDef unrestGainedMultiplier;

        static FCStatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCStatDefOf));
        }
    }
}
