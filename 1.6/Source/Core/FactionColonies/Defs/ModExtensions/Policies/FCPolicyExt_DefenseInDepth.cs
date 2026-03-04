namespace FactionColonies
{
    public class FCPolicyExt_DefenseInDepth : FCPolicyModExtension
    {
        public override void ModifyMilitaryForce(ref double level, ref double efficiency, bool isAttacking)
        {
            if (!isAttacking)
                level += 2;
        }
    }
}
