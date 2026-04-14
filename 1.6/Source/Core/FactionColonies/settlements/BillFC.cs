using RimWorld;
using Verse;

namespace FactionColonies
{
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

            if (settlement != null)
            {
                string messageString = "FCNotEnoughSilverForBill".Translate() + " " + settlement.Name + ". " + "FCConfiscatedTithes".Translate() + "." + " " + "FCUnpaidTitheEffect".Translate();
                settlement.GainUnrestWithReason(new Message(messageString, MessageTypeDefOf.NegativeEvent), 10d);
                settlement.GainHappiness(-10d);
            }
            else
            {
                LogUtil.Warning("BillFC.Resolve: bill has null settlement (loadID=" + loadID + "). Skipping penalty.");
            }
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
}
