using System;
using System.Collections.Generic;
using System.Threading;
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

        // Background MST computation state
        volatile int mstGeneration;
        volatile int completedGeneration = -1;
        List<Edge> computedMSTEdges;

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

        /// <summary>
        /// Runs on a background thread. Computes all pairwise A* pathfinding costs
        /// and builds the MST using Kruskal's algorithm. Results are stored for the
        /// main thread to pick up via ProcessPath.
        ///
        /// Thread-safety notes:
        /// - WorldPathing.FindPath reads Find.World/WorldGrid/WorldReachability and
        ///   PlanetLayer NativeArrays. These are read-only during normal gameplay so
        ///   concurrent access is safe in practice. During game teardown (exit to menu)
        ///   these can be invalidated; we guard against that with a Current.Game null
        ///   check each iteration and catch any residual exceptions.
        /// - WorldPathPool access is synchronized via Harmony patches in
        ///   WorldPathPoolPatches.cs (Monitor lock around Get/Release). The pool's
        ///   internal leak detection may fire an ErrorOnce log due to the background
        ///   thread's borrowed paths inflating the count — this is harmless.
        /// </summary>
        void ComputeMSTBackground(List<int> allTiles, PlanetLayer layer, int generation)
        {
            try
            {
                int n = allTiles.Count;
                Dictionary<int, int> tileToIndex = new Dictionary<int, int>(n);
                for (int i = 0; i < n; i++)
                    tileToIndex[allTiles[i]] = i;

                // Single WorldPathing instance reused for all pairs
                List<Edge> edges = new List<Edge>(n * (n - 1) / 2);
                using (var pathing = new WorldPathing(layer))
                {
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = i + 1; j < n; j++)
                        {
                            // Bail early if superseded or the game is being torn down
                            if (generation != mstGeneration || Current.Game is null)
                                return;

                            var fromTile = new PlanetTile(allTiles[i], layer);
                            var toTile = new PlanetTile(allTiles[j], layer);
                            WorldPath path = pathing.FindPath(fromTile, toTile, null);
                            float cost = path.Found ? path.TotalCost : float.MaxValue;
                            path.Dispose();
                            edges.Add(new Edge
                            {
                                fromTile = allTiles[i],
                                toTile = allTiles[j],
                                cost = cost
                            });
                        }
                    }
                }

                // Bail if superseded or game torn down
                if (generation != mstGeneration || Current.Game is null)
                    return;

                // Kruskal's MST
                edges.Sort((a, b) => a.cost.CompareTo(b.cost));
                UnionFind uf = new UnionFind(n);
                List<Edge> mstEdges = new List<Edge>(n - 1);

                foreach (Edge edge in edges)
                {
                    if (edge.cost >= float.MaxValue)
                        break;

                    int idxA = tileToIndex[edge.fromTile];
                    int idxB = tileToIndex[edge.toTile];

                    if (uf.TryMerge(idxA, idxB))
                    {
                        mstEdges.Add(edge);
                        if (mstEdges.Count == n - 1)
                            break;
                    }
                }

                // Publish results only if still the current generation
                if (generation == mstGeneration)
                {
                    computedMSTEdges = mstEdges;
                    completedGeneration = generation;
                    LogUtil.Message($"Road MST computed on background thread: {edges.Count} edges, {mstEdges.Count} MST edges");
                }
            }
            catch (Exception e)
            {
                LogUtil.Error($"Road MST background computation failed: {e}");
            }
        }

        /// <summary>
        /// Yields FCRoadPath objects from pre-computed MST edges (Phase 4 only).
        /// Called on the main thread after ComputeMSTBackground completes.
        /// </summary>
        IEnumerator<FCRoadPath> ProcessPath()
        {
            if (computedMSTEdges is null)
                yield break;

            foreach (Edge edge in computedMSTEdges)
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

            // Phase 0: Purge incomplete paths and completed paths with inferior
            // road types so the MST can re-optimize the network when settlements
            // change or road tech upgrades.
            roadPaths.RemoveAll(p => !p.IsCompleted ||
                FCRoadPath.IsNewRoadBetter(p.builtRoadDef, this.roadDef));

            // Collect all unique tile IDs
            HashSet<int> allTileSet = new HashSet<int>();
            foreach (PlanetTile tile in settlementsFromTiles)
                allTileSet.Add(tile.tileId);
            foreach (PlanetTile tile in settlementsToTiles)
                allTileSet.Add(tile.tileId);

            List<int> allTiles = new List<int>(allTileSet);
            if (allTiles.Count < 2)
                return;

            // Spawn background thread for MST computation
            var layer = Find.WorldGrid.PlanetLayers[0];
            int generation = ++mstGeneration;
            roadPathIterator = null;

            Thread thread = new Thread(() => ComputeMSTBackground(allTiles, layer, generation));
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Advances the path iterator by one step. Returns true if still working, false if exhausted.
        /// </summary>
        public bool ProcessOnePath()
        {
            // MST still computing on background thread
            if (completedGeneration != mstGeneration)
                return true;

            // MST just completed — create iterator
            if (this.roadPathIterator is null)
                this.roadPathIterator = ProcessPath();

            if (this.roadPathIterator.MoveNext())
            {
                this.roadPaths.Add(this.roadPathIterator.Current);
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
