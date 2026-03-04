using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Feudal : FCPolicyState
    {
        public int tickLastUsedMercenary = -1;
        public bool canUseMercenary = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickLastUsedMercenary, "tickLastUsedMercenary", -1);
            Scribe_Values.Look(ref canUseMercenary, "canUseMercenary", true);
        }
    }
}
