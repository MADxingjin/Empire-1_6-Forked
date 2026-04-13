using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class FCRoadQueue : IExposable
    {
        public int nextRoadTick;
        public int daysBetweenTicks;
        protected RoadDef roadDef;

        public bool shouldUpdateSettlementsToProcess = true;

        public List<PlanetTile> settlementsFromTiles = new List<PlanetTile>();
        public List<PlanetTile> settlementsToTiles = new List<PlanetTile>();
        IEnumerator<FCRoadPath> roadPathIterator;

        public RoadDef RoadDef
        {
            get
            {
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
            if (built)
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
            // Phase 0: Purge incomplete paths and completed paths with inferior
            // road types so the MST can re-optimize the network when settlements
            // change or road tech upgrades. Partially-built road tiles remain on
            // the world map but no further effort is spent on them.
            roadPaths.RemoveAll(p => !p.IsCompleted ||
                FCRoadPath.IsNewRoadBetter(p.builtRoadDef, this.roadDef));

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
                    yield return null; // Spread A* pathfinds across ticks
                }
            }
            LogUtil.Message($"Road MST computed for {n * (n - 1) / 2} edges");

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
                    !FCRoadPath.IsNewRoadBetter(path.builtRoadDef, this.roadDef) &&
                    ((path.From == from && path.To == to) ||
                     (path.From == to && path.To == from)));

                if (!alreadyExists)
                {
                    FCRoadPath newPath = new FCRoadPath(from, to);
                    newPath.builtRoadDef = this.roadDef;
                    yield return newPath;
                }
            }
            LogUtil.Message($"Road paths fully processed through ProcessPath");
        }

        public void UpdateSettlementsToProcess()
        {
            settlementsFromTiles.Clear();
            settlementsToTiles.Clear();

            FactionFC fC = FactionCache.FactionComp;
            foreach (WorldSettlementFC settlement in fC.settlements)
            {
                if (!settlement.Tile.Layer.IsRootSurface)
                    continue;
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

        /// <summary>
        /// Advances the path iterator by one step. Returns true if a path was added, false if exhausted.
        /// </summary>
        public bool ProcessOnePath()
        {
            if (this.roadPathIterator == null)
                this.roadPathIterator = ProcessPath();

            if (this.roadPathIterator.MoveNext())
            {
                FCRoadPath path = this.roadPathIterator.Current;
                if (path is object)
                    this.roadPaths.Add(path);
                return true;
            }
            return false;
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
}