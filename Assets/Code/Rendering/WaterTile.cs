using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Zavala.World {
    public class WaterTile : BatchedComponent {
        [NonSerialized] public int TileIndex;

        public MeshRenderer SurfaceRenderer;
        public MeshRenderer[] EdgeRenderers;
    }
}