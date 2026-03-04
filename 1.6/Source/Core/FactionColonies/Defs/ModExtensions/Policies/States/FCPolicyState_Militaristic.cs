using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Militaristic : FCPolicyState
    {
        public int tickLastUsedExtraSquad = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickLastUsedExtraSquad, "tickLastUsedExtraSquad", -1);
        }
    }
}
