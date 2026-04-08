using FactionColonies.util;
using RimWorld.Planet;
using Verse;
using RimWorld;

namespace FactionColonies
{
    public static class TravelUtil
    {
        public const int timespanFallback = GenDate.TicksPerDay * 10;
        public static int ReturnTicksToArrive(PlanetTile currentTile, PlanetTile destinationTile)
        {
            LogUtil.Message($"ReturnTicksToArrive Debug: currentTile={currentTile}, destinationTile={destinationTile}");

            // Cross-layer (e.g. surface <-> orbital): caravan pathing is impossible.
            if (currentTile.Layer != destinationTile.Layer)
            {
                bool podsAvailable = FactionCache.TechTransportPods?.IsFinished ?? false;
                if (podsAvailable)
                {
                    int multiplier = (currentTile, destinationTile).AreTilesInAnyShuttleRange() ? 5 : 10;
                    return Find.WorldGrid.TraversalDistanceBetween(currentTile, destinationTile) * multiplier;
                }
                return timespanFallback; //10-day fallback
            }

            bool tilesInShuttleRange = (currentTile, destinationTile).AreTilesInAnyShuttleRange();
            bool medievalOnly = FCSettings.medievalTechOnly;
            bool podsResearched = FactionCache.TechTransportPods?.IsFinished ?? false;

            if (!medievalOnly)
            {
                bool tilesValid = (currentTile, destinationTile).AreValidTiles();
                LogUtil.Message($"ReturnTicksToArrive Debug: tilesValid={tilesValid}, medievalOnly={medievalOnly}, podsResearched={podsResearched}");

                if (!tilesValid)
                {
                    int fallbackTime = podsResearched ? 30000 : timespanFallback;
                    LogUtil.Message($"ReturnTicksToArrive Debug: Invalid tiles, returning fallback time: {fallbackTime} ticks ({fallbackTime / 60000f:F1} days)");
                    return fallbackTime;
                }
                if (podsResearched)
                {
                    int multiplier = tilesInShuttleRange ? 5 : 10;
                    return Find.WorldGrid.TraversalDistanceBetween(currentTile, destinationTile) * multiplier;
                }
            }

            var mainPlanetLayer = Find.WorldGrid.PlanetLayers[0];
            var fromTile = new PlanetTile(currentTile, mainPlanetLayer);
            var toTile = new PlanetTile(destinationTile, mainPlanetLayer);
            using (var pathing = new WorldPathing(mainPlanetLayer))
            {
                using (WorldPath tempPath = pathing.FindPath(fromTile, toTile, null))
                {
                    if (tempPath == WorldPath.NotFound) return timespanFallback;

                    return CaravanArrivalTimeEstimator.EstimatedTicksToArrive(fromTile, toTile, tempPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(null), Find.TickManager.TicksAbs);
                }
            }
        }

        public static void SendPrisoner(Pawn prisoner, WorldSettlementFC settlement)
        {
            settlement.AddPrisoner(prisoner);
            prisoner.DeSpawn();
        }
    }
}
