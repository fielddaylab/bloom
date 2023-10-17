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
using System.IO;
using FieldDay.Scripting;

namespace Zavala.Roads
{
    [SharedStateInitOrder(10)]
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public RoadBuffers Roads;
        [NonSerialized] public List<RoadInstanceController> RoadObjects; // The physical instances of road prefabs
        [NonSerialized] public HashSet<int> DestinationIndices;

        [NonSerialized] public Dictionary<int, RingBuffer<RoadPathSummary>> Connections; // key is tile index, values are tiles connected via road

        public RoadLibrary Library;
        public bool UpdateNeeded; // TEMP for testing; should probably use a more robust signal. Set every time the road system is updated.

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            Connections = new Dictionary<int, RingBuffer<RoadPathSummary>>();
            UpdateNeeded = true;
            SimGridState gridState = ZavalaGame.SimGrid;
            Roads.Create(gridState.HexSize);
            RoadObjects = new List<RoadInstanceController>();
            DestinationIndices = new HashSet<int>(64);
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // Registration
    }

    public struct RoadPathSummary
    {
        public int TileIndx; // destination index
        public bool Connected;
        public float Distance;
        // TODO: store actual path

        public RoadPathSummary(int idx, bool connected, float dist) {
            TileIndx = idx;
            Connected = connected;
            Distance = dist;
        }
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
        static public RoadPathSummary IsConnected(RoadNetwork network, HexGridSize gridSize, int tileIdxA, int tileIdxB) {
            RoadPathSummary info;

            // at this point, every tile index has a list of indexes of tiles they are connected to.
            if (network.Connections.ContainsKey(tileIdxA)) {
                RingBuffer<RoadPathSummary> aConnections = network.Connections[tileIdxA];

                for (int i = 0; i < aConnections.Count; i++) {
                    if (aConnections[i].TileIndx == tileIdxB) {
                        // All info contained in connection
                        return aConnections[i];
                    }
                }
            }

            // tile A index not found, has no list of connections
            info.Connected = false;
            info.TileIndx = tileIdxB;

            HexVector a = ZavalaGame.SimGrid.HexSize.FastIndexToPos(tileIdxA);
            HexVector b = ZavalaGame.SimGrid.HexSize.FastIndexToPos(tileIdxB);
            info.Distance = HexVector.EuclidianDistance(a, b);
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
                network.RoadObjects.Add(newRoad);
            }
        }

        static public void RemoveRoad(RoadNetwork network, SimGridState grid, int tileIndex) {

            // Erase record from adj nodes

            RemoveInleadingRoads(network, grid, tileIndex);

            ref RoadTileInfo centerTileInfo = ref network.Roads.Info[tileIndex];
            centerTileInfo.FlowMask.Clear();

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

        static public void RegisterRoadAnchor(OccupiesTile position) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            Assert.NotNull(network);

            network.Roads.Info[position.TileIndex].Flags |= RoadFlags.IsRoadAnchor | RoadFlags.IsRoadDestination; // roads may connect with buyers/sellers
            network.DestinationIndices.Add(position.TileIndex);
            Debug.Log("[StagingRoad] tile " + position.TileIndex + " is now a road anchor");
        }

        static public void DeregisterRoadAnchor(OccupiesTile position) {
            if (Game.IsShuttingDown) {
                return;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            if (network != null && (network.Roads.Info[position.TileIndex].Flags & RoadFlags.IsRoadAnchor) != 0) {
                network.Roads.Info[position.TileIndex].Flags &= ~(RoadFlags.IsRoadAnchor | RoadFlags.IsRoadDestination);
                network.DestinationIndices.Remove(position.TileIndex);
            }
        }

        #endregion // Register
    }
}