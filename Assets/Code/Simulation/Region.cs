using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {
    public struct RegionInfo {
        public const int MaxRegions = 5;

        public HexGridSubregion GridArea;
        public ushort MaxHeight;
        public RegionId Id;
        public UnsafeSpan<RegionEdgeInfo> Edges;
        public UnsafeSpan<ushort> WaterEdges;
        public int Age;
    }

    public struct RegionEdgeInfo {
        public ushort Index;
        public TileAdjacencyMask Directions;
        public TileCorner SharedCornerCW;
        public TileCorner SharedCornerCCW;
    }

    public delegate void RegionTileHandlerDelegate(ushort regionIndex, int tileIndex);

    static public class RegionUtility {
        static private TextId[] s_RegionNameTable = new TextId[] {
            "region.hill.name", "region.forest.name", "region.prairie.name", "region.wetlands.name", "region.urban.name"
        };

        static private TextId[] s_RegionLongNameTable = new TextId[] {
            "region.hill.name.long", "region.forest.name.long", "region.prairie.name.long", "region.wetlands.name.long", "region.urban.name.long"
        };

        static public TextId GetName(ushort regionIndex) {
            return regionIndex >= s_RegionNameTable.Length ? default : s_RegionNameTable[regionIndex];
        }

        static public TextId GetNameLong(ushort regionIndex) {
            return regionIndex >= s_RegionLongNameTable.Length ? default : s_RegionLongNameTable[regionIndex];
        }

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