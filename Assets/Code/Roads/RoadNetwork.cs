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
using static UnityEditor.PlayerSettings;

namespace Zavala.Roads
{
    public sealed class RoadNetwork : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public RoadBuffers Roads;

        [NonSerialized] public Dictionary<int, RingBuffer<RoadPathSummary>> Connections; // key is tile index, values are tiles connected via road

        #region Registration

        void IRegistrationCallbacks.OnRegister() {
            Connections = new Dictionary<int, RingBuffer<RoadPathSummary>>();
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
    }

    /// <summary>
    /// Data for each tile indicating outgoing roads
    /// </summary>
    public struct RoadTileInfo
    {
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
    }
}