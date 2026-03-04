namespace FactionColonies
{
    public class FCPolicyExt_Innovative : FCPolicyModExtension
    {
        public override double ModifyResearchContribution(double contribution, WorldSettlementFC settlement)
        {
            double profitBonus = (settlement?.getTotalProfit() ?? 0) * 0.05;
            return contribution + profitBonus;
        }
    }
}
