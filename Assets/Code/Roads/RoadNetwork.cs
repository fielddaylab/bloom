using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using BeauUtil.Debugger;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;
using Zavala.Building;
using FieldDay.Scripting;
using Zavala.Economy;

namespace Zavala.Roads
{
    [SharedStateInitOrder(10)]
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public RoadBuffers Roads;
        [NonSerialized] public RingBuffer<RoadInstanceController> RoadObjects; // The physical instances of road prefabs
        [NonSerialized] public RingBuffer<RoadDestinationInfo> Destinations;

        public RoadLibrary Library;
        [NonSerialized] public Dictionary<uint, List<ResourceSupplierProxy>> ExportDepotMap; // Maps region index to export depots in that region

        public bool UpdateNeeded; // TEMP for testing; should probably use a more robust signal. Set every time the road system is updated.

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            UpdateNeeded = true;
            SimGridState gridState = ZavalaGame.SimGrid;
            Roads.Create(gridState.HexSize);
            RoadObjects = new RingBuffer<RoadInstanceController>(64, RingBufferMode.Expand);
            Destinations = new RingBuffer<RoadDestinationInfo>(48, RingBufferMode.Expand);
            ExportDepotMap = new Dictionary<uint, List<ResourceSupplierProxy>>();
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // Registration
    }

    [Flags]
    public enum RoadDestinationMask : uint {
        Manure = ResourceMask.Manure,
        MFertilizer = ResourceMask.MFertilizer,
        DFertilizer = ResourceMask.DFertilizer,
        Grain = ResourceMask.Grain,
        Milk = ResourceMask.Milk,
        Tollbooth = Milk << 1,
        Export = Milk << 2
    }

    public struct RoadDestinationInfo {
        public ushort TileIdx;
        public ushort RegionIdx;
        public RoadDestinationMask Type;
        public RoadDestinationMask Filter;
        public UnsafeSpan<RoadPathSummary> Connections;
    }

    public struct RoadPathSummary {
        public ushort DestinationIdx; // destination index
        public ushort ProxyConnectionIdx; // the proxy tile through which this connection is enabled, e.g. Export Depot tile index
        public UnsafeSpan<ushort> Tiles;
        public RoadPathFlags Flags;

        public bool Connected {
            get { return Tiles.Length > 0 || (Flags & RoadPathFlags.ForceConnection) != 0; }
            set {
                if (value) {
                    Flags |= RoadPathFlags.ForceConnection;
                } else {
                    Flags &= ~RoadPathFlags.ForceConnection;
                }
            }
        }

        public ushort Distance {
            get { return (ushort) Tiles.Length; }
        }

        public RoadPathSummary(ushort idx, UnsafeSpan<ushort> tiles, RoadPathFlags flags, ushort proxyConnectionIdx = Tile.InvalidIndex16) {
            DestinationIdx = idx;
            Tiles = tiles;
            Flags = flags;
            ProxyConnectionIdx = proxyConnectionIdx;
        }
    }

    [Flags]
    public enum RoadPathFlags : byte {
        Reversed = 0x01,
        ForceConnection = 0x02,
        Proxy = 0x04
    }

    /// <summary>
    /// Data for each tile indicating outgoing roads
    /// </summary>
    public struct RoadTileInfo
    {
        // May be able to optimize checks if there's an 'IsValid' bool here, to apply to certain types of tiles (suppliers, requesters, roads, etc.)
        public TileAdjacencyMask FlowMask; // valid road connections (flow outward from center to given direction)
        public TileAdjacencyMask StagingMask; // staged road connections, to be merged with flow mask upon successful road build
        public TileDirection ForwardStagingDir; // The direction leading to the next road segment when staging (when rewinding, this is used to undo forward step)
        public ushort RegionIndex; // region identifier. used as a mask for sim updates (e.g. update region 1, update region 2, etc)
        public RoadFlags Flags;
    }

    /// <summary>
    /// Road behavior flags.
    /// </summary>
    [Flags]
    public enum RoadFlags : ushort
    {
        IsRoadAnchor = 0x01, // roads, suppliers/buyers (used so that suppliers/buyers do not act as a road tile)
        IsRoad = 0x02, // roads
        IsRoadDestination = 0x04 // solely for destinations
    }

    static public class RoadUtility
    {
        static public readonly Predicate<RoadDestinationInfo, ushort> FindDestinationByTileIndex = (d, i) => d.TileIdx == i;

        static public unsafe RoadPathSummary IsConnected(RoadNetwork network, HexGridSize gridSize, int tileIdxA, int tileIdxB) {
            // idxA is supplier, idxB is buyer
            RoadPathSummary info;

            // at this point, every tile index has a list of indexes of tiles they are connected to.
            int startIdx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) tileIdxA);
            if (startIdx >= 0) {
                UnsafeSpan<RoadPathSummary> aConnections = network.Destinations[startIdx].Connections;

                // First pass: check if connected directly (takes precedence over export depot)
                for (int i = 0; i < aConnections.Length; i++) {
                    if (aConnections[i].DestinationIdx == tileIdxB) {
                        // All info contained in connection
                        return aConnections[i];
                    }
                }

                // Second pass: check if connected through export depots if NOT in same region
                uint currRegion = ZavalaGame.SimGrid.Terrain.Regions[tileIdxA];
                if (currRegion != ZavalaGame.SimGrid.Terrain.Regions[tileIdxB]) {
                    if (network.ExportDepotMap.ContainsKey(currRegion)) {
                        List<ResourceSupplierProxy> relevantDepots = network.ExportDepotMap[currRegion];

                        for (int i = 0; i < aConnections.Length; i++) {
                            foreach (var depot in relevantDepots) {
                                // For each export depot, check if supplier is connected to it.
                                if (aConnections[i].DestinationIdx == depot.Position.TileIndex) {
                                    // Create new proxy summary with 1) buyer destination index, 2) distance from supplier to export depot, and 3) proxy bool
                                    RoadPathSummary proxySummary = aConnections[i];
                                    // set buyer as destination
                                    proxySummary.DestinationIdx = (ushort) tileIdxB;
                                    // note: distance already set
                                    // set proxy bool
                                    proxySummary.ProxyConnectionIdx = (ushort) depot.Position.TileIndex;

                                    return proxySummary;
                                }
                            }
                        }
                    }
                }
                
            }

            // tile A index not found, has no list of connections
            info = default;
            return info;
        }

        static public void StageRoad(RoadNetwork network, SimGridState grid, int tileIndex, TileDirection toStage) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            // tileInfo.FlowMask[TileDirection.Self] = true; // doesn't seem to be necessary for road connection assessment

            Debug.Log("[StagingRoad] Begin staging tile " + tileIndex + " in directions : " + toStage.ToString());

            tileInfo.StagingMask[toStage] = true;
            tileInfo.ForwardStagingDir = toStage;

            Debug.Log("[StagingRoad] New staging mask for tile " + tileIndex + " : " + tileInfo.StagingMask.ToString());

            network.Roads.Info[tileIndex] = tileInfo;
        }

        static public void UnstageRoad(RoadNetwork network, SimGridState grid, int tileIndex) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            tileInfo.StagingMask.Clear();

            Debug.Log("[StagingRoad] Unstaged tile " + tileIndex);

            network.Roads.Info[tileIndex] = tileInfo;
        }

        static public void UnstageForward(RoadNetwork network, SimGridState grid, int tileIndex) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            tileInfo.StagingMask[tileInfo.ForwardStagingDir] = false;
            tileInfo.ForwardStagingDir = TileDirection.Self;

            Debug.Log("[StagingRoad] Unstaging forward of tile " + tileIndex + " || new dirs: " + tileInfo.StagingMask.ToString());

            network.Roads.Info[tileIndex] = tileInfo;
        }

        static public void FinalizeRoad(RoadNetwork network, SimGridState grid, BuildingPools pools, int tileIndex, bool isEndpoint) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            RoadUtility.MergeStagedRoadMask(ref tileInfo);
            tileInfo.Flags |= RoadFlags.IsRoadAnchor; // roads may connect with other roads
            if (!isEndpoint) {
                tileInfo.Flags |= RoadFlags.IsRoad; // endpoints should not act as roads (unless it is a road)
            }

            network.Roads.Info[tileIndex] = tileInfo;

            network.UpdateNeeded = true;

            // Do not create road objects on endpoints
            if (!isEndpoint) {
                // TEMP add road, snap to tile
                HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
                Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
                RoadInstanceController newRoad = pools.Roads.Alloc(worldPos);
                network.RoadObjects.PushBack(newRoad);

                newRoad.Ramps = Tile.GatherAdjacencySet<ushort, RoadRampType>(tileIndex, grid.Terrain.Height, grid.HexSize, (in ushort c, in ushort a, out RoadRampType o) => {
                    if (c < a - 50) {
                        o = RoadRampType.Tall;
                        return true;
                    } else if (c < a) {
                        o = RoadRampType.Ramp;
                        return true;
                    } else {
                        o = default;
                        return false;
                    }
                });

                ZavalaGame.SimWorld.QueuedVisualUpdates.PushBack(new VisualUpdateRecord() {
                    TileIndex = (ushort) tileIndex,
                    Type = VisualUpdateType.Road
                });
            }
        }

        static public void RemoveRoad(RoadNetwork network, SimGridState grid, int tileIndex) {

            // Erase record from adj nodes

            RemoveInleadingRoads(network, grid, tileIndex);

            ref RoadTileInfo centerTileInfo = ref network.Roads.Info[tileIndex];
            centerTileInfo.FlowMask.Clear();
            centerTileInfo.Flags &= ~(RoadFlags.IsRoad | RoadFlags.IsRoadAnchor);

            network.UpdateNeeded = true;
        }

        static public void RemoveInleadingRoads(RoadNetwork network, SimGridState grid, int tileIndex) {

            RoadTileInfo centerTileInfo = network.Roads.Info[tileIndex];

            // Erase record from adj nodes

            HexVector currPos = grid.HexSize.FastIndexToPos(tileIndex);
            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                if (!centerTileInfo.FlowMask[dir]) {
                    // no record in adj tile
                    continue;
                }

                HexVector adjPos = HexVector.Offset(currPos, dir);
                if (!grid.HexSize.IsValidPos(adjPos)) {
                    continue;
                }
                int adjIdx = grid.HexSize.FastPosToIndex(adjPos);

                ref RoadTileInfo adjTileInfo = ref network.Roads.Info[adjIdx];

                TileDirection currDir = dir; // to stage into curr road
                TileDirection adjDir = currDir.Reverse(); // to stage into prev road

                adjTileInfo.FlowMask[adjDir] = false;

                // Update prev road rendering
                for (int r = network.RoadObjects.Count - 1; r >= 0; r--) {
                    if (network.RoadObjects[r].GetComponent<OccupiesTile>().TileIndex == adjIdx) {
                        RoadVisualUtility.UpdateRoadMesh(network.RoadObjects[r], network.Library, network.Roads.Info[adjIdx].FlowMask);
                        //network.RoadObjects[r].UpdateSegmentVisuals(network.Roads.Info[adjIdx].FlowMask);
                    }
                }
            }
        }

        static public void MergeStagedRoadMask(ref RoadTileInfo info) {
            // For each direction, set the flow to true if either existing road or staged road unlocks that direction
            Debug.Log("[StagingRoad] Merging staged mask...");

            info.FlowMask |= info.StagingMask;

            info.StagingMask.Clear();
            info.ForwardStagingDir = TileDirection.Self; // necessary?

            Debug.Log("[StagingRoad] Final merged mask: " + info.FlowMask.ToString());
        }

        #region Register

        static public void RegisterRoadDestination(OccupiesTile position, RoadDestinationMask pathFilter, RoadDestinationMask incomingMask) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            network.Roads.Info[position.TileIndex].Flags |= RoadFlags.IsRoadAnchor | RoadFlags.IsRoadDestination; // roads may connect with buyers/sellers

            int currentIdx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) position.TileIndex);
            if (currentIdx >= 0) {
                ref RoadDestinationInfo dInfo = ref network.Destinations[currentIdx];
                dInfo.Filter |= pathFilter;
                dInfo.Type |= incomingMask;
            } else {
                RoadDestinationInfo dInfo;
                dInfo.Filter = pathFilter;
                dInfo.Type = incomingMask;
                dInfo.TileIdx = (ushort) position.TileIndex;
                dInfo.RegionIdx = position.RegionIndex;
                dInfo.Connections = default;
                network.Destinations.PushBack(dInfo);
            }
            Debug.Log("[StagingRoad] tile " + position.TileIndex + " is now a road anchor");
        }

        static public void DeregisterRoadDestination(OccupiesTile position) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            if (network != null && (network.Roads.Info[position.TileIndex].Flags & RoadFlags.IsRoadDestination) != 0) {
                network.Roads.Info[position.TileIndex].Flags &= ~(RoadFlags.IsRoadAnchor | RoadFlags.IsRoadDestination);
                int idx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) position.TileIndex);
                if (idx >= 0) {
                    network.Destinations.FastRemoveAt(idx);
                }
            }
        }

        static public void RegisterExportDepot(ResourceSupplierProxy proxy) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            if (!network.ExportDepotMap.ContainsKey(proxy.Position.RegionIndex)) {
                network.ExportDepotMap.Add(proxy.Position.RegionIndex, new List<ResourceSupplierProxy>());
            }
            List<ResourceSupplierProxy> currentDepots = network.ExportDepotMap[proxy.Position.RegionIndex];
            currentDepots.Add(proxy);
            network.ExportDepotMap[proxy.Position.RegionIndex] = currentDepots;
        }

        static public void DeregisterExportDepot(ResourceSupplierProxy proxy) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            if (network.ExportDepotMap.ContainsKey(proxy.Position.RegionIndex)) {
                List<ResourceSupplierProxy> currentDepots = network.ExportDepotMap[proxy.Position.RegionIndex];
                currentDepots.Remove(proxy);
                network.ExportDepotMap[proxy.Position.RegionIndex] = currentDepots;
            }
        }

        #endregion // Register
    }
}