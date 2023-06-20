using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using BeauUtil.Debugger;
using BeauUtil;

namespace Zavala.Roads
{
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class RoadSystem : SharedStateSystemBehaviour<RoadNetwork, SimGridState>
    {
        private struct TileRoadData
        {
            // any road connection data here
            public int TileIdx;
        }

        // delegate for extracting road flow from one tile to another
        static private readonly Tile.TileDataMapWithIdxDelegate<RoadTileInfo, TileRoadData> ExtractRoadConnections = (in RoadTileInfo c, in RoadTileInfo a, in int adjIdx, out TileRoadData o) => {
            // as long as passed the mask, should be added to consideration during graph traversal

            o = new TileRoadData();
            o.TileIdx = adjIdx;
            return false;
        };

        public override void ProcessWork(float deltaTime) {
            // TODO: implement trigger to notify of needed update
            bool updateNeeded = false;

            if (updateNeeded) {
                // Need to recalculate whether a given tile is connected with any other given tile via road,
                // then cache into the RoadNetwork Connections member.

                // Get shared state
                HexGridSize gridSize = m_StateB.HexSize;
                RoadNetwork network = m_StateA;

                // Clear old connections data
                network.Connections.Clear();

                // Gather new connections data
                foreach (var index in gridSize) {
                    // Run shortest path algorithm
                    RingBuffer<RoadPathSummary> connections = FindConnections(index, network.Roads.Info, gridSize);

                    // Add newfound connections
                    network.Connections.Add(index, connections);
                }
            }
        }


        /// <summary>
        /// Runs the Dijkstra's connection algorithm
        /// </summary>
        static public RingBuffer<RoadPathSummary> FindConnections(int startIdx, SimBuffer<RoadTileInfo> infoBuffer, HexGridSize gridSize) {
            RingBuffer<RoadPathSummary> allConnections = new RingBuffer<RoadPathSummary>();

            // TODO: previous to now, assign infoBuffer whenever a road is created/destroyed

            // Run Dijkstra's

            // Get all tiles connected by roads leading out of current tile
            int currIdx = startIdx;
            TileAdjacencyDataSet<TileRoadData> currConnections = Tile.GatherAdjacencySetWithIdx<RoadTileInfo, TileRoadData>(currIdx, infoBuffer, gridSize, ExtractRoadConnections);

            // Continue algorithm with currConnections[i].TileIdx


            return allConnections;
        }


        /*
        // delegate for extracting height different from one tile to another
        static private readonly Tile.TileDataMapDelegate<RoadTileInfo, RoadConnectionData> ExtractRoadConnections = (in RoadTileInfo c, in RoadTileInfo a, out RoadConnectionData o) => {
            if (a.Height < ushort.MaxValue && a.Height < c.Height + SimilarHeightThreshold) {
                o.HeightDiff = (short)(a.Height - c.Height);
                o.IsWater = (a.Flags & (ushort)TerrainFlags.IsWater) != 0;
                return true;
            }

            o.HeightDiff = 0;
            o.IsWater = false;
            return false;

            o.Connected = false;
            return false;
        };
        */
    }
}