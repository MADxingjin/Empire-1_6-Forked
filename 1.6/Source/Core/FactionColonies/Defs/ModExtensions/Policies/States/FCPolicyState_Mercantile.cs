using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Mercantile : FCPolicyState
    {
        public int nextCaravanTick = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextCaravanTick, "nextCaravanTick", -1);
        }
    }
}
