using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Expansionist : FCPolicyBehavior
    {
        private CooldownAbility feeReductionCooldown = new CooldownAbility();

        public override void PostInitialize()
        {
            var ext = Ext<FCPolicyBehaviorExt_Expansionist>();
            feeReductionCooldown.readyLetterKey = ext.readyLetterKey;
            if (feeReductionCooldown.cooldownTicks == 0)
                feeReductionCooldown.cooldownTicks = ext.feeReductionCooldownTicks;
        }

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            int targetLevel = Ext<FCPolicyBehaviorExt_Expansionist>().autoUpgradeToLevel;
            if (settlement.settlementLevel < targetLevel)
                settlement.UpgradeSettlement();
        }

        public override double ModifyStat(FCStatDef stat, double currentValue, WorldSettlementFC settlement)
        {
            if (stat != FCStatDefOf.settlementCostMultiplier) return currentValue;

            FactionFC faction = FactionCache.FactionComp;

            // First settlement is free
            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
                return 0;

            // Discount when fee reduction is available
            if (feeReductionCooldown.IsReady)
                return currentValue * Ext<FCPolicyBehaviorExt_Expansionist>().discountMultiplier;

            return currentValue;
        }

        public override string GetStatDescription(FCStatDef stat, WorldSettlementFC settlement)
        {
            if (stat != FCStatDefOf.settlementCostMultiplier) return null;

            FactionFC faction = FactionCache.FactionComp;
            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
                return TextUtil.ColorizeMultiplierBonus(0) + " - " + policy.def.LabelCap + "\n";
            if (feeReductionCooldown.IsReady)
                return TextUtil.ColorizeMultiplierBonus(Ext<FCPolicyBehaviorExt_Expansionist>().discountMultiplier) + " - " + policy.def.LabelCap + "\n";
            return null;
        }

        public override void OnSettlementCostPaid(FactionFC faction)
        {
            if (feeReductionCooldown.IsReady)
                feeReductionCooldown.Use();
        }

        public override void Tick(FactionFC faction)
        {
            feeReductionCooldown.TickCheckReady();
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref feeReductionCooldown, "feeReductionCooldown");
            feeReductionCooldown = feeReductionCooldown ?? new CooldownAbility();
        }

        // Debug accessors
        public bool DebugCooldownReady() => feeReductionCooldown.IsReady;
        public float DebugCooldownDays() => feeReductionCooldown.DaysRemaining;
        public void DebugResetCooldown() => feeReductionCooldown.tickLastUsed = -1;
    }
}
