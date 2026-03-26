using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_RoadBuilders : FCPolicyBehavior
    {
        public override void OnEnacted(FactionFC faction)
        {
            string defName = Ext<FCPolicyBehaviorExt_RoadBuilders>().autoUnlockResearchDefName;
            ResearchProjectDef researchDef = DefDatabase<ResearchProjectDef>.GetNamed(defName, false);
            if (researchDef == null)
                LogUtil.Error("Road research returned Null");
            else if (Find.ResearchManager.GetProgress(researchDef) != researchDef.baseCost)
                Find.ResearchManager.FinishProject(researchDef);
        }
    }
}
