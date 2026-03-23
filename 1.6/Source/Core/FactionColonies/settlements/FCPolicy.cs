using System;
using Verse;

namespace FactionColonies
{
    public class FCPolicy : IExposable
    {
        public FCPolicy()
        {
        }

        public FCPolicy(FCPolicyDef def)
        {
            FactionFC faction = FactionCache.FactionComp;
            this.def = def;
            timeEnacted = Find.TickManager.TicksGame;

            // Create behavior instance if this policy has procedural logic
            if (def.behaviorClass != null)
            {
                behavior = (FCPolicyBehavior)Activator.CreateInstance(def.behaviorClass);
                behavior.policy = this;
                try
                {
                    behavior.OnEnacted(faction);
                }
                catch (Exception e)
                {
                    LogUtil.Error($"FCPolicyBehavior.OnEnacted error for '{def.defName}': {e}");
                }
            }

        }

        public FCPolicyDef def;
        public int timeEnacted;
        public FCPolicyBehavior behavior;

        public bool IsFullyActive => def.enactDuration <= 0
            || Find.TickManager.TicksGame - timeEnacted >= def.enactDuration;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref timeEnacted, "timeEnacted");
            Scribe_Deep.Look(ref behavior, "behavior");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && behavior != null)
                behavior.policy = this;
        }
    }
}
