using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Lucky : FCPolicyBehavior
    {
        private const float RerollChance = 0.2f;

        public override bool ShouldRerollEvent(FCEventDef eventDef)
        {
            if (!eventDef.isNegative) return false;
            if (!Rand.Chance(RerollChance)) return false;
            LogUtil.Message($"Lucky trait re-rolled negative event: {eventDef.defName}");
            return true;
        }
    }
}
