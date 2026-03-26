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

            // Create behavior instance if this policy has a behavior extension
            FCPolicyBehaviorExtension ext = def.BehaviorExtension;
            if (ext != null)
            {
                behavior = ext.CreateBehavior();
                behavior.policy = this;
                behavior.PostInitialize();
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
            {
                behavior.policy = this;
                if (def?.BehaviorExtension != null)
                {
                    behavior.extension = def.BehaviorExtension;
                    behavior.PostInitialize();
                }
            }
        }
    }
}
