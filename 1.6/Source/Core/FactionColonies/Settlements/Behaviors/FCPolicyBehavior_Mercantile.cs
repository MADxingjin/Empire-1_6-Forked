using RimWorld;
using Verse;

namespace FactionColonies
{
    public class FCPolicyBehavior_Mercantile : FCPolicyBehavior
    {
        private int nextCaravanTick;

        public override void OnEnacted(FactionFC faction)
        {
            ScheduleNextCaravan();
        }

        public override void Tick(FactionFC faction)
        {
            if (nextCaravanTick > Find.TickManager.TicksGame) return;

            Map map = faction.ReturnCapitalMap();
            if (map is null)
            {
                ScheduleNextCaravan(true);
                return;
            }

            IncidentWorker_TraderCaravanArrival worker = new IncidentWorker_TraderCaravanArrival();
            worker.def = IncidentDefOf.TraderCaravanArrival;
            IncidentParms parms =
                StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);
            parms.faction = FactionCache.PlayerColonyFaction;

            if (!worker.CanFireNow(parms))
            {
                LogUtil.Warning($"Mercantile trader blocked by CanFireNow | colonists on map: {map.mapPawns.FreeColonistsSpawnedCount}, " +
                    $"trader kinds: {parms.faction?.def?.caravanTraderKinds?.Count ?? -1}");
                ScheduleNextCaravan(true);
                return;
            }

            RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Friendly);
            parms.spawnRotation = Rot4.FromAngleFlat((map.Center - parms.spawnCenter).AngleFlat);

            bool success = false;
            if (parms.spawnCenter.IsValid)
            {
                success = worker.TryExecute(parms);
            }
            else
            {
                LogUtil.Warning("Mercantile - Spawn Center not valid");
            }

            if (!success)
            {
                LogUtil.Warning($"Mercantile trader failed to spawn | trader kinds: {parms.faction?.def?.caravanTraderKinds?.Count ?? -1}, " +
                    $"colonists on map: {map.mapPawns.FreeColonistsSpawnedCount}");
            }

            ScheduleNextCaravan(!success);
        }

        private void ScheduleNextCaravan(bool failCase = false)
        {
            var ext = Ext<FCPolicyBehaviorExt_Mercantile>();
            float days;
            if (failCase)
            {
                days = ext.caravanRetryDays;
            }
            else
            {
                days = Rand.RangeInclusive(ext.caravanMinDays, ext.caravanMaxDays);
            }
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
