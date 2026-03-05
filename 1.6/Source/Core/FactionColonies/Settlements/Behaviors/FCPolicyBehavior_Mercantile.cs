using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Mercantile : FCPolicyBehavior
    {
        private int nextCaravanTick;

        public override void OnEnacted(FactionFC faction)
        {
            float days = Rand.RangeInclusive(3, 5);
            nextCaravanTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
        }

        public override void Tick(FactionFC faction)
        {
            if (nextCaravanTick > Find.TickManager.TicksGame) return;

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
            nextCaravanTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextCaravanTick, "nextCaravanTick");
        }

        // Debug accessors
        public int DebugNextCaravanTick() => nextCaravanTick;
        public void DebugResetNextCaravan() => nextCaravanTick = Find.TickManager.TicksGame;
    }
}
