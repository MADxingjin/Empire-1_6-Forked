using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class BillUtility
    {
        public static void ProcessBills()
        {
            FactionFC factionfc = FactionCache.FactionComp;
            List<WorldSettlementFC> latePaidSettlements = new List<WorldSettlementFC>();

            for (int i = factionfc.Bills.Count - 1; i >= 0; i--)
            {
                if (factionfc.Bills[i].dueTick < Find.TickManager.TicksGame)
                {
                    BillFC bill = factionfc.Bills[i];
                    bool owedMoney = bill.taxes.silverAmount < 0;
                    WorldSettlementFC settlement = bill.settlement;

                    if (bill.AttemptResolve())
                    {
                        if (owedMoney && settlement != null)
                        {
                            latePaidSettlements.Add(settlement);
                        }
                    }
                    else
                    {
                        string messageString = "NotEnoughSilverForBill".Translate() + " "
                            + settlement.Name + ". "
                            + "ConfiscatedTithes".Translate() + "."
                            + " " + "UnpaidTitheEffect".Translate();
                        settlement.GainUnrestWithReason(new Message(messageString, MessageTypeDefOf.NegativeEvent), 10d);
                        settlement.GainHappiness(-10d);
                        factionfc.Bills.Remove(bill);
                    }
                }
            }

            if (latePaidSettlements.Count > 0)
            {
                foreach (WorldSettlementFC settlement in latePaidSettlements)
                {
                    settlement.GainUnrest(4d);
                    settlement.GainHappiness(-4d);
                }

                string settlementList = string.Join("\n", latePaidSettlements.Select(s => "  - " + s.Name));
                Find.LetterStack.ReceiveLetter(
                    "LateBillAutoPaidLabel".Translate(),
                    "LateBillAutoPaidDesc".Translate(latePaidSettlements.Count, settlementList),
                    LetterDefOf.NegativeEvent);
            }
        }
    }
}