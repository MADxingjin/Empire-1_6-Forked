using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_RoadBuilders : FCPolicyModExtension
    {
        public override void OnEnacted(FactionFC faction, FCPolicy policy)
        {
            ResearchProjectDef researchdef = DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingDirt", false);
            if (researchdef == null)
                LogUtil.Error("Road research returned Null");
            else if (Find.ResearchManager.GetProgress(researchdef) != researchdef.baseCost)
                Find.ResearchManager.FinishProject(researchdef);
        }

        public override bool EnablesAction(FCActionType action) => action == FCActionType.BuildRoadsToAllies;
    }
}
