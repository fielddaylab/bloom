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
using Zavala.Rendering;

namespace Zavala.Roads
{
    [SharedStateInitOrder(10)]
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public RoadBuffers Roads;
        [NonSerialized] public RingBuffer<RoadInstanceController> RoadObjects; // The physical instances of road prefabs
        [NonSerialized] public RingBuffer<RoadDestinationInfo> Destinations;
        [NonSerialized] public RingBuffer<RoadSourceInfo> Sources;

        public RoadLibrary Library;
        [NonSerialized] public Dictionary<uint, List<ResourceSupplierProxy>> ExportDepotMap; // Maps region index to export depots in that region

        public bool UpdateNeeded; // TEMP for testing; should probably use a more robust signal. Set every time the road system is updated.

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            UpdateNeeded = true;
            SimGridState gridState = ZavalaGame.SimGrid;
            Roads.Create(gridState.HexSize);
            RoadObjects = new RingBuffer<RoadInstanceController>(64, RingBufferMode.Expand);
            Destinations = new RingBuffer<RoadDestinationInfo>(32, RingBufferMode.Expand);
            Sources = new RingBuffer<RoadSourceInfo>(32, RingBufferMode.Expand);
            ExportDepotMap = new Dictionary<uint, List<ResourceSupplierProxy>>();
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // Registration
    }

    [Flags]
    public enum RoadDestinationMask : ushort {
        Manure = (ushort) ResourceMask.Manure,
        MFertilizer = (ushort) ResourceMask.MFertilizer,
        DFertilizer = (ushort) ResourceMask.DFertilizer,
        Grain = (ushort) ResourceMask.Grain,
        Milk = (ushort) ResourceMask.Milk,
        Tollbooth = Milk << 1,
        Export = Milk << 2
    }

    public struct RoadSourceInfo {
        public ushort TileIdx;
        public ushort RegionIdx;
        public RoadDestinationMask Filter;
        public UnsafeSpan<RoadPathSummary> Connections;
    }

    public struct RoadDestinationInfo {
        public ushort TileIdx;
        public ushort RegionIdx;
        public RoadDestinationMask Type;
    }

    public struct RoadPathSummary {
        public ushort DestinationIdx; // destination index
        public ushort ProxyConnectionIdx; // the proxy tile through which this connection is enabled, e.g. Export Depot tile index
        public UnsafeSpan<ushort> Tiles;
        public ushort RegionsCrossed;
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
            get { return (ushort) Math.Max(Tiles.Length - 1, 0); }
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
        IsAnchor = 0x01, // roads, suppliers/buyers (used so that suppliers/buyers do not act as a road tile)
        IsRoad = 0x02, // roads
        IsSource = 0x04, // solely for sources
        IsDestination = 0x08, // solely for destinations
    
        IsConnectionEndpoint = IsSource | IsDestination
    }

    static public class RoadUtility
    {
        static public readonly Predicate<RoadSourceInfo, ushort> FindSourceByTileIndex = (s, i) => s.TileIdx == i;
        static public readonly Predicate<RoadDestinationInfo, ushort> FindDestinationByTileIndex = (d, i) => d.TileIdx == i;

        static public unsafe RoadPathSummary IsConnected(RoadNetwork network, HexGridSize gridSize, int tileIdxA, int tileIdxB) {
            if (tileIdxA == tileIdxB) {
                return new RoadPathSummary() {
                    DestinationIdx = (ushort)tileIdxB,
                    Flags = RoadPathFlags.ForceConnection,
                    ProxyConnectionIdx = Tile.InvalidIndex16,
                };
            }

            // at this point, every tile index has a list of indexes of tiles they are connected to.
            int startIdx = network.Sources.FindIndex(FindSourceByTileIndex, (ushort) tileIdxA);
            int endIdx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) tileIdxB);
            if (startIdx < 0 || endIdx < 0) {
                return default;
            }

            RoadSourceInfo srcInfo = network.Sources[startIdx];
            RoadDestinationInfo destInfo = network.Destinations[endIdx];

            // if these don't overlap, then don't bother checking
            if ((srcInfo.Filter & destInfo.Type) == 0) {
                return default;
            }
            
            UnsafeSpan<RoadPathSummary> aConnections = srcInfo.Connections;

            // First pass: check if connected directly (takes precedence over export depot)
            for (int i = 0; i < aConnections.Length; i++) {
                if (aConnections[i].DestinationIdx == tileIdxB) {
                    // All info contained in connection
                    return aConnections[i];
                }
            }

            // Second pass: check if connected through export depots if NOT in same region
            ushort currRegion = srcInfo.RegionIdx;
            if (currRegion != destInfo.RegionIdx) {
                if (network.ExportDepotMap.ContainsKey(currRegion)) {
                    List<ResourceSupplierProxy> relevantDepots = network.ExportDepotMap[currRegion];

                    for (int i = 0; i < aConnections.Length; i++) {
                        foreach (var depot in relevantDepots) {
                            // For each export depot, check if supplier is connected to it.
                            if (aConnections[i].DestinationIdx == depot.Position.TileIndex) {
                                // if supplier is connected to export depot, then we already know the export depot transports a type of resource the supplier is selling
                                // Create new proxy summary with 1) buyer destination index, 2) distance from supplier to export depot, and 3) proxy index
                                RoadPathSummary proxySummary = aConnections[i];
                                // set buyer as destination
                                proxySummary.DestinationIdx = (ushort) tileIdxB;
                                // note: distance already set
                                // set proxy index
                                proxySummary.ProxyConnectionIdx = (ushort) depot.Position.TileIndex;
                                proxySummary.Flags |= RoadPathFlags.Proxy;

                                return proxySummary;
                            }
                        }
                    }
                }
            }

            return default;
        }

        #region Staging/Removal

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

        static public void FinalizeRoad(RoadNetwork network, SimGridState grid, BuildingPools pools, int tileIndex, bool isEndpoint, Material holoMat) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            RoadUtility.MergeStagedRoadMask(ref tileInfo);
            tileInfo.Flags |= RoadFlags.IsAnchor; // roads may connect with other roads
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

                // temporarily render the build as holo
                var matSwap = newRoad.GetComponent<MaterialSwap>();
                if (matSwap) { matSwap.SetMaterial(holoMat); }

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
            centerTileInfo.Flags &= ~(RoadFlags.IsRoad | RoadFlags.IsAnchor);

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

        #endregion // Staging/Removal

        #region Destinations

        static public void RegisterDestination(OccupiesTile position, RoadDestinationMask incomingMask) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            Assert.True(incomingMask != 0);

            network.Roads.Info[position.TileIndex].Flags |= RoadFlags.IsAnchor | RoadFlags.IsDestination; // roads may connect with buyers/sellers

            int currentIdx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) position.TileIndex);
            if (currentIdx >= 0) {
                ref RoadDestinationInfo dInfo = ref network.Destinations[currentIdx];
                dInfo.Type |= incomingMask;
            } else {
                RoadDestinationInfo dInfo;
                dInfo.Type = incomingMask;
                dInfo.TileIdx = (ushort) position.TileIndex;
                dInfo.RegionIdx = position.RegionIndex;
                network.Destinations.PushBack(dInfo);
            }
            Debug.Log("[StagingRoad] tile " + position.TileIndex + " is now a road anchor");
        }

        static public void DeregisterDestination(OccupiesTile position) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            if (network != null) {
                ref RoadTileInfo roadInfo = ref network.Roads.Info[position.TileIndex];

                // remove destination flag
                // if we're not an endpoint anymore, then remove anchor flag
                roadInfo.Flags &= ~RoadFlags.IsDestination;
                if ((roadInfo.Flags & RoadFlags.IsConnectionEndpoint) == 0) {
                    roadInfo.Flags &= ~RoadFlags.IsAnchor;
                }

                int destIdx = network.Destinations.FindIndex(FindDestinationByTileIndex, (ushort) position.TileIndex);
                if (destIdx >= 0) {
                    network.Destinations.FastRemoveAt(destIdx);
                }
            }
        }

        #endregion // Destinations

        #region Export Depots

        static public void RegisterExportDepot(ResourceSupplierProxy proxy) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            if (!network.ExportDepotMap.ContainsKey(proxy.Position.RegionIndex)) {
                network.ExportDepotMap.Add(proxy.Position.RegionIndex, new List<ResourceSupplierProxy>());
            }
            List<ResourceSupplierProxy> currentDepots = network.ExportDepotMap[proxy.Position.RegionIndex];
            currentDepots.Add(proxy);
            network.ExportDepotMap[proxy.Position.RegionIndex] = currentDepots;

            RegisterSource(proxy.Position, (RoadDestinationMask) proxy.ProxyMask | RoadDestinationMask.Export);
            RegisterDestination(proxy.Position, (RoadDestinationMask) proxy.ProxyMask | RoadDestinationMask.Export);
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

            DeregisterSource(proxy.Position);
            DeregisterDestination(proxy.Position);
        }

        #endregion // Export Depots

        #region Sources

        static public void RegisterSource(OccupiesTile position, RoadDestinationMask outputMask) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            Assert.True(outputMask != 0);

            network.Roads.Info[position.TileIndex].Flags |= RoadFlags.IsAnchor | RoadFlags.IsSource; // roads may connect with buyers/sellers

            int currentIdx = network.Sources.FindIndex(FindSourceByTileIndex, (ushort) position.TileIndex);
            if (currentIdx >= 0) {
                ref RoadSourceInfo sInfo = ref network.Sources[currentIdx];
                sInfo.Filter |= outputMask;
            } else {
                RoadSourceInfo sInfo;
                sInfo.Filter = outputMask;
                sInfo.TileIdx = (ushort) position.TileIndex;
                sInfo.RegionIdx = position.RegionIndex;
                sInfo.Connections = default;
                network.Sources.PushBack(sInfo);
            }
        }

        static public void DeregisterSource(OccupiesTile position) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            if (network != null) {
                ref RoadTileInfo roadInfo = ref network.Roads.Info[position.TileIndex];

                // remove source flag
                // if we're not an endpoint anymore, then remove anchor flag
                roadInfo.Flags &= ~RoadFlags.IsSource;
                if ((roadInfo.Flags & RoadFlags.IsConnectionEndpoint) == 0) {
                    roadInfo.Flags &= ~RoadFlags.IsAnchor;
                }

                int destIdx = network.Sources.FindIndex(FindSourceByTileIndex, (ushort) position.TileIndex);
                if (destIdx >= 0) {
                    network.Sources.FastRemoveAt(destIdx);
                }
            }
        }

        #endregion // Sources
    }
}