using FactionColonies;
using FactionColonies.util;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace FactionColonies
{
    public static class TravelUtil
    {
        public static int ReturnTicksToArrive(int currentTile, int destinationTile)
        {
            Log.Message($"ReturnTicksToArrive Debug: currentTile={currentTile}, destinationTile={destinationTile}");

            bool tilesInShuttleRange = (currentTile, destinationTile).AreTilesInAnyShuttleRange();
            bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
            bool podsResearched = DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false;

            if (!medievalOnly)
            {
                bool tilesValid = (currentTile, destinationTile).AreValidTiles();
                Log.Message($"ReturnTicksToArrive Debug: tilesValid={tilesValid}, medievalOnly={medievalOnly}, podsResearched={podsResearched}");

                if (!tilesValid)
                {
                    int fallbackTime = podsResearched ? 30000 : 600000;
                    Log.Message($"ReturnTicksToArrive Debug: Invalid tiles, returning fallback time: {fallbackTime} ticks ({fallbackTime / 60000f:F1} days)");
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
                    if (tempPath == WorldPath.NotFound) return 600000;

                    return CaravanArrivalTimeEstimator.EstimatedTicksToArrive(currentTile, destinationTile, tempPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(null), Find.TickManager.TicksAbs);
                }
            }
        }

        public static void sendPrisoner(Pawn prisoner, SettlementFC settlement)
        {
            settlement.addPrisoner(prisoner);
            prisoner.DeSpawn();
        }
    }
}
