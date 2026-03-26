using Verse;
using RimWorld;

namespace FactionColonies
{
    public class FCPolicyBehaviorExt_Expansionist : FCPolicyBehaviorExtension
    {
        public int feeReductionCooldownTicks = GenDate.TicksPerYear;
        public double discountMultiplier = 0.5;
        public int autoUpgradeToLevel = 2;
        public string readyLetterKey = "FCActionAvailable";
    }
}
