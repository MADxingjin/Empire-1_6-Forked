using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Technocratic : FCPolicyModExtension
    {
        public override double ModifyResearchContribution(double contribution, WorldSettlementFC settlement) => contribution * 2;

        public override IEnumerable<(TaggedString label, Action onClick)> GetMainTabActionButtons(FactionFC faction)
        {
            yield return ("FCSendResearchItems".Translate(), () =>
            {
                if (Find.ColonistBar.GetColonistsInOrder().Count > 0)
                {
                    Pawn playerNegotiator = Find.ColonistBar.GetColonistsInOrder()[0];
                    FCTrader_Research trader = new FCTrader_Research();
                    Find.WindowStack.Add(new Dialog_Trade(playerNegotiator, trader));
                }
                else
                {
                    LogUtil.Error("Couldn't find any colonists to trade with");
                }
            });
        }
    }
}
