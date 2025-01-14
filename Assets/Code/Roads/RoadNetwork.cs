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
using UnityEditor;
using Zavala.Data;

namespace Zavala.Roads
{
    [SharedStateInitOrder(10)]
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject, ISaveStatePostLoad
    {
        [NonSerialized] public RoadBuffers Roads;
        [NonSerialized] public RingBuffer<RoadInstanceController> RoadObjects; // The physical instances of road prefabs
        [NonSerialized] public RingBuffer<RoadDestinationInfo> Destinations;
        [NonSerialized] public RingBuffer<RoadSourceInfo> Sources;

        public RoadLibrary Library;
        [NonSerialized] public Dictionary<uint, List<ResourceSupplierProxy>> ExportDepotMap; // Maps region index to export depots in that region

        public bool UpdateNeeded; // TEMP for testing; should probably use a more robust signal. Set every time the road system is updated.

        public readonly ActionEvent OnConnectionsReevaluated = new ActionEvent(4);

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            UpdateNeeded = true;
            SimGridState gridState = ZavalaGame.SimGrid;
            Roads.Create(gridState.HexSize);
            RoadObjects = new RingBuffer<RoadInstanceController>(64, RingBufferMode.Expand);
            Destinations = new RingBuffer<RoadDestinationInfo>(32, RingBufferMode.Expand);
            Sources = new RingBuffer<RoadSourceInfo>(32, RingBufferMode.Expand);
            ExportDepotMap = new Dictionary<uint, List<ResourceSupplierProxy>>();

            ZavalaGame.SaveBuffer.RegisterHandler("Roads", this, 50);
            ZavalaGame.SaveBuffer.RegisterPostLoad(this);
        }

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Roads");
            ZavalaGame.SaveBuffer.DeregisterPostLoad(this);
        }

        #endregion // Registration

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                writer.Write(Roads.Info[idx].FlowMask.Value);
            }
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                reader.Read(ref Roads.Info[idx].FlowMask.Value);
            }
        }

        void ISaveStatePostLoad.PostLoad(SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();

            for (int i = 0; i < consts.DataRegion.Size; i++) {
                int idx = consts.DataRegion.FastIndexToGridIndex(i);
                ref var roadInfo = ref Roads.Info[idx];
                if (!roadInfo.FlowMask.IsEmpty) {
                    SimWorldUtility.QueueVisualUpdate((ushort) idx, VisualUpdateType.Road);
                    if ((roadInfo.Flags & RoadFlags.IsConnectionEndpoint) == 0) {
                        roadInfo.Flags |= RoadFlags.IsAnchor;
                        roadInfo.Flags |= RoadFlags.IsRoad;

                        if ((roadInfo.Flags & RoadFlags.IsTollbooth) == 0) {
                            RoadUtility.CreateRoadObject(this, ZavalaGame.SimGrid, pools, idx, null, false);
                        }
                    }
                }
            }

            UpdateNeeded = true;
        }
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
        public bool IsExternal;
    }

    public struct RoadDestinationInfo {
        public ushort TileIdx;
        public ushort RegionIdx;
        public RoadDestinationMask Type;
        public bool isExternal;
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
        // public TileAdjacencyMask StagingMask; // staged road connections, to be merged with flow mask upon successful road build
        // public TileDirection ForwardStagingDir; // The direction leading to the next road segment when staging (when rewinding, this is used to undo forward step)
        public TileDirection PreserveFlow;
        public ushort RegionIndex; // region identifier. used as a mask for sim updates (e.g. update region 1, update region 2, etc)
        public RoadFlags Flags;
    }

    public struct RoadTileConstructionInfo {
        public TileAdjacencyMask InProgressMask; // staged road connections, to be merged with flow mask upon successful road build
        public TileAdjacencyMask ToCommitMask;
        public TileDirection ForwardStagingDir; // The direction leading to the next road segment when staging (when rewinding, this is used to undo forward step)
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
        IsTollbooth = 0x10, // solely for tollbooths
    
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
            ref RoadTileInfo tileInfo = ref network.Roads.Info[tileIndex];
            ref RoadTileConstructionInfo constructInfo = ref network.Roads.ConstructInfo[tileIndex];
            // tileInfo.FlowMask[TileDirection.Self] = true; // doesn't seem to be necessary for road connection assessment

            Debug.Log("[StagingRoad] Begin staging tile " + tileIndex + " in directions : " + toStage.ToString());

            //tileInfo.StagingMask[toStage] = true;
            //tileInfo.ForwardStagingDir = toStage;

            constructInfo.InProgressMask[toStage] = true;
            constructInfo.ForwardStagingDir = toStage;

            Debug.Log("[StagingRoad] New staging mask for tile " + tileIndex + " : " + constructInfo.InProgressMask.ToString());

            // TODO: Handle ramps appearing on non-road tiles
            RoadUtility.UpdateRoadVisuals(network, tileIndex);

        }

        static public void UnstageRoad(RoadNetwork network, SimGridState grid, int tileIndex) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];
            ref RoadTileConstructionInfo constructInfo = ref network.Roads.ConstructInfo[tileIndex];
            constructInfo.InProgressMask.Clear();

            Debug.Log("[StagingRoad] Unstaged tile " + tileIndex);

            //network.Roads.Info[tileIndex] = tileInfo;

            // remove road object
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();
            RemoveStagedRoadObj(network, pools, tileIndex, !tileInfo.FlowMask.IsEmpty);
        }

        static public void UnstageForward(RoadNetwork network, SimGridState grid, int tileIndex) {
            ref RoadTileInfo tileInfo = ref network.Roads.Info[tileIndex];
            ref RoadTileConstructionInfo constructInfo = ref network.Roads.ConstructInfo[tileIndex];

            constructInfo.InProgressMask[constructInfo.ForwardStagingDir] = false;
            constructInfo.ForwardStagingDir = TileDirection.Self;

            
            Debug.Log("[StagingRoad] Unstaging forward of tile " + tileIndex + " || new dirs: " + constructInfo.InProgressMask.ToString());


            RoadUtility.UpdateRoadVisuals(network, tileIndex);
        }

        static public void FinalizeRoad(RoadNetwork network, SimGridState grid, BuildingPools pools, int tileIndex, bool isEndpoint, Material holoMat) {
            ref RoadTileInfo tileInfo = ref network.Roads.Info[tileIndex];
            ref RoadTileConstructionInfo constructInfo = ref network.Roads.ConstructInfo[tileIndex];
            bool isToll = (grid.Terrain.Info[tileIndex].Flags & TerrainFlags.IsToll) != 0;

            RoadUtility.MergeStagedRoadMask(ref tileInfo, ref constructInfo);
            tileInfo.Flags |= RoadFlags.IsAnchor; // roads may connect with other roads
            if (!isEndpoint || isToll) {
                tileInfo.Flags |= RoadFlags.IsRoad; // endpoints should not act as roads (unless it is a road or toll)
            }


            network.UpdateNeeded = true;

            /*
            // Do not create road objects on endpoints
            if (!isEndpoint || isToll) {
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
            */
        }

        static public void CreateRoadObject(RoadNetwork network, SimGridState grid, BuildingPools pools, int tileIndex, Material holoMat, bool preview = true)
        {
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            RoadInstanceController newRoad = pools.Roads.Alloc(worldPos);
            network.RoadObjects.PushBack(newRoad);

            newRoad.gameObject.SetActive(true);

            if (preview) {
                // temporarily render the build as holo
                var matSwap = newRoad.GetComponent<BuildingPreview>();
                if (matSwap) { matSwap.Preview(holoMat); }
            }

            newRoad.Ramps = Tile.GatherAdjacencySet<ushort, RoadRampType>(tileIndex, grid.Terrain.Height, grid.HexSize, (in ushort c, in ushort a, out RoadRampType o) => {
                if (c < a - 50)
                {
                    o = RoadRampType.Tall;
                    return true;
                }
                else if (c < a)
                {
                    o = RoadRampType.Ramp;
                    return true;
                }
                else
                {
                    o = default;
                    return false;
                }
            });

            RoadUtility.UpdateRoadVisuals(network, tileIndex);
        }

        static public void RemoveStagedRoadObj(RoadNetwork network, BuildingPools pools, int tileIndex, bool someRoadExists)
        {
            if (someRoadExists)
            {
                if (!network.Roads.Info[tileIndex].FlowMask.IsEmpty)
                {
                    RoadUtility.UpdateRoadVisuals(network, tileIndex);
                    return;
                }
            }

            // TODO: differentiate between staged road objs and existing road objs
            for (int i = network.RoadObjects.Count - 1; i >= 0; i--)
            {
                if (network.RoadObjects[i].Position.TileIndex == tileIndex)
                {
                    // TODO: Check if there is nothing after staging mask is removed
                    pools.Roads.Free(network.RoadObjects[i]);
                    network.RoadObjects.RemoveAt(i);
                    break;
                }
            }

            RoadUtility.UpdateRoadVisuals(network, tileIndex);
        }

        static public void UpdateRoadVisuals(RoadNetwork network, int roadTileIndex)
        {
            RoadTileInfo tileInfo = network.Roads.Info[roadTileIndex];
            RoadTileConstructionInfo constructInfo = network.Roads.ConstructInfo[roadTileIndex];
            for (int r = network.RoadObjects.Count - 1; r >= 0; r--)
            {
                if (network.RoadObjects[r].Position.TileIndex == roadTileIndex)
                {
                    RoadVisualUtility.UpdateRoadMesh(network.RoadObjects[r], network.Library, tileInfo.FlowMask, constructInfo.InProgressMask | constructInfo.ToCommitMask);
                }
            }

            SimWorldUtility.QueueVisualUpdate((ushort) roadTileIndex, VisualUpdateType.Road);
        }

        static public void UpdateAllRoadVisuals(RoadNetwork network) {
            for (int r = network.RoadObjects.Count - 1; r >= 0; r--) {
                int roadTileIndex = network.RoadObjects[r].Position.TileIndex;
                RoadTileInfo tileInfo = network.Roads.Info[roadTileIndex];
                RoadTileConstructionInfo constructInfo = network.Roads.ConstructInfo[roadTileIndex];
                RoadVisualUtility.UpdateRoadMesh(network.RoadObjects[r], network.Library, tileInfo.FlowMask, constructInfo.InProgressMask | constructInfo.ToCommitMask);
                SimWorldUtility.QueueVisualUpdate((ushort) roadTileIndex, VisualUpdateType.Road);
            }
        }

        static public void RemoveRoad(RoadNetwork network, SimGridState grid, BuildingPools pools, int tileIndex, bool removeInleading, out TileAdjacencyMask inleadingDirsRemoved) {

            // Erase record from adj nodes

            if (removeInleading)
            {
                RemoveInleadingRoads(network, grid, tileIndex, out inleadingDirsRemoved);
            }
            else
            {
                inleadingDirsRemoved = default;
            }

            /*
            // differentiate between staged road objs and existing road objs
            for (int i = network.RoadObjects.Count - 1; i >= 0; i--)
            {
                if (network.RoadObjects[i].GetComponent<OccupiesTile>().TileIndex == tileIndex)
                {
                    // Check if there is nothing after staging mask is removed
                    pools.Roads.Free(network.RoadObjects[i]);
                    network.RoadObjects.RemoveAt(i);
                    break;
                }
            }
            */

            ref RoadTileInfo centerTileInfo = ref network.Roads.Info[tileIndex];
            centerTileInfo.FlowMask.Clear();
            if (centerTileInfo.PreserveFlow != 0) {
                centerTileInfo.FlowMask |= centerTileInfo.PreserveFlow;
            }
            centerTileInfo.Flags &= ~(RoadFlags.IsRoad | RoadFlags.IsAnchor);

            network.UpdateNeeded = true;
        }

        static public void RemoveInleadingRoads(RoadNetwork network, SimGridState grid, int tileIndex, out TileAdjacencyMask inleadingDirsRemoved) {
            inleadingDirsRemoved = default;

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

                TileDirection currDir = dir;
                TileDirection adjDir = currDir.Reverse();

                adjTileInfo.FlowMask[adjDir] = false;

                inleadingDirsRemoved |= currDir;

                // Update prev road rendering
                RoadUtility.UpdateRoadVisuals(network, adjIdx);
            }
        }

        static public void MergeStagedRoadMask(ref RoadTileInfo info, ref RoadTileConstructionInfo constructInfo) {
            // For each direction, set the flow to true if either existing road or staged road unlocks that direction
            Debug.Log("[StagingRoad] Merging staged mask...");

            info.FlowMask |= constructInfo.InProgressMask;
            constructInfo.ToCommitMask |= constructInfo.InProgressMask;

            constructInfo.InProgressMask.Clear();
            constructInfo.ForwardStagingDir = TileDirection.Self; // necessary?


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
                dInfo.isExternal = position.IsExternal;
            }
            else {
                RoadDestinationInfo dInfo;
                dInfo.Type = incomingMask;
                dInfo.TileIdx = (ushort) position.TileIndex;
                dInfo.RegionIdx = position.RegionIndex;
                dInfo.isExternal = position.IsExternal;
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

        #region Fixed Road Tiles

        static public void RegisterFixedRoad(RoadInstanceController road) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            SimGridState grid = ZavalaGame.SimGrid;
            Assert.NotNull(network);

            network.RoadObjects.PushBack(road);
            int roadTileIndex = road.Position.TileIndex;
            RoadTileInfo tileInfo = network.Roads.Info[roadTileIndex];
            RoadTileConstructionInfo constructInfo = network.Roads.ConstructInfo[roadTileIndex];

            road.Ramps = Tile.GatherAdjacencySet<ushort, RoadRampType>(roadTileIndex, grid.Terrain.Height, grid.HexSize, (in ushort c, in ushort a, out RoadRampType o) => {
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


            RoadVisualUtility.UpdateRoadMesh(road, network.Library, tileInfo.FlowMask, constructInfo.InProgressMask | constructInfo.ToCommitMask);
            SimWorldUtility.QueueVisualUpdate((ushort) roadTileIndex, VisualUpdateType.Road);
        }

        static public void DeregisterFixedRoad(RoadInstanceController road) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            network.RoadObjects.FastRemove(road);
            SimWorldUtility.QueueVisualUpdate((ushort) road.Position.TileIndex, VisualUpdateType.Road);
        }

        #endregion // Fixed Road Tiles

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
                sInfo.IsExternal = position.IsExternal;
            } else {
                RoadSourceInfo sInfo;
                sInfo.Filter = outputMask;
                sInfo.TileIdx = (ushort) position.TileIndex;
                sInfo.RegionIdx = position.RegionIndex;
                sInfo.Connections = default;
                sInfo.IsExternal = position.IsExternal;
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