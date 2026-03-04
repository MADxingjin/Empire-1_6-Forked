namespace FactionColonies
{
    public class FCPolicyExt_Raiders : FCPolicyModExtension
    {
        public override int ModifyMilitaryCooldown(int ticks, MilitaryJob job)
        {
            if (job == MilitaryJob.RaidEnemySettlement || job == MilitaryJob.EnslaveEnemySettlement)
                return ticks - 60000;
            return ticks;
        }

        public override double ModifyLootMultiplier(double mult) => mult * 1.2;
    }
}
