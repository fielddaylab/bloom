using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using System;
using UnityEngine;
using Zavala.Rendering;

namespace Zavala.World {
    public class BorderRenderState : SharedStateComponent
    {
        [NonSerialized] public MeshData16<TileVertexFormat> MeshGeneratorA = new MeshData16<TileVertexFormat>(256);
        [NonSerialized] public MeshData16<TileVertexFormat> MeshGeneratorB = new MeshData16<TileVertexFormat>(256);
        [NonSerialized] public RingBuffer<ushort> RegionQueue = new RingBuffer<ushort>(2, RingBufferMode.Expand);

        [Header("Outline Parameters")]
        public float OutlineThickness = 0.2f;
        public float ThickOutlineThickness = 5;
        public float RadiusMuliplier = 1.05f;

        [Header("Outline Rendering")]
        public MeshFilter OutlineFilter;
        public MeshRenderer OutlineRenderer;

        [Header("Blueprint Outline Rendering")]
        public MeshFilter LockedOutlineFilter;
        public MeshRenderer LockedOutlineRenderer;
        public Material LockedOutlineMaterial;

        [DebugMenuFactory]
        static private DMInfo DebugMenu() {
            DMInfo info = new DMInfo("Regions");
            info.AddButton("Refresh Outline Meshes", () => {
                var world = Game.SharedState.Get<SimWorldState>();
                var borders = Game.SharedState.Get<BorderRenderState>();
                for(int i = 0; i < world.RegionCount; i++) {
                    borders.RegionQueue.PushBack((ushort) i);
                }
            }, () => Game.SharedState.TryGet(out BorderRenderState _));
            return info;
        }
    }
}