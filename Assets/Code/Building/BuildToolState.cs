using System;
using UnityEngine;
using FieldDay.SharedState;
using FieldDay;
using System.Collections.Generic;
using BeauUtil;
using Zavala.Sim;
using Zavala.World;
using Zavala.Roads;
using Leaf.Runtime;
using BeauUtil.Debugger;
using Zavala.Data;

namespace Zavala.Building {
    public struct RoadToolState {
        // [NonSerialized] public bool StartedRoad;
        [NonSerialized] public List<int> TracedTileIdxs;
        [NonSerialized] public int PrevTileIndex; // last known tile used for building roads
        [NonSerialized] public bool Dragging;

        // [NonSerialized] public List<GameObject> StagedBuilds; // visual indicator to player of what they will build, but not finalized on map

        // TODO: implement toll booths
        // [NonSerialized] public TollBooth m_lastKnownToll;

        public void ClearState() {
            PrevTileIndex = -1;
            Dragging = false;
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
    public class BuildToolState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject {
        [NonSerialized] public UserBuildTool ActiveTool = UserBuildTool.None;
        [NonSerialized] public RoadToolState RoadToolState;

        [NonSerialized] public HexVector VecPrev;
        [NonSerialized] public bool VecPrevValid;

        [NonSerialized] public int TotalBuildingsBuilt;
        [NonSerialized] public int NumStoragesBuilt;
        [NonSerialized] public BitSet32 StorageBuiltInRegion;

        [NonSerialized] public int NumDigestersBuilt;
        [NonSerialized] public BitSet32 DigesterBuiltInRegion;

        [NonSerialized] public BitSet32 FarmsConnectedInRegion;
        [NonSerialized] public BitSet32 CityConnectedInRegion;

        [NonSerialized] public bool ToolUpdated;

        // Blocked Tiles (for non-road buildings)
        [NonSerialized] public HashSet<int> BlockedIdxs; // tile indices of non-buildables
        [NonSerialized] public RingBuffer<int> BlockedAdjIdxs; // temp list for gathering tiles adjacent to sources/destinations
        [NonSerialized] public SimBuffer<byte> BlockedTileBuffer;

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("BuildTool");
        }

        public void OnRegister() {
            ClearRoadTool();

            BlockedIdxs = new HashSet<int>(64);
            BlockedAdjIdxs = new RingBuffer<int>(32, RingBufferMode.Expand);
            BlockedTileBuffer = SimBuffer.Create<byte>(ZavalaGame.SimGrid.HexSize);
            StorageBuiltInRegion = default;
            DigesterBuiltInRegion = default;

            FarmsConnectedInRegion = default;
            CityConnectedInRegion = default;

            ZavalaGame.SaveBuffer.RegisterHandler("BuildTool", this);
        }

        /// <summary>
        /// Resets road tool state
        /// </summary>
        public void ClearRoadTool() {
            RoadToolState.ClearState();
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write(TotalBuildingsBuilt);
            writer.Write(NumStoragesBuilt);
            writer.Write(NumDigestersBuilt);

            StorageBuiltInRegion.Unpack(out uint storageRegionBits);
            writer.Write((byte) storageRegionBits);

            DigesterBuiltInRegion.Unpack(out uint digesterRegionBits);
            writer.Write((byte) digesterRegionBits);

            FarmsConnectedInRegion.Unpack(out uint farmsConnectedBits);
            writer.Write((byte) farmsConnectedBits);

            CityConnectedInRegion.Unpack(out uint citiesConnectedBits);
            writer.Write((byte)citiesConnectedBits);

        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            reader.Read(ref TotalBuildingsBuilt);
            reader.Read(ref NumStoragesBuilt);
            reader.Read(ref NumDigestersBuilt);

            StorageBuiltInRegion = new BitSet32(reader.Read<byte>());
            DigesterBuiltInRegion = new BitSet32(reader.Read<byte>());

            CityConnectedInRegion = new BitSet32(reader.Read<byte>());
            FarmsConnectedInRegion = new BitSet32(reader.Read<byte>());
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


        [LeafMember("BuildingBuiltInRegion")]
        public static bool BuildingBuiltInRegionLeaf(uint region, BuildingType type) {
            return BuildingBuiltInRegion(region - 1, type); // 1-indexed to 0-indexed
        }
        public static bool BuildingBuiltInRegion(uint region, BuildingType type) {
            BuildToolState bts = Game.SharedState.Get<BuildToolState>();
            switch (type) {
                case BuildingType.Digester: {
                    return bts.DigesterBuiltInRegion[(int) region];
                }
                case BuildingType.Storage: {
                    return bts.StorageBuiltInRegion[(int) region];
                }
                default: {
                    Log.Warn("[BuildToolUtility] Tried to check if {0} built in region {1}: building {0} not set up!", type, region);
                    return false;
                }
            }
        }
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
            foreach (var index in grid.Regions[grid.CurrRegionIndex].GridArea)
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

        public static void RecalculateBlockedTilesForRoads(SimGridState grid, SimWorldState world, RoadNetwork network, BuildToolState btState) {
            // Collect indices of non-buildable tiles
            btState.BlockedIdxs.Clear();

            //// Copy adjacent into holistic list
            //foreach (int adjIdx in btState.BlockedAdjIdxs) {
            //    btState.BlockedIdxs.Add(adjIdx);
            //}

            // Other non-buildable tiles
            foreach (var index in grid.Regions[grid.CurrRegionIndex].GridArea) {
                if (grid.HexSize.IsValidIndex(index)) {
                    if (!world.Tiles[index]) {
                        continue;
                    }

                    // If non-buildable, add to list
                    if ((grid.Terrain.Info[index].Flags & TerrainFlags.NonBuildable) != 0
                        || ((grid.Terrain.Info[index].Flags & TerrainFlags.IsToll) != 0 && (network.Roads.Info[index].Flags & RoadFlags.IsConnectionEndpoint) == 0)) {
                        btState.BlockedIdxs.Add(index);
                    }
                }
            }

            // Save in the sim buffer
            SimBuffer.Clear<byte>(btState.BlockedTileBuffer);
            foreach (int index in btState.BlockedIdxs) {
                btState.BlockedTileBuffer[index] = 1;
            }
        }
    }

}