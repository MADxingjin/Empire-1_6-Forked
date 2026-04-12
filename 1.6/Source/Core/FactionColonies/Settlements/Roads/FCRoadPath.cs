using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class FCRoadPath : IExposable
    {
        public WorldPath Path { get; protected set; }
        public int From { get; protected set; }
        public int To { get; protected set; }
        public RoadDef builtRoadDef;

        /// <summary>
        /// Parameterless constructor required for Scribe deserialization.
        /// </summary>
        public FCRoadPath() { }

        public FCRoadPath(Settlement from, Settlement to)
        {
            if (from.Tile == to.Tile)
            {
                LogUtil.Error("Attempted to create road path to the same tile");
            }
            this.SetupPath(from.Tile, to.Tile);
        }

        public FCRoadPath(int from, int to)
        {
            if (from == to)
            {
                LogUtil.Error("Attempted to create road path to the same tile");
            }
            this.SetupPath(from, to);
        }

        void SetupPath(int from, int to)
        {
            this.From = from;
            this.To = to;

            var mainPlanetLayer = Find.WorldGrid.PlanetLayers[0];
            var fromTile = new PlanetTile(from, mainPlanetLayer);
            var toTile = new PlanetTile(to, mainPlanetLayer);
            WorldPath path;
            using (var pathing = new WorldPathing(mainPlanetLayer))
            {
                path = pathing.FindPath(fromTile, toTile, null);
            }

            // path belongs to a WorldPathPool that gets very vocal in the error log
            // when theres more WorldPaths than caravans. The workaround to this error
            // is to copy the path to a new WorldPath object that is not a part of
            // the pool and Dispose of the one that is
            this.Path = new WorldPath();
            foreach (int node in path.NodesReversed)
            {
                this.Path.AddNodeAtStart(node);
            }
            this.Path.SetupFound(path.TotalCost, mainPlanetLayer);
            this.Path.inUse = true;
            path.Dispose();
        }

        public void ExposeData()
        {
            int from = this.From;
            int to = this.To;
            Scribe_Values.Look(ref from, "from");
            Scribe_Values.Look(ref to, "to");
            Scribe_Defs.Look(ref builtRoadDef, "builtRoadDef");

            List<int> nodeIds = null;
            float totalCost = 0f;
            int nodesLeft = 0;

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                nodeIds = this.Path.NodesReversed.Select(t => t.tileId).ToList();
                totalCost = this.Path.TotalCost;
                nodesLeft = this.Path.NodesLeftCount;
            }

            Scribe_Collections.Look(ref nodeIds, "pathNodes", LookMode.Value);
            Scribe_Values.Look(ref totalCost, "totalCost");
            Scribe_Values.Look(ref nodesLeft, "nodesLeft");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.From = from;
                this.To = to;

                var mainPlanetLayer = Find.WorldGrid.PlanetLayers[0];
                this.Path = new WorldPath();
                foreach (int nodeId in nodeIds)
                {
                    this.Path.AddNodeAtStart(new PlanetTile(nodeId, mainPlanetLayer));
                }
                this.Path.SetupFound(totalCost, mainPlanetLayer);
                this.Path.inUse = true;

                // Restore build progress (curNodeIndex is private in WorldPath)
                Traverse.Create(this.Path).Field("curNodeIndex").SetValue(nodesLeft - 1);
            }
        }

        /// <summary>
        ///  Builds 1 segment of road. Returns if a segment was built
        /// </summary>
        /// <returns> this.IsCompleted </returns>
        /// <param name="roadDef">Road def.</param>
        public bool BuildSegment(RoadDef roadDef)
        {
            start:
            if (!this.Path.Found || this.IsCompleted)
                return false;

            int tile = this.Path.ConsumeNextNode();
            int lastTile = this.Path.Peek(-1);

            WorldGrid grid = Find.WorldGrid;

            RoadDef existingRoad = grid.GetRoadDef(lastTile, tile);
            if (IsNewRoadBetter(existingRoad, roadDef))
            {
                // Replaces the road if this.Road.priority > the existing road's priority
                grid.OverlayRoad(lastTile, tile, roadDef);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(lastTile, out _);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(tile, out _);
            }
            else
            {
                goto start;
            }
            return true;
        }


        public bool IsCompleted
        {
            get
            {
                return this.Path.NodesLeftCount == 1;
            }
        }

        public static bool IsNewRoadBetter(RoadDef oldRoad, RoadDef newRoad)
        {
            if (newRoad == null)
                return false;

            if (oldRoad == null)
                return true;

            return newRoad.priority > oldRoad.priority;
        }

        public void DrawPath()
        {
            this.Path.DrawPath(null);
        }

        public void ResetProgress()
        {
            Traverse.Create(this.Path).Field("curNodeIndex").SetValue(this.Path.NodesReversed.Count - 1);
        }
    }
}