using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using BeauUtil.Debugger;
using BeauUtil;
using Zavala.Data.Utils;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using System.Xml;

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

                // Clear old connections data
                network.Connections.Clear();

                // Gather new connections data
                foreach (var index in gridSize) {
                    // Run shortest path algorithm
                    RingBuffer<RoadPathSummary> connections = FindConnections(index, network.Roads.Info, network.OutputMasks.Info, network.IntakeMasks.Info, gridSize);

                    // Add newfound connections
                    network.Connections.Add(index, connections);
                }

                MarketData marketData = Game.SharedState.Get<MarketData>();
                MarketUtility.TriggerConnectionTriggers(marketData, m_StateA, gridSize);

                network.UpdateNeeded = false;
            }
        }

        private struct GraphNode
        {
            public uint TileIdx;
            public bool Visited;
            public uint TentativeDistance;

            public GraphNode(uint idx, uint dist) {
                TileIdx = idx;
                Visited = false;
                TentativeDistance = dist;
            }
        }

        /// <summary>
        /// Runs the Dijkstra's shortest tree algorithm
        /// </summary>
        static public RingBuffer<RoadPathSummary> FindConnections(int startIdx, SimBuffer<RoadTileInfo> infoBuffer, SimBuffer<ResourceMask> outputBuffer, SimBuffer<ResourceMask> intakeBuffer, HexGridSize gridSize) {
            RingBuffer<RoadPathSummary> allConnections = new RingBuffer<RoadPathSummary>();

            // TODO: optimize the data structures in this algorithm

            // Run Dijkstra's
            {
                // Create a set of all unvisited nodes 'universalSet'
                List<GraphNode> universalSet = new List<GraphNode>();
                GraphNode currNode = new GraphNode();

                bool startNodeFound = false;
                // Generate graph nodes
                for (int i = 0; i < gridSize.Size; i++) {
                    if (!gridSize.IsValidIndex(i)) { // if bugs arise, may need to make graph nodes for invalid indices as well
                        continue;
                    }

                    // Node is marked unvisited at the start and assigned tentative distance of uint.MaxValue
                    GraphNode newNode = new GraphNode((uint)i, uint.MaxValue);

                    // special case for initial node
                    if (i == startIdx) {
                        // initial node starts with tentative distance of 0
                        newNode.TentativeDistance = 0;

                        // Set initial node as current
                        currNode = newNode;

                        startNodeFound = true;
                    }

                    universalSet.Add(newNode);
                }

                // TODO: do we need to consider a case where start node is not found?
                Assert.True(startNodeFound);

                // At the start, there will always be at least the first node
                bool setExhausted = false;
                bool startingNode = true;

                // Algorithm finishes when the smallest tentative distance among nodes in the unvisited set is infinity (uint.MaxValue), or if the unvisited set is empty
                while (!setExhausted) {
                    // For the current node, check all (valid) unvisited neighbors and calculate tentative distances to current node.
                    {
                        bool isRoad = (infoBuffer[(int)currNode.TileIdx].Flags & RoadFlags.IsRoad) != 0;
                        bool isRoadAnchor = (infoBuffer[(int)currNode.TileIdx].Flags & RoadFlags.IsRoadAnchor) != 0;
                        if (isRoadAnchor && !isRoad && !startingNode) {
                            // stop road path search at a roadAnchor, unless it is also a road or the starting node
                        }
                        else {
                            // Get all tiles connected by roads leading out of current tile
                            List<int> unvisitedNeighborList = new List<int>();

                            // TODO: maybe package this code block into a reusable function (originally repurposed from Tile.GatherAdjacencySetWithIdx<>)
                            RoadTileInfo center = infoBuffer[(int)currNode.TileIdx];
                            HexVector pos = gridSize.FastIndexToPos((int)currNode.TileIdx);
                            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                                HexVector adjPos = HexVector.Offset(pos, dir);
                                if (!gridSize.IsValidPos(adjPos)) {
                                    continue;
                                }

                                // if this tile flows out to curr direction
                                int adjIdx = gridSize.FastPosToIndex(adjPos);
                                if (center.FlowMask[dir]) {
                                    // Distill to only those in the unvisited set
                                    if (SetContains(universalSet, adjIdx)) {
                                        unvisitedNeighborList.Add(adjIdx);
                                    }
                                }
                                else {
                                    // if another tile flows into this tile from curr direction
                                    RoadTileInfo adjInfo = infoBuffer[(int)adjIdx];
                                    TileDirection inverseDir = dir.Reverse();
                                    if (adjInfo.FlowMask[inverseDir]) {
                                        if (SetContains(universalSet, adjIdx)) {
                                            unvisitedNeighborList.Add(adjIdx);
                                        }
                                    }
                                }
                            }

                            // Update distances for all unvisited neighbors
                            for (int i = 0; i < unvisitedNeighborList.Count; i++) {
                                UpdateTentativeDistance(universalSet, unvisitedNeighborList[i], currNode.TentativeDistance + 1);
                            }
                        }

                        // Mark the current node as visited; remove from the unvisited set, add it to allConnections if relevant to each other
                        // Supplier is startIdx. Buyer is currNode.TileIdx
                        if ((outputBuffer[startIdx] & intakeBuffer[(int)currNode.TileIdx]) != 0) {
                            allConnections.PushBack(new RoadPathSummary((int)currNode.TileIdx, true, currNode.TentativeDistance));
                        }
                        // TODO: more sophisticated check if local option. If so, add to list as connected. Else, don't.
                        else if (startIdx == currNode.TileIdx) {
                            allConnections.PushBack(new RoadPathSummary((int)currNode.TileIdx, true, currNode.TentativeDistance));
                        }
                        universalSet.Remove(currNode);

                        // set the unvisited with the smallest tentative distance to current, then repeat while unvisited set is not exhausted
                        GraphNode nextNode;
                        setExhausted = !TryGetNextNode(universalSet, out nextNode);
                        currNode = nextNode;
                        startingNode = false;
                    }
                }
            }

            return allConnections;
        }

        private static void UpdateTentativeDistance(List<GraphNode> universalSet, int tileIdx, uint newDist) {
            for (int i = 0; i < universalSet.Count; i++) {
                if (universalSet[i].TileIdx == tileIdx) {
                    GraphNode toUpdate = universalSet[i];

                    // Compare new distance to current one assigned to neighbor and assign the smaller.
                    if (newDist < toUpdate.TentativeDistance) {
                        toUpdate.TentativeDistance = newDist;
                        universalSet[i] = toUpdate;
                    }
                }
            }
        }

        static private bool SetContains(List<GraphNode> set, int idx) {
            for (int i = 0; i < set.Count; i++) {
                if (set[i].TileIdx == idx) {
                    return true;
                }
            }

            return false;
        }

        static private bool TryGetNextNode(List<GraphNode> universalSet, out GraphNode nextNode) {
            // Algorithm finishes when the smallest tentative distance among nodes in the unvisited set is infinity (uint.MaxValue), or if the unvisited set is empty
            uint lowestVal = uint.MaxValue;
            int lowestIdx = -1;
            for (int i = 0; i < universalSet.Count; i++) {
                if (universalSet[i].TentativeDistance < lowestVal) {
                    lowestVal = universalSet[i].TentativeDistance;
                    lowestIdx = i;
                }
            }

            if (lowestIdx == -1) {
                nextNode = new GraphNode();
                return false;
            }

            nextNode = universalSet[lowestIdx];
            return true;
        }
    }
}