using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using FactionColonies.util;

namespace FactionColonies
{
    public class FCRoadBuilder : IExposable
    {
        public FCRoadQueue roadQueue;
        public RoadDef roadDef;

        public int daysBetweenTicks = 3;
        public bool roadBuildingEnabled = true;
        public bool wasRoadBuildingDisabled = true;
        bool hasRoadBuildersBoost;

        public FCRoadBuilder()
        {
        }

        //DirtPath (priority: 10) - This is the lowest priority road, likely the basic dirt path
        //DirtRoad (priority: 20) - This is the traditional dirt road
        //StoneRoad (priority: 30)
        //AncientAsphaltRoad (priority: 40)
        //AncientAsphaltHighway (priority: 50)
        // We could define our own and provide a roaddef for this, such as spacer / glitterworld tech level roads...

        public void ExposeData()
        {
            Scribe_Defs.Look(ref roadDef, "roadDef");
            Scribe_Values.Look(ref daysBetweenTicks, "daysBetweenTicks");
            Scribe_Values.Look(ref roadBuildingEnabled, "roadBuildingEnabled");
            Scribe_Values.Look(ref wasRoadBuildingDisabled, "wasRoadBuildingDisabled");
            Scribe_Deep.Look(ref roadQueue, "roadQueue", new object[]{ this.roadDef, this.daysBetweenTicks });
        }

        public void FirstTick()
        {
            CheckForTechChanges();
            CreateRoadQueue(false);
            FlagUpdateRoadQueues();

            if (daysBetweenTicks == 0)
            {
                LogUtil.Message("FCRoadBuilder - Resetting daysBetweenTicks");
                int days = hasRoadBuildersBoost ? 1 : 3;
                daysBetweenTicks = days;
                roadQueue.daysBetweenTicks = days;
            }
        }

        public void RoadTick()
        {
            if (roadDef == null)
            {
                wasRoadBuildingDisabled = true;
                return;
            }
            
            if (!roadBuildingEnabled)
            {
                wasRoadBuildingDisabled = true;
                return;
            }

            // Every 20 ticks causes a slight stutter, but the game is still playable
            // TODO: Make this a config option
            if(Find.TickManager.TicksGame % 20 == 0)
            {                
                FactionFC faction = FactionCache.FactionComp;

                if (roadQueue == null)
                {
                    LogUtil.Message("RoadTick: No road queue found");
                    return;
                }

                if (!hasRoadBuildersBoost && faction.IsActionAllowed(FCActionType.BuildRoadsToAllies))
                {
                    roadQueue.shouldUpdateSettlementsToProcess = true;
                    roadQueue.daysBetweenTicks = 1;
                    hasRoadBuildersBoost = true;
                    daysBetweenTicks = 1;
                }

                // If road building was disabled, then set the next tick to make a road to the correct time
                if (wasRoadBuildingDisabled)
                {
                    wasRoadBuildingDisabled = false;
                    roadQueue.nextRoadTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * roadQueue.daysBetweenTicks;
                }

                roadQueue.ProcessOnePath();
                
                bool segmentBuilt = roadQueue.BuildRoadSegments();
            }
        }

        // Returns whether or not a settlement would be built to.
        public static bool IsValidRoadTarget(Settlement settlement)
        {
            FactionFC fC = FactionCache.FactionComp;

            // If faction exists and is either player or player has roadBuilders trait and the faction is an ally
            if (settlement.Faction != null)
                if (settlement.Faction.IsPlayer || (fC.IsActionAllowed(FCActionType.BuildRoadsToAllies) && settlement.Faction.PlayerRelationKind == FactionRelationKind.Ally))
                    return true;

            foreach (WorldSettlementFC settlementFC in fC.settlements)
            {
                if (settlementFC.Tile == settlement.Tile)
                    return true;
            }

            return false;
        }

        public FCRoadQueue CreateRoadQueue(bool logFailure = true)
        {
            if (roadQueue != null) 
            {
                if (logFailure)
                {
                    LogUtil.Message($"Road queue already exists.");
                }

                return roadQueue;
            }
            roadQueue = new FCRoadQueue(roadDef, daysBetweenTicks);
            return roadQueue;
        }

        public void CheckForTechChanges()
        {
            LogUtil.Message("CheckForTechChanges: Starting tech check...");
            
            FactionFC faction = FactionCache.FactionComp;
            RoadDef def = this.roadDef;
            RoadDef oldDef = def;

            if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingHighway", false).IsFinished)
            {
                def = RoadDefOf.AncientAsphaltHighway;
                LogUtil.Message("CheckForTechChanges: Highway research complete, using AncientAsphaltHighway");
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingRoad", false).IsFinished)
            {
                def = RoadDefOf.AncientAsphaltRoad;
                LogUtil.Message("CheckForTechChanges: Road research complete, using AncientAsphaltRoad");
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingDirt", false).IsFinished)
            {
                // Use DirtPath (priority 10) to match existing world-generated dirt paths
                def = FCRoadsDef.DirtPath ?? DefDatabase<RoadDef>.GetNamed("DirtPath", false);
                LogUtil.Message($"CheckForTechChanges: Dirt road research complete, using {def?.defName ?? "null"}");
            }
            else
            {
                LogUtil.Message("CheckForTechChanges: No road research completed yet");
            }

            if (this.roadDef != def)
            {
                LogUtil.Message($"CheckForTechChanges: Road type changed from {oldDef?.defName ?? "null"} to {def?.defName ?? "null"}");
                this.roadDef = def;

                roadQueue.RoadDef = def;
            }
            else
            {
                LogUtil.Message($"CheckForTechChanges: Road type unchanged: {this.roadDef?.defName ?? "null"}");
            }
        }

        public void DrawPaths()
        {
            roadQueue.DrawPaths();
        }

        /// <summary>
        /// Flags all road queues to update whenever they are able.
        /// </summary>
        public void FlagUpdateRoadQueues()
        {
            if (roadQueue != null)
                roadQueue.shouldUpdateSettlementsToProcess = true;
        }
    }

    public class FCRoadQueue : IExposable
    {
        public int nextRoadTick;
        public int daysBetweenTicks;
        protected RoadDef roadDef;

        public bool shouldUpdateSettlementsToProcess = true;

        public List<PlanetTile> settlementsFromTiles = new List<PlanetTile>();
        public List<PlanetTile> settlementsToTiles = new List<PlanetTile>();
        IEnumerator<FCRoadPath> roadPathIterator;

        public RoadDef RoadDef {
            get {
                return roadDef;
            }
            set
            {
                roadDef = value;
                ResetPaths();
            }
        }

        public List<FCRoadPath> roadPaths = new List<FCRoadPath>();

        private class UnionFind
        {
            private int[] parent;
            private int[] rank;

            public UnionFind(int size)
            {
                parent = new int[size];
                rank = new int[size];
                for (int i = 0; i < size; i++)
                    parent[i] = i;
            }

            public int FindRoot(int x)
            {
                if (parent[x] != x)
                    parent[x] = FindRoot(parent[x]);
                return parent[x];
            }

            public bool TryMerge(int x, int y)
            {
                int rootX = FindRoot(x);
                int rootY = FindRoot(y);
                if (rootX == rootY)
                    return false;
                if (rank[rootX] < rank[rootY])
                    parent[rootX] = rootY;
                else if (rank[rootX] > rank[rootY])
                    parent[rootY] = rootX;
                else
                {
                    parent[rootY] = rootX;
                    rank[rootX]++;
                }
                return true;
            }
        }

        private struct Edge
        {
            public int fromTile;
            public int toTile;
            public float cost;
        }

        private static float ComputePathCost(int from, int to, PlanetLayer layer)
        {
            var fromTile = new PlanetTile(from, layer);
            var toTile = new PlanetTile(to, layer);
            using (var pathing = new WorldPathing(layer))
            {
                WorldPath path = pathing.FindPath(fromTile, toTile, null);
                float cost = path.Found ? path.TotalCost : float.MaxValue;
                path.Dispose();
                return cost;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref nextRoadTick, "nextRoadTick");
            Scribe_Values.Look(ref daysBetweenTicks, "daysBetweenTicks");
            Scribe_Defs.Look(ref roadDef, "roadDef");
            Scribe_Collections.Look(ref roadPaths, "roadPaths", LookMode.Deep);
            if (roadPaths == null)
                roadPaths = new List<FCRoadPath>();
        }

        public FCRoadQueue(RoadDef roadDef, int daysBetweenTicks)
        {
            this.roadDef = roadDef;
            this.daysBetweenTicks = daysBetweenTicks;
            this.nextRoadTick = this.nextRoadTick == 0 ? Find.TickManager.TicksGame : this.nextRoadTick;
        }

        public void AddPath(FCRoadPath path)
        {
            this.roadPaths.Add(path);
        }

        /// <summary>
        /// Updates processed settlements if needed and that it is time to build the segements,
        /// then builds the segments
        /// </summary>
        public bool BuildRoadSegments()
        {
            if (this.shouldUpdateSettlementsToProcess)
            {
                this.UpdateSettlementsToProcess();
                this.shouldUpdateSettlementsToProcess = false;
            }
            if (this.nextRoadTick > Find.TickManager.TicksGame)
                return false;

            this.nextRoadTick += GenDate.TicksPerDay * this.daysBetweenTicks;
            return this.ForceBuildRoadSegments();
        }

        bool ForceBuildRoadSegments()
        {
            bool built = false;
            foreach (FCRoadPath path in roadPaths)
            {
                built |= path.BuildSegment(this.roadDef);
            }
            if(built)
            {
                var mainPlanetLayer = Find.WorldGrid.PlanetLayers[0];
                Find.World.renderer.SetDirty<WorldDrawLayer_Roads>(mainPlanetLayer);
                Find.World.renderer.SetDirty<WorldDrawLayer_Paths>(mainPlanetLayer);
                
                // Send blue notification when roads are built
                string roadTypeName = this.roadDef?.LabelCap ?? "Road";
                Find.LetterStack.ReceiveLetter(
                    "Roads Built", 
                    $"Your Empire settlements have constructed new {roadTypeName} segments connecting your territories.", 
                    LetterDefOf.PositiveEvent
                );
            }
            return built;
        }

        public void DrawPaths()
        {
            foreach (FCRoadPath path in roadPaths)
            {
                path.DrawPath();
            }
        }

        IEnumerator<FCRoadPath> ProcessPath()
        {
            // Phase 0: Purge incomplete paths so the MST can re-optimize
            // the network when settlements change. Partially-built road tiles
            // remain on the world map but no further effort is spent on them.
            roadPaths.RemoveAll(p => !p.IsCompleted);

            // Phase 1: Collect all unique tile IDs from both settlement lists
            HashSet<int> allTileSet = new HashSet<int>();
            foreach (PlanetTile tile in this.settlementsFromTiles)
                allTileSet.Add(tile.tileId);
            foreach (PlanetTile tile in this.settlementsToTiles)
                allTileSet.Add(tile.tileId);

            List<int> allTiles = new List<int>(allTileSet);
            int n = allTiles.Count;

            if (n < 2)
                yield break;

            // Build index mapping for Union-Find
            Dictionary<int, int> tileToIndex = new Dictionary<int, int>(n);
            for (int i = 0; i < n; i++)
                tileToIndex[allTiles[i]] = i;

            // Compute all pairwise pathfinding costs (accounts for existing roads)
            var mainPlanetLayer = Find.WorldGrid.PlanetLayers[0];
            List<Edge> edges = new List<Edge>(n * (n - 1) / 2);
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    float cost = ComputePathCost(allTiles[i], allTiles[j], mainPlanetLayer);
                    edges.Add(new Edge
                    {
                        fromTile = allTiles[i],
                        toTile = allTiles[j],
                        cost = cost
                    });
                }
            }

            // Sort edges by cost (Kruskal's algorithm)
            edges.Sort((a, b) => a.cost.CompareTo(b.cost));

            // Select MST edges using Union-Find
            UnionFind uf = new UnionFind(n);
            List<Edge> mstEdges = new List<Edge>(n - 1);

            foreach (Edge edge in edges)
            {
                if (edge.cost >= float.MaxValue)
                    break; // Remaining edges are unreachable (different landmasses)

                int idxA = tileToIndex[edge.fromTile];
                int idxB = tileToIndex[edge.toTile];

                if (uf.TryMerge(idxA, idxB))
                {
                    mstEdges.Add(edge);
                    if (mstEdges.Count == n - 1)
                        break;
                }
            }

            // Phase 2: Yield MST edges that don't already have completed road paths
            foreach (Edge edge in mstEdges)
            {
                int from = edge.fromTile;
                int to = edge.toTile;

                bool alreadyExists = this.roadPaths.Any(path =>
                    path.IsCompleted &&
                    ((path.From == from && path.To == to) ||
                     (path.From == to && path.To == from)));

                if (!alreadyExists)
                    yield return new FCRoadPath(from, to);
            }
        }

