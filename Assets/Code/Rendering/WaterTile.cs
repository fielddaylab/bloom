using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {
    public class WaterTile : BatchedComponent {
        [NonSerialized] public int TileIndex;

        public MeshRenderer SurfaceRenderer;
        public DecorationRenderer EdgeRenderer;
    }

    static public class WaterTileUtility {
        static private readonly Tile.TileDataMaskPredicate<ushort> HeightDiffDelegate = (in ushort c, in ushort a) => {
            return a < c;
        };

        static public void UpdateWaterfallEdges(WaterTile tile, SimGridState grid, WaterMaterialData materialData) {
            DecorationUtility.ClearDecorations(tile.EdgeRenderer);

            TileAdjacencyMask mask = Tile.GatherAdjacencyMask(tile.TileIndex, grid.Terrain.Height, grid.HexSize, HeightDiffDelegate);
            foreach(var dir in mask) {
                int turns = (int) dir - (int) TileDirection.S;
                Quaternion rot = Quaternion.Euler(0, turns * -60, 0);
                DecorationUtility.AddDecoration(tile.EdgeRenderer, null, Matrix4x4.TRS(default, rot, Vector3.one));
            }
        }
    }
}