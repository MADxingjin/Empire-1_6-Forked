namespace FactionColonies
{
    public class FCPolicyExt_Slaver : FCPolicyModExtension
    {
        public override int ModifyExtraWorkersSoftcap(int extra) => extra + 2;

        public override int ModifyOverMaxWorkers(int adjustment) => adjustment - 2;
    }
}
