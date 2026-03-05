using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Expansionist : FCPolicyBehavior
    {
        private CooldownAbility feeReductionCooldown = new CooldownAbility
        {
            cooldownTicks = GenDate.TicksPerYear,
            readyLetterKey = "FCActionAvailable"
        };

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            if (settlement.settlementLevel == 1)
                settlement.upgradeSettlement();
        }

        public override double ModifyStat(FCStatDef stat, double currentValue, WorldSettlementFC settlement)
        {
            if (stat != FCStatDefOf.settlementCostMultiplier) return currentValue;

            FactionFC faction = FactionCache.FactionComp;

            // First settlement is free
            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
                return 0;

            // 50% discount when fee reduction is available
            if (feeReductionCooldown.IsReady)
                return currentValue * 0.5;

            return currentValue;
        }

        public override string GetStatDescription(FCStatDef stat, WorldSettlementFC settlement)
        {
            if (stat != FCStatDefOf.settlementCostMultiplier) return null;

            FactionFC faction = FactionCache.FactionComp;
            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
                return TextUtil.colorizeMultiplierBonus(0) + " - " + policy.def.LabelCap + "\n";
            if (feeReductionCooldown.IsReady)
                return TextUtil.colorizeMultiplierBonus(0.5) + " - " + policy.def.LabelCap + "\n";
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
            feeReductionCooldown = feeReductionCooldown ?? new CooldownAbility
            {
                cooldownTicks = GenDate.TicksPerYear,
                readyLetterKey = "FCActionAvailable"
            };
        }

        // Debug accessors
        public bool DebugCooldownReady() => feeReductionCooldown.IsReady;
        public float DebugCooldownDays() => feeReductionCooldown.DaysRemaining;
        public void DebugResetCooldown() => feeReductionCooldown.tickLastUsed = -1;
    }
}
