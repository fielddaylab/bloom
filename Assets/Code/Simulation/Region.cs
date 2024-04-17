using System;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Leaf.Runtime;
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
        public Color32 BorderColor;
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

        
        static public string GetNameString(int region) {
            if (region < 0) {
                Log.Warn("[RegionUtility] Negative region {0} couldn't be converted to long name", region);
            }
            return Loc.Find(GetNameLong((ushort)(region)));
        }


        static public Bounds CalculateApproximateWorldBounds(in HexGridSubregion subregion, in HexGridWorldSpace worldSpace, ushort maxHeight, float bottomBuffer, float expand) {
            HexVector minHex = subregion.FastIndexToPos(0);
            HexVector maxHex = subregion.FastIndexToPos((int) (subregion.Size - 1));
            
            Vector3 minWorld = HexVector.ToWorld(minHex, 0, worldSpace);
            minWorld.y -= bottomBuffer;
            Vector3 maxWorld = HexVector.ToWorld(maxHex, maxHeight, worldSpace);

            Vector3 center = (maxWorld + minWorld) / 2;
            Vector3 size = maxWorld - minWorld + worldSpace.Scale;
            size.x += expand;
            size.z += expand;

            Bounds bounds = new Bounds(center, size);
            return bounds;
        }

        static public unsafe BoundingSphere CalculateApproximateWorldSphere(UnsafeSpan<RegionEdgeInfo> borders, Vector3 center, in HexGridSize grid, in HexGridWorldSpace worldSpace, ushort maxHeight, float expand) {
            Vector3* edgePos = stackalloc Vector3[(int) borders.Length];

            for(int i = 0; i < borders.Length; i++) {
                edgePos[i] = HexVector.ToWorld(grid.FastIndexToPos(borders[i].Index), maxHeight, worldSpace);
            }

            float radius = 0;
            float tempRadius;
            for(int i = 0; i < borders.Length; i++) {
                tempRadius = Vector3.Distance(center, edgePos[i]);
                if (radius < tempRadius) {
                    radius = tempRadius;
                }
            }

            radius += expand / 2;

            center.y = 0;

            BoundingSphere bounds = new BoundingSphere(center, radius);
            return bounds;
        }
    }
}