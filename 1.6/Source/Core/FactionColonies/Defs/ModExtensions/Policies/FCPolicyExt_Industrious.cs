using System;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Industrious : FCPolicyModExtension
    {
        public override double ModifyTaxTimeMultiplier(double mult, WorldSettlementFC settlement)
        {
            int num = Rand.RangeInclusive(1, 20);
            if (num == 5)
            {
                double boost = 1f + (Rand.RangeInclusive(20, 50) / 100f);
                Find.LetterStack.ReceiveLetter("FCIdustriousTaxBoost".Translate(),
                    "FCIndustriousPop".Translate(settlement.Name, Math.Round((boost - 1f) * 100f) + "%"),
                    LetterDefOf.PositiveEvent);
                return mult * boost;
            }
            return mult;
        }
    }
}
