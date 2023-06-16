using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {
    public struct RegionInfo {
        public const int MaxRegions = 16;

        public HexGridSubregion GridArea;
        public ushort MaxHeight;
        public ushort PaletteType;
    }

    public delegate void RegionTileHandlerDelegate(ushort regionIndex, int tileIndex);

    static public class RegionUtility {
        static public Bounds CalculateApproximateWorldBounds(in HexGridSubregion subregion, in HexGridWorldSpace worldSpace, ushort maxHeight, float bottomBuffer, float expand) {
            HexVector minHex = subregion.FastIndexToPos(0);
            HexVector maxHex = subregion.FastIndexToPos((int) (subregion.Size - 1));
            
            Vector3 minWorld = HexVector.ToWorld(minHex, 0, worldSpace);
            minWorld.y -= bottomBuffer;
            Vector3 maxWorld = HexVector.ToWorld(maxHex, maxHeight, worldSpace);

            Vector3 center = (maxWorld + minWorld) / 2;
            Vector3 size = maxWorld - minWorld + worldSpace.Scale;

            Bounds bounds = new Bounds(center, size);
            bounds.Expand(expand);
            return bounds;
        }
    }
}