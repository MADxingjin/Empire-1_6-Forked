using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Expansionist : FCPolicyState
    {
        public int tickLastUsedFeeReduction = -1;
        public bool canUseFeeReduction = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickLastUsedFeeReduction, "tickLastUsedFeeReduction", -1);
            Scribe_Values.Look(ref canUseFeeReduction, "canUseFeeReduction", true);
        }
    }
}
