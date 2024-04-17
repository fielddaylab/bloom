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
        [NonSerialized] public WaterDepthObject DepthObject;
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

        static public void UpdateDepthObject(WaterTile tile, SimGridState grid) {
            if (!tile.DepthObject) {
                return;
            }
            int idx = tile.TileIndex;
            if (!grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.NW)]) {
                return;
            }
            if (!grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.SW)]) {
                return;
            }
            if (!grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.S)]) {
                return;
            }
            UnityHelper.SafeDestroyGO(ref tile.DepthObject);
        }

        static public void TrySpawnDepthObject(WaterTile tile, SimGridState grid, WaterDepthObject depthObj, Material waterMaterial) {
            if (tile.DepthObject) {
                return;
            }
            int idx = tile.TileIndex;
            if (grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.NW)] 
                && grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.SW)]
                && grid.Terrain.NonVoidTiles[grid.HexSize.OffsetIndexFrom(idx, TileDirection.S)]) {
                return;
            }
            tile.DepthObject = GameObject.Instantiate(depthObj, tile.transform.position, Quaternion.identity);
            tile.DepthObject.WaterRenderer.material = waterMaterial;
        }
    }
}