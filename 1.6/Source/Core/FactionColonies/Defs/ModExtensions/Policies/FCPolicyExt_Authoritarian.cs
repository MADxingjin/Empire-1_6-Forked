using System.Collections.Generic;
using Verse;
using FactionColonies.util;

namespace FactionColonies
{
    public class FCPolicyExt_Authoritarian : FCPolicyModExtension
    {
        public override void OnSettlementCreated(FactionFC faction, WorldSettlementFC settlement)
        {
            settlement.loyalty = 70;
        }

        public override bool EnablesAction(FCActionType action) => action == FCActionType.EnslaveSettlement;

        public override IEnumerable<FloatMenuOption> GetSettlementActions(FactionFC faction, WorldSettlementFC settlement)
        {
            yield return new FloatMenuOption("FCBuyLoyalty".Translate(),
                delegate { Find.WindowStack.Add(new FCWindow_Pay_Silver_Loyalty(settlement)); });
        }

        public override int ModifyDeadPawnCooldownMultiplier(int multiplier) => multiplier - 2000;
    }
}
