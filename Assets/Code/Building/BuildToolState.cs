using System;
using UnityEngine;
using FieldDay.SharedState;
using FieldDay;
using System.Collections.Generic;
using BeauUtil;
using Zavala.Sim;
using Zavala.World;
using Zavala.Roads;

namespace Zavala.Building {
    public struct RoadToolState {
        // [NonSerialized] public bool StartedRoad;
        [NonSerialized] public List<int> TracedTileIdxs;
        [NonSerialized] public int PrevTileIndex; // last known tile used for building roads

        // [NonSerialized] public List<GameObject> StagedBuilds; // visual indicator to player of what they will build, but not finalized on map

        // TODO: implement toll booths
        // [NonSerialized] public TollBooth m_lastKnownToll;

        public void ClearState() {
            PrevTileIndex = -1;
            if (TracedTileIdxs == null) {
                TracedTileIdxs = new List<int>();
                // StagedBuilds = new List<GameObject>();
            }
            else {
                TracedTileIdxs.Clear();
                // StagedBuilds.Clear();
            }
        }
    }

    [SharedStateInitOrder(10)]
    public class BuildToolState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public UserBuildTool ActiveTool = UserBuildTool.None;
        [NonSerialized] public RoadToolState RoadToolState;

        [NonSerialized] public HexVector VecPrev;
        [NonSerialized] public bool VecPrevValid;

        [NonSerialized] public bool ToolUpdated;

        // Blocked Tiles (for non-road buildings)
        [NonSerialized] public HashSet<int> BlockedIdxs; // tile indices of non-buildables
        [NonSerialized] public RingBuffer<int> BlockedAdjIdxs; // temp list for gathering tiles adjacent to sources/destinations
        [NonSerialized] public SimBuffer<byte> BlockedTileBuffer;

        public void OnDeregister() {
        }

        public void OnRegister() {
            ClearRoadTool();

            BlockedIdxs = new HashSet<int>(64);
            BlockedAdjIdxs = new RingBuffer<int>(32, RingBufferMode.Expand);
            BlockedTileBuffer = SimBuffer.Create<byte>(ZavalaGame.SimGrid.HexSize);
        }

        /// <summary>
        /// Resets road tool state
        /// </summary>
        public void ClearRoadTool() {
            RoadToolState.ClearState();
        }
    }

    public enum UserBuildTool : byte {
        None,
        Destroy,
        Road,
        Storage,
        Digester,
        Skimmer
    }

    public static class BuildToolUtility {
        public static void SetTool(BuildToolState bts, UserBuildTool toolType)
        {
            bts.ActiveTool = toolType;
            bts.ToolUpdated = true;

            if (bts.ActiveTool == UserBuildTool.None)
            {
                Game.Events.Dispatch(GameEvents.BuildToolDeselected);
            }
            else
            {
                Game.Events.Dispatch(GameEvents.BuildToolSelected);
            }
        }

        public static void RecalculateBlockedTiles(SimGridState grid, SimWorldState world, RoadNetwork network, BuildToolState btState)
        {
            // Collect indices of non-buildable tiles
            btState.BlockedIdxs.Clear();

            // Sources and Destinations
            foreach (var dest in network.Destinations)
            {
                if (dest.isExternal || dest.RegionIdx != grid.CurrRegionIndex)
                {
                    continue;
                }

                btState.BlockedIdxs.Add(dest.TileIdx);
            }
            foreach (var src in network.Sources)
            {
                if (src.IsExternal || src.RegionIdx != grid.CurrRegionIndex)
                {
                    continue;
                }

                btState.BlockedIdxs.Add(src.TileIdx);
            }

            // Tiles Adjacent to Sources and Destinations
            btState.BlockedAdjIdxs.Clear();
            foreach (int centerIdx in btState.BlockedIdxs)
            {
                HexVector currPos = grid.HexSize.FastIndexToPos(centerIdx);
                for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++)
                {
                    HexVector adjPos = HexVector.Offset(currPos, dir);
                    if (!grid.HexSize.IsValidPos(adjPos))
                    {
                        continue;
                    }
                    int adjIdx = grid.HexSize.FastPosToIndex(adjPos);

                    btState.BlockedAdjIdxs.PushBack(adjIdx);
                }
            }

            // Copy adjacent into holistic list
            foreach (int adjIdx in btState.BlockedAdjIdxs)
            {
                btState.BlockedIdxs.Add(adjIdx);
            }

            // Other non-buildable tiles
            foreach (var index in grid.HexSize)
            {
                if (grid.HexSize.IsValidIndex(index))
                {
                    if (!world.Tiles[index])
                    {
                        continue;
                    }

                    // If non-buildable, add to list
                    if ((grid.Terrain.Info[index].Flags & TerrainFlags.NonBuildable) != 0)
                    {
                        btState.BlockedIdxs.Add(index);

                    }
                    if ((grid.Terrain.Info[index].Flags & TerrainFlags.IsWater) != 0)
                    {
                        btState.BlockedIdxs.Add(index);
                    }
                    if ((grid.Terrain.Info[index].Flags & TerrainFlags.IsOccupied) != 0)
                    {
                        btState.BlockedIdxs.Add(index);
                    }
                    if ((network.Roads.Info[index].Flags & RoadFlags.IsRoad) != 0)
                    {
                        btState.BlockedIdxs.Add(index);
                    }
                }
            }

            // Save in the sim buffer
            SimBuffer.Clear<byte>(btState.BlockedTileBuffer);
            foreach (int index in btState.BlockedIdxs)
            {
                btState.BlockedTileBuffer[index] = 1;
            }
        }
    }

}