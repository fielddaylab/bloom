using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zavala.Sim {
    [SharedStateInitOrder(10)]
    public sealed class SimWorldState : SharedStateComponent, IRegistrationCallbacks {
        #region Inspector

        [Header("World Scale")]
        public Vector3 Scale = Vector3.one;
        public Vector3 Offset;

        [Header("Tile Spawning")]
        public TileInstance DefaultTilePrefab;
        public TileInstance DefaultWaterPrefab;

        [Header("Bounds Calculations")]
        public float BottomBounds = 100;
        public float BoundsExpand = 1.5f;

        #endregion // Inspector

        [NonSerialized] public Camera RenderCamera;
        [NonSerialized] public HexGridWorldSpace WorldSpace;
        [NonSerialized] public SimWorldOverlayMask Overlays = SimWorldOverlayMask.Phosphorus;

        // region data

        [NonSerialized] public SimBuffer<Bounds> RegionBounds;
        [NonSerialized] public uint RegionCount; // cached from SimDataComponent
        [NonSerialized] public uint RegionCullingMask;

        // phosphorus data

        [NonSerialized] public PhosphorusRenderState[] Phosphorus = new PhosphorusRenderState[RegionInfo.MaxRegions];

        // instantiated prefabs

        [NonSerialized] public TileInstance[] Tiles;

        // temporary data

        [NonSerialized] public int NewRegions;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            TransformHelper.TryGetCamera(transform, out RenderCamera);
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            WorldSpace = new HexGridWorldSpace(grid.HexSize, Scale, Offset);
            RegionBounds = SimBuffer.Create<Bounds>(grid.HexSize);
            RegionCount = grid.RegionCount;
            RegionCullingMask = 0;

            for(int i = 0; i < RegionInfo.MaxRegions; i++) {
                Phosphorus[i].Create();
            }

            Tiles = new TileInstance[grid.HexSize.Size];
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
                            Vector3 pos = vec.ToWorld(grid.Terrain.Height[i], WorldSpace);
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
        Phosphorus = 0x01
    }
}