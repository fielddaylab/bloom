using FieldDay.Systems;
using FieldDay;
using Zavala.Economy;
using Zavala.Sim;
using BeauUtil.Debugger;
using BeauUtil;

namespace Zavala.Roads
{
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class RoadSystem : SharedStateSystemBehaviour<RoadNetwork, SimGridState>
    {
        public override void ProcessWork(float deltaTime) {
            // TODO: implement trigger to notify of needed update
            bool updateNeeded = m_StateA.UpdateNeeded;

            if (updateNeeded) {
                // Need to recalculate whether a given tile is connected with any other given tile via road,
                // then cache into the RoadNetwork Connections member.

                // Get shared state
                HexGridSize gridSize = m_StateB.HexSize;
                RoadNetwork network = m_StateA;

                using (Profiling.Time("Road Network Rebuild", ProfileTimeUnits.Microseconds)) {

                    // Clear old connections data
                    network.Roads.PathIndexAllocator.Reset();
                    network.Roads.PathInfoAllocator.Reset();

                    for (int i = 0; i < m_StateA.Sources.Count; i++) {
                        m_StateA.Sources[i].Connections = default;
                    }

                    if (m_StateA.Destinations.Count > 0 && m_StateA.Sources.Count > 0) {
                        TraversalResources resources;
                        resources.NodeQueue = new UnsafeQueue<ushort>(Frame.AllocSpan<ushort>(1024));
                        resources.TraversalMap = Frame.AllocSpan<TileDirection>((int) gridSize.Size);
                        resources.SummaryAccumulator = Frame.AllocSpan<RoadPathSummary>(m_StateA.Destinations.Count);
                        resources.PathNodeAccumulator = Frame.AllocSpan<ushort>((int) gridSize.Size);
                        resources.SummaryAllocator = network.Roads.PathInfoAllocator;
                        resources.PathNodeAllocator = network.Roads.PathIndexAllocator;
                        resources.Sources = network.Sources;
                        resources.Destinations = network.Destinations;
                        resources.RegionInfo = m_StateB.Terrain.Regions;

                        Unsafe.Clear(resources.TraversalMap);

                        for (int i = 0; i < m_StateA.Sources.Count; i++) {
                            ref RoadSourceInfo src = ref m_StateA.Sources[i];
                            src.Connections = FindConnections(src.TileIdx, src.Filter, network.Roads.Info, resources, i, gridSize);

                            resources.Clear();
                        }
                    }
                }

                MarketData marketData = Game.SharedState.Get<MarketData>();
                MarketUtility.TriggerConnectionTriggers(marketData, m_StateA, gridSize);

                network.OnConnectionsReevaluated.Invoke();
                network.UpdateNeeded = false;
            }
        }

        public override void Initialize() {
            base.Initialize();

            RoadLibrary.OnUpdated.Register(ForceRefreshRamps);
        }

        public override void Shutdown() {
            base.Shutdown();

            RoadLibrary.OnUpdated.Deregister(ForceRefreshRamps);
        }

        private void ForceRefreshRamps() {
            RoadUtility.UpdateAllRoadVisuals(m_StateA);
        }

        private struct TraversalResources {
            public UnsafeQueue<ushort> NodeQueue;
            public UnsafeSpan<TileDirection> TraversalMap;

            public UnsafeSpan<RoadPathSummary> SummaryAccumulator;
            public SimArena<RoadPathSummary> SummaryAllocator;

            public UnsafeSpan<ushort> PathNodeAccumulator;
            public SimArena<ushort> PathNodeAllocator;

            public SimBuffer<ushort> RegionInfo;
            public RingBuffer<RoadSourceInfo> Sources;
            public RingBuffer<RoadDestinationInfo> Destinations;

            public void Clear() {
                Unsafe.Clear(TraversalMap);
                NodeQueue.Clear();
            }
        }

        private struct UnsafeQueue<T> where T : unmanaged {
            public UnsafeSpan<T> Data;
            public int Head;
            public int Tail;
            public int Count;

            public UnsafeQueue(UnsafeSpan<T> backing) {
                Data = backing;
                Head = Tail = Count = 0;
            }

            public void Clear() {
                Head = Tail = Count = 0;
            }

            public void Enqueue(T item) {
                Assert.True(Count < Data.Length);
                Data[Tail] = item;
                Tail = (Tail + 1) % Data.Length;
                Count++;
            }

            public T Dequeue() {
                Assert.True(Count > 0);
                T item = Data[Head];
                Count--;
                Head = (Head + 1) % Data.Length;
                return item;
            }
        }

        /// <summary>
        /// BFS search (roads are equally weighted)
        /// </summary>
        static private unsafe UnsafeSpan<RoadPathSummary> FindConnections(ushort startIdx, RoadDestinationMask destinationMask, SimBuffer<RoadTileInfo> infoBuffer, TraversalResources resources, int reverseSearchCount, HexGridSize gridSize) {
            int pathsFound = 0;

            resources.NodeQueue.Enqueue(startIdx);
            while(pathsFound < resources.SummaryAccumulator.Length && resources.NodeQueue.Count > 0) {
                ushort idx = resources.NodeQueue.Dequeue();

                RoadTileInfo tileInfo = infoBuffer[idx];
                //bool skipOutgoing = false;

                // check destination
                if (idx != startIdx && (tileInfo.Flags & RoadFlags.IsDestination) != 0) {
                    RoadDestinationInfo destInfo = resources.Destinations.Find(RoadUtility.FindDestinationByTileIndex, idx);
                    Assert.True(destInfo.Type != 0);
                    if ((destinationMask & destInfo.Type) != 0) {

                        // TODO: find reversed paths

                        UnsafeSpan<ushort> allocatedPath = TraverseBackwards(resources, idx, gridSize, out ushort crossedBorders);
                        ref RoadPathSummary summary = ref resources.SummaryAccumulator[pathsFound++];
                        summary.ProxyConnectionIdx = Tile.InvalidIndex16;
                        summary.RegionsCrossed = (byte) crossedBorders;
                        summary.DestinationIdx = idx;
                        summary.Flags = 0;
                        summary.Tiles = allocatedPath;

                        if (pathsFound >= resources.SummaryAccumulator.Length) {
                            break;
                        }
                    }

                    // if tollbooth, we can continue traversing
                    // otherwise, terminate here. no paths leading through buildings
                    //skipOutgoing = (destInfo.Type & RoadDestinationMask.Tollbooth) == 0;
                }
                
                //if (skipOutgoing) {
                //    continue;
                //}

                TileAdjacencyMask flow = tileInfo.FlowMask;
                if (!flow.IsEmpty) {
                    for (TileDirection dir = (TileDirection) 1; dir < TileDirection.COUNT; dir++) {
                        if (!flow.Has(dir)) {
                            continue;
                        }

                        ushort nextIdx = (ushort) gridSize.OffsetIndexFrom(idx, dir);
                        if (nextIdx != startIdx && resources.TraversalMap[nextIdx] == 0) {
                            resources.TraversalMap[nextIdx] = dir;
                            resources.NodeQueue.Enqueue(nextIdx);
                        }
                    }
                }
            }
        
            if (pathsFound <= 0) {
                return default;
            }

            UnsafeSpan<RoadPathSummary> allocatedSummaries = resources.SummaryAllocator.Alloc((uint) pathsFound);
            Unsafe.CopyArray(resources.SummaryAccumulator.Ptr, pathsFound, allocatedSummaries.Ptr);
            return allocatedSummaries;
        }

        static private unsafe UnsafeSpan<ushort> TraverseBackwards(in TraversalResources resources, ushort start, HexGridSize gridSize, out ushort bordersCrossed) {
            UnsafeSpan<ushort> writeBuf = resources.PathNodeAccumulator;
            UnsafeSpan<TileDirection> map = resources.TraversalMap;
            int writeOffset = writeBuf.Length - 1;

            bordersCrossed = 0;
            ushort idx = start;
            ushort currentRegion = resources.RegionInfo[idx];
            writeBuf[writeOffset--] = idx;
            while (map[idx] != 0) {
                if (currentRegion != resources.RegionInfo[idx]) {
                    currentRegion = resources.RegionInfo[idx];
                    bordersCrossed++;
                }
                idx = (ushort) gridSize.OffsetIndexFrom(idx, map[idx].Reverse());
                writeBuf[writeOffset--] = idx;
            }

            int head = writeOffset + 1;
            int length = writeBuf.Length - head;

            UnsafeSpan<ushort> allocated = resources.PathNodeAllocator.Alloc((uint) length);
            Unsafe.CopyArray(writeBuf.Ptr + head, length, allocated.Ptr);
            return allocated;
        }
    }
}