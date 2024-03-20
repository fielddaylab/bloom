using System;
using System.Runtime.InteropServices;
using BeauUtil;
using FieldDay;
using FieldDay.Rendering;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.Rendering;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Rendering {
    [SharedStateInitOrder(50)]
    public class PhosphorusHeatMap : SharedStateComponent, IRegistrationCallbacks {
        public Gradient Gradient;
        public MeshRenderer TargetRenderer;
        public MeshFilter TargetFilter;

        [NonSerialized] public Texture2D GradientLUT;
        [NonSerialized] public MeshData16<HeatMapVertex> MeshData;
        [NonSerialized] public OffsetLengthU16[] TileToVertexRangeMap;
        [NonSerialized] public SimBuffer<ushort> TileRegions;
        [NonSerialized] public SimBitSet NewTiles;
        [NonSerialized] public Mesh Mesh;

        [NonSerialized] public SimPhosphorusState Phosphorus;
        [NonSerialized] public HexGridSize GridSize;

        void IRegistrationCallbacks.OnDeregister() {
            UnityHelper.SafeDestroy(ref GradientLUT);
            UnityHelper.SafeDestroy(ref Mesh);

            Shader.SetGlobalTexture("_HeatMapTexture", null);
        }

        void IRegistrationCallbacks.OnRegister() {
            HexGridSize hexSize = Find.State<SimGridState>().HexSize;

            GridSize = hexSize;
            Phosphorus = Find.State<SimPhosphorusState>();

            TileToVertexRangeMap = new OffsetLengthU16[hexSize.Size];
            NewTiles = SimBitSet.Create(hexSize.Size);
            TileRegions = SimBuffer.Create<ushort>(hexSize.Size);
            SimBuffer.Clear(TileRegions, Tile.InvalidIndex16);

            Texture2D lut = LUTUtility.CreateLUT(256, 1);
            lut.hideFlags = HideFlags.DontSave;
            LUTUtility.WriteLUT(Gradient, lut);
            GradientLUT = lut;

            Shader.SetGlobalTexture("_HeatMapTex", lut);

            MeshData = new MeshData16<HeatMapVertex>(512);
            Mesh = new Mesh();
            Mesh.hideFlags = HideFlags.DontSave;
            Mesh.name = "HeatMapTarget";

            TargetFilter.sharedMesh = Mesh;
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying || !Frame.IsActive(this)) {
                return;
            }

            if (GradientLUT != null) {
                LUTUtility.WriteLUT(Gradient, GradientLUT);
            }
        }

#endif // UNITY_EDITOR
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HeatMapVertex {
        [VertexAttr(VertexAttribute.Position)] public Vector3 Position;
        [VertexAttr(VertexAttribute.TexCoord0)] public float Color;
    }

    static public class HeatMapUtility {
        static public void UpdateTile(PhosphorusHeatMap heatMap, int tileIndex, int phosphorusCount) {
            OffsetLengthU16 vertexRange = heatMap.TileToVertexRangeMap[tileIndex];
            MeshData16<HeatMapVertex> meshData = heatMap.MeshData;

            float color = (float) phosphorusCount / PhosphorusSim.MaxPhosphorusPerTile;
            for(int i = vertexRange.Offset; i < vertexRange.End; i++) {
                meshData.Vertex(i).Color = color;
            }
        }

        static public OffsetLengthU16 AddTile(PhosphorusHeatMap heatMap, HexGridWorldSpace world, int tileIndex, ushort regionIndex, float height, int phosphorusCount) {
            Vector3 pos = HexVector.ToWorld(tileIndex, height, world);
            pos.y += 0.2f;
            float color = (float) phosphorusCount / PhosphorusSim.MaxPhosphorusPerTile;

            HeatMapVertex vert = new HeatMapVertex() {
                Position = pos,
                Color = color
            };

            OffsetLengthU16 range = heatMap.TileToVertexRangeMap[tileIndex];
            if (range.Length > 0) {
                heatMap.MeshData.Vertex(range.Offset) = vert;
                heatMap.TileRegions[tileIndex] = regionIndex;
                return range;
            }

            int nextVert = heatMap.MeshData.VertexCount;

            heatMap.MeshData.AddVertex(vert);

            range = new OffsetLengthU16((ushort) nextVert, 1);
            heatMap.TileToVertexRangeMap[tileIndex] = range;
            heatMap.NewTiles[tileIndex] = true;
            heatMap.TileRegions[tileIndex] = regionIndex;
            return range;
        }

        static public void AddEdgeFalloff(PhosphorusHeatMap heatMap, HexGridWorldSpace world, HexGridSize grid, ushort targetRegion, UnsafeSpan<RegionEdgeInfo> edgeIndices, SimBuffer<ushort> heights, SimBitSet nonVoids) {
            foreach(var edge in edgeIndices) {
                int height = heights[edge.Index];
                foreach(var dir in edge.Directions) {
                    if (grid.IsValidIndexOffset(edge.Index, dir, out int neighbor) && !nonVoids[neighbor]) {
                        AddTile(heatMap, world, neighbor, targetRegion, height, 0);
                    }
                }
            }
        }

        static public void ConnectTilesInner(PhosphorusHeatMap heatMap, HexGridSize grid, ushort targetRegion, HexGridSubregion subregion, SimBuffer<ushort> regionInfo, SimBitSet nonVoids) {
            foreach(var idx in subregion) {
                if (regionInfo[idx] != targetRegion) {
                    continue;
                }

                OffsetLengthU16 verts = heatMap.TileToVertexRangeMap[idx];

                int neighbor;
                int lastTri = -1;
                if (grid.IsValidIndexOffset(idx, TileDirection.NW, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion || heatMap.NewTiles[neighbor]) {
                        // cool we can connect this
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.SW, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion || heatMap.NewTiles[neighbor]) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            heatMap.MeshData.AddIndices(heatMap.TileToVertexRangeMap[neighbor].Offset, heatMap.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                        }
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.S, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion || heatMap.NewTiles[neighbor]) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            heatMap.MeshData.AddIndices(heatMap.TileToVertexRangeMap[neighbor].Offset, heatMap.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                        }
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.SE, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion || heatMap.NewTiles[neighbor]) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            heatMap.MeshData.AddIndices(heatMap.TileToVertexRangeMap[neighbor].Offset, heatMap.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                        }
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }
            }
        }

        static public void PatchEdges(PhosphorusHeatMap heatMap, HexGridSize grid, ushort targetRegionStart, SimBuffer<ushort> regionInfo, UnsafeSpan<RegionEdgeInfo> edgeIndices, SimBitSet nonVoids) {
            foreach (var edge in edgeIndices) {
                OffsetLengthU16 verts = heatMap.TileToVertexRangeMap[edge.Index];

                int lastTri = -1;
                for(int i = 0; i <= 6; i++) {
                    TileDirection dir = (TileDirection) (1 + i % 6);
                    if (grid.IsValidIndexOffset(edge.Index, dir, out int neighbor) && nonVoids[neighbor]) {
                        if (regionInfo[neighbor] >= targetRegionStart && heatMap.NewTiles[neighbor]) {
                            // cool we can connect this
                            if (lastTri != -1) {
                                heatMap.MeshData.AddIndices(heatMap.TileToVertexRangeMap[neighbor].Offset, heatMap.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                            }
                        }
                        lastTri = neighbor;
                    }
                }
            }
        }

        static public void PushChanges(PhosphorusHeatMap heatMap) {
            heatMap.MeshData.Upload(heatMap.Mesh);
            heatMap.NewTiles.Clear();
        }
    }
}