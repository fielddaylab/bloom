using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using Zavala.Sim;

namespace Zavala.World {
    [SharedStateInitOrder(10)]
    public sealed class SimWorldState : SharedStateComponent, IRegistrationCallbacks {
        

        #region Inspector

        [Header("World Scale")]
        public Vector3 Scale = Vector3.one;
        public Vector3 Offset;

        [Header("Tile Spawning")]
        public TileInstance DefaultWaterPrefab;
        public WaterGroupInstance WaterProxyPrefab;

        [Header("Bounds Calculations")]
        public float BottomBounds = 100;
        public float BoundsExpand = 1.5f;

        #endregion // Inspector

        [NonSerialized] public HexGridWorldSpace WorldSpace;
        [NonSerialized] public SimWorldOverlayMask Overlays = SimWorldOverlayMask.None;

        // region data

        [NonSerialized] public SimBuffer<Bounds> RegionBounds;
        [NonSerialized] public uint RegionCount; // cached from SimDataComponent
        [NonSerialized] public uint RegionCullingMask;

        // phosphorus data

        [NonSerialized] public PhosphorusRenderState[] Phosphorus = new PhosphorusRenderState[RegionInfo.MaxRegions];

        // instantiated prefabs

        [NonSerialized] public TileInstance[] Tiles;
        [NonSerialized] public RegionPrefabPalette[] Palettes;

        // temporary data

        [NonSerialized] public int NewRegions;
        [NonSerialized] public SimWorldSpawnBuffer Spawns;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            SimGridState grid = ZavalaGame.SimGrid;
            WorldSpace = new HexGridWorldSpace(grid.HexSize, Scale, Offset);
            RegionBounds = SimBuffer.Create<Bounds>(grid.HexSize);
            Palettes = new RegionPrefabPalette[RegionInfo.MaxRegions];
            RegionCount = grid.RegionCount;
            RegionCullingMask = 0;

            for(int i = 0; i < RegionInfo.MaxRegions; i++) {
                Phosphorus[i].Create();
            }

            Tiles = new TileInstance[grid.HexSize.Size];
            Spawns.Create();
        }

#if UNITY_EDITOR

        private void OnValidate() {
            if (!Application.isPlaying) {
                return;
            }

            if (!Frame.IsActive(this)) {
                return;
            }

            if (Game.SharedState.TryGet(out SimGridState grid)) {
                if (WorldSpace.Scale != Scale || WorldSpace.Offset != Offset) {
                    WorldSpace = new HexGridWorldSpace(grid.HexSize, Scale, Offset);
                    for (int i = 0; i < Tiles.Length; i++) {
                        if (Tiles[i]) {
                            HexVector vec = grid.HexSize.FastIndexToPos(i);
                            Vector3 pos = HexVector.ToWorld(vec, grid.Terrain.Height[i], WorldSpace);
                            Tiles[i].transform.position = pos;
                        }
                    }
                }
            }
        }

#endif // UNITY_EDITOR
    }

    [Flags]
    public enum SimWorldOverlayMask : uint {
        None = 0x00,
        Phosphorus = 0x01
    }

    static public class SimWorldUtility {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetTileIndexFromWorld(Vector3 worldPos, out int index) {
            return TryGetTileIndexFromWorld(ZavalaGame.SimGrid, ZavalaGame.SimWorld, worldPos, out index);
        }

        static public bool TryGetTileIndexFromWorld(SimGridState grid, SimWorldState world, Vector3 worldPos, out int index) {
            HexVector vec = HexVector.FromWorld(worldPos, world.WorldSpace);
            if (grid.HexSize.IsValidPos(vec)) {
                index = grid.HexSize.FastPosToIndex(vec);
                return true;
            }
            index = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryGetTilePosFromWorld(Vector3 worldPos, out HexVector pos) {
            return TryGetTilePosFromWorld(ZavalaGame.SimGrid, ZavalaGame.SimWorld, worldPos, out pos);
        }

        static public bool TryGetTilePosFromWorld(SimGridState grid, SimWorldState world, Vector3 worldPos, out HexVector pos) {
            pos = HexVector.FromWorld(worldPos, world.WorldSpace);
            return grid.HexSize.IsValidPos(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector3 GetTileCenter(HexVector pos) {
            return GetTileCenter(ZavalaGame.SimGrid, ZavalaGame.SimWorld, pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector3 GetTileCenter(int index) {
            return GetTileCenter(ZavalaGame.SimGrid, ZavalaGame.SimWorld, index);
        }

        static public Vector3 GetTileCenter(SimGridState grid, SimWorldState world, HexVector pos) {
            int index = grid.HexSize.FastPosToIndex(pos);
            return HexVector.ToWorld(pos, grid.Terrain.Height[index], world.WorldSpace);
        }

        static public Vector3 GetTileCenter(SimGridState grid, SimWorldState world, int index) {
            HexVector pos = grid.HexSize.FastIndexToPos(index);
            return HexVector.ToWorld(pos, grid.Terrain.Height[index], world.WorldSpace);
        }

        #region Centroids

        static public Vector3 GetWaterCentroid(SimWorldState worldState, WaterGroupInfo waterGroup) {
            unsafe {
                ushort* tiles = waterGroup.TileIndices;
                return GetTileCentroid(worldState.WorldSpace, tiles, waterGroup.TileCount);
            }
        }

        static public unsafe Vector3 GetTileCentroid(HexGridWorldSpace worldSpace, ushort* tileIndices, int indexCount) {
            if (indexCount <= 0) {
                return default;
            }

            Vector3 accum = default;
            for(int i = 0; i < indexCount; i++) {
                accum += HexVector.ToWorld(tileIndices[i], 0, worldSpace);
            }
            return accum / indexCount;
        }

        #endregion // Centroids
    }

    public struct SimWorldSpawnBuffer {
        public RingBuffer<SpawnRecord<BuildingType>> QueuedBuildings;
        public RingBuffer<SpawnRecord<RegionAsset.TerrainModifier>> QueuedModifiers;

        public void Create() {
            QueuedBuildings = new RingBuffer<SpawnRecord<BuildingType>>();
            QueuedModifiers = new RingBuffer<SpawnRecord<RegionAsset.TerrainModifier>>();
        }
    }

    public struct SpawnRecord<T> {
        public ushort TileIndex;
        public ushort RegionIndex;
        public StringHash32 Id;
        public T Data;
    }
}