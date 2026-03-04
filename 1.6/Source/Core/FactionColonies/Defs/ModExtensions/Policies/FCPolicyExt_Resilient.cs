namespace FactionColonies
{
    public class FCPolicyExt_Resilient : FCPolicyModExtension
    {
        public override void ModifyBattlePenalties(ref double prosperityLoss, ref double happinessLoss, ref double loyaltyLoss)
        {
            prosperityLoss *= 0.5;
        }

        public override bool PreventBuildingDestruction() => true;
    }
}