        public void UpdateSettlementsToProcess()
        {
            settlementsFromTiles.Clear();
            settlementsToTiles.Clear();

            FactionFC fC = FactionCache.FactionComp;
            foreach (WorldSettlementFC settlement in fC.settlements)
            {
                settlementsFromTiles.Add(settlement.Tile);
            }
            foreach (Settlement settlement in Find.World.worldObjects.Settlements)
            {
                if (FCRoadBuilder.IsValidRoadTarget(settlement))
                {
                    settlementsToTiles.Add(settlement.Tile);
                }
            }

            roadPathIterator = ProcessPath();
        }

        public void ProcessOnePath()
        {
            if (this.roadPathIterator == null)
                this.roadPathIterator = ProcessPath();

            if (this.roadPathIterator.MoveNext())
                this.roadPaths.Add(this.roadPathIterator.Current);
        }

        /// <summary>
        /// Resets the paths progress. Does not recalculate the paths.
        /// </summary>
        public void ResetPaths()
        {
            foreach (FCRoadPath path in this.roadPaths)
            {
                path.ResetProgress();
            }
        }
    }

    public class FCRoadPath : IExposable
    {
        public WorldPath Path { get; protected set; }
        public int From { get; protected set; }
        public int To { get; protected set; }

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
                bool needsRecache;
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(lastTile, out needsRecache);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(tile, out needsRecache);
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
