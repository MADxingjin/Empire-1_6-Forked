using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{

    public class TaxesFC : ILoadReferenceable, IExposable
    {
        //internal variables
        public int loadID;
        public List<Thing> itemTithes;
        public float silverAmount;
        public List<ResourcePool> resourcePools;

        //ref
        public WorldSettlementFC settlement;
        public BillFC bill;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref itemTithes, "itemTithes", LookMode.Deep);
            Scribe_Values.Look(ref silverAmount, "silverAmount");
            Scribe_Collections.Look(ref resourcePools, "resourcePools", LookMode.Deep);

            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref bill, "bill");
        }

        public string GetUniqueLoadID()
        {
            return "Taxes_" + loadID;
        }

        public TaxesFC()
        {

        }

        public TaxesFC(BillFC bill)
        {
            SetUniqueLoadID();
            this.bill = bill;
            settlement = bill.settlement;
            silverAmount = 0;
            itemTithes = new List<Thing>();
            resourcePools = new List<ResourcePool>();

        }

        public void SetUniqueLoadID()
        {
            loadID = FactionCache.FactionComp.GetNextTaxID();
        }
    }

    public class BillFC : ILoadReferenceable, IExposable
    {
        //internal variables
        public int loadID;
        public int dueTick;


        //ref
        public WorldSettlementFC settlement;
        public TaxesFC taxes;



        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref dueTick, "dueTick", -1);


            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Deep.Look(ref taxes, "taxes");

        }

        public string GetUniqueLoadID()
        {
            return "Bill_" + loadID;
        }

        public BillFC()
        {

        }

        public BillFC(WorldSettlementFC settlement)
        {
            SetUniqueLoadID();
            this.settlement = settlement;
            dueTick = Find.TickManager.TicksGame + 300000;
            taxes = new TaxesFC(this);
        }

        public void SetUniqueLoadID()
        {
            loadID = FactionCache.FactionComp.GetNextBillID();
        }

        public bool Resolve()
        {
            FactionFC factionfc = FactionCache.FactionComp;
            if (AttemptResolve())
            {
                return true;
            }

            string messageString = "NotEnoughSilverForBill".Translate() + " " + settlement.Name + ". " + "ConfiscatedTithes".Translate() + "." + " " + "UnpaidTitheEffect".Translate();
            settlement.GainUnrestWithReason(new Message(messageString, MessageTypeDefOf.NegativeEvent), 10d);
            settlement.GainHappiness(-10d);
            factionfc.Bills.Remove(this);
            return false;
        }

        public bool AttemptResolve()
        {
            FactionFC factionfc = FactionCache.FactionComp;
            if (PaymentUtil.GetSilver() >= -1 * taxes.silverAmount || taxes.silverAmount >= 0)
            { //if have enough silver on the current map to pay  & map belongs to player

                FCEventMaker.CreateTaxEvent(this);
                if (taxes.resourcePools.Count > 0)
                {
                    factionfc.AddResourcePools(taxes.resourcePools);
                }

                return true;

            }

            return false;
        }
    }



    public class billUtility
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
