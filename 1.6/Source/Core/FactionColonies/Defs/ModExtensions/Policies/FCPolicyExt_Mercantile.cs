using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyExt_Mercantile : FCPolicyModExtension
    {
        public override FCPolicyState CreateState() => new FCPolicyState_Mercantile();

        public override void OnEnacted(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Mercantile;
            if (state != null)
            {
                float days = Rand.RangeInclusive(3, 5);
                LogUtil.Message($"Mercantile enacted. First caravan in {days} days.");
                state.nextCaravanTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
            }
        }

        public override void Tick(FactionFC faction, FCPolicy policy)
        {
            var state = policy.state as FCPolicyState_Mercantile;
            if (state == null || state.nextCaravanTick > Find.TickManager.TicksGame) return;

            LogUtil.Message("Attempting to send mercantile trader caravan");
            IncidentWorker_TraderCaravanArrival worker = new IncidentWorker_TraderCaravanArrival();
            worker.def = IncidentDefOf.TraderCaravanArrival;
            IncidentParms parms =
                StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, faction.returnCapitalMap());
            parms.faction = FactionCache.PlayerColonyFaction;
            RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, (Map)parms.target,
                CellFinder.EdgeRoadChance_Friendly);
            parms.spawnRotation = Rot4.FromAngleFlat((((Map)parms.target).Center - parms.spawnCenter).AngleFlat);
            if (parms.spawnCenter.IsValid)
                worker.TryExecute(parms);
            else
                LogUtil.Warning("Mercantile - Spawn Center not valid");

            float days = Rand.RangeInclusive(3, 5);
            LogUtil.Message($"Resetting Mercantile Caravan Time. New arrival in {days} days.");
            state.nextCaravanTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
        }
    }
}
