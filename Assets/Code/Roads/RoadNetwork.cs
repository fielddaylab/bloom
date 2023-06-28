using BeauUtil;
using BeauUtil.Graph;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEditor.VersionControl;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;
using static UnityEditor.PlayerSettings;

namespace Zavala.Roads
{
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks
    {
        public GameObject TempRoadPrefab; // TEMP HACK placing roads via prefab for testing

        [NonSerialized] public RoadBuffers Roads;

        [NonSerialized] public Dictionary<int, RingBuffer<RoadPathSummary>> Connections; // key is tile index, values are tiles connected via road

        public bool UpdateNeeded; // TEMP for testing; should probably use a more robust signal. Set every time the road system is updated.

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            Connections = new Dictionary<int, RingBuffer<RoadPathSummary>>();
            UpdateNeeded = false;
            SimGridState gridState = ZavalaGame.SimGrid;
            Roads.Create(gridState.HexSize);
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
        public ushort RegionIndex; // region identifier. used as a mask for sim updates (e.g. update region 1, update region 2, etc)
        public ushort Flags; // copy of tile flags
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

        static public void AddRoad(RoadNetwork network, SimGridState grid, int tileIndex) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            // add a road leading in all directions
            // tileInfo.FlowMask[TileDirection.Self] = true; // doesn't seem to be necessary for road connection assessment

            // TODO: in the future, when implementing the road building, will need to set Flow mask according to flow of the road. Not just everywhere.
            // Example: consider three road pieces. One sits at tile 50. Another road piece is NE of it. The third road piece is SE of that one.
            // Then the first has a FlowMask with NE set to true. The second has SW (because it leads back to the first) and SE as true. And the third has NW as true.
            tileInfo.FlowMask[TileDirection.N] = true;
            tileInfo.FlowMask[TileDirection.S] = true;
            tileInfo.FlowMask[TileDirection.SE] = true;
            tileInfo.FlowMask[TileDirection.SW] = true;
            tileInfo.FlowMask[TileDirection.NE] = true;
            tileInfo.FlowMask[TileDirection.NW] = true;

            network.Roads.Info[tileIndex] = tileInfo;

            // TEMP TESTING add placeholder render of road, snap to tile
            GameObject newRoad = MonoBehaviour.Instantiate(network.TempRoadPrefab);
              
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            worldPos.y += 0.2f;
            newRoad.transform.position = worldPos;

            network.UpdateNeeded = true;
        }

        // TODO: consider pooling road tiles...?

        static public void RemoveRoad(RoadNetwork network, SimGridState grid, int tileIndex) {
            RoadTileInfo tileInfo = network.Roads.Info[tileIndex];

            tileInfo.FlowMask[TileDirection.N] = false;
            tileInfo.FlowMask[TileDirection.S] = false;
            tileInfo.FlowMask[TileDirection.SE] = false;
            tileInfo.FlowMask[TileDirection.SW] = false;
            tileInfo.FlowMask[TileDirection.NE] = false;
            tileInfo.FlowMask[TileDirection.NW] = false;

            network.Roads.Info[tileIndex] = tileInfo;


        }
    }
}