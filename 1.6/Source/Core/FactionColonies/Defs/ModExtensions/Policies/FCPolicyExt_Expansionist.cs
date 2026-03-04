using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Expansionist : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Expansionist();

        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            if (settlement.settlementLevel == 1)
                settlement.upgradeSettlement();
        }

        public override double ModifySettlementCost(double cost)
        {
            FactionFC faction = FactionCache.FactionComp;
            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
                return 0;

            var state = faction.GetPolicyState<FCPolicyState_Expansionist>();
            if (state != null && (state.tickLastUsedFeeReduction == -1 || state.canUseFeeReduction))
                return cost / 2;

            return cost;
        }

        public override void OnSettlementCostPaid(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Expansionist;
            if (state != null && state.canUseFeeReduction)
            {
                state.tickLastUsedFeeReduction = Find.TickManager.TicksGame;
                state.canUseFeeReduction = false;
            }
        }

        public override void Tick(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Expansionist;
            if (state == null) return;

            if (!state.canUseFeeReduction &&
                (state.tickLastUsedFeeReduction + GenDate.TicksPerYear) <= Find.TickManager.TicksGame)
            {
                state.canUseFeeReduction = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionSettlementFeeReduction".Translate(), LetterDefOf.PositiveEvent);
            }
        }
    }
}
