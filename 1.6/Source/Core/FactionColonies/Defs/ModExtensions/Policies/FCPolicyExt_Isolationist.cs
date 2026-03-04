namespace FactionColonies
{
    public class FCPolicyExt_Isolationist : FCPolicyModExtension
    {
        public override double ModifySettlementCost(double cost) => cost * 2;

        public override int ModifyBuildTime(int ticks) => ticks / 2;

        public override int ModifyExtraWorkersSoftcap(int extra) => extra + 3;

        public override bool BlocksAction(FCActionType action) => action == FCActionType.CaptureSettlement;

        public override double ModifyTaxBonus(double bonus, WorldSettlementFC settlement) => bonus + 10;
    }
}
