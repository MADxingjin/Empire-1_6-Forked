using Verse;

namespace FactionColonies
{
    public class FCPolicyState_Pacifist : FCPolicyState
    {
        public int tickLastUsedDiplomat = -1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickLastUsedDiplomat, "tickLastUsedDiplomat", -1);
        }
    }
}
