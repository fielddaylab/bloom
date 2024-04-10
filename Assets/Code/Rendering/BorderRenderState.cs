using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using System;
using UnityEngine;
using Zavala.Rendering;
using Zavala.Sim;

namespace Zavala.World {
    public class BorderRenderState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public MeshData16<TileVertexFormat> MeshGeneratorA = new MeshData16<TileVertexFormat>(256);
        [NonSerialized] public MeshData16<HeatMapVertex> MeshGeneratorB = new MeshData16<HeatMapVertex>(256);

        [NonSerialized] public RingBuffer<ushort> RegionQueue = new RingBuffer<ushort>(2, RingBufferMode.Expand);

        [Header("Outline Parameters")]
        public float OutlineThickness = 0.2f;
        public float RadiusMuliplier = 1.05f;

        [Header("Outline Rendering")]
        public MeshFilter OutlineFilter;
        public MeshRenderer OutlineRenderer;
        public Material OutlineMaterial;

        [Header("Shadow Rendering")]
        public MeshFilter[] ShadowFilters;
        public MeshRenderer[] ShadowRenderers;
        public Material ShadowMaterial;
        public Routine ShadowFade;


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

        public void OnDeregister() {
            ShadowFade.Stop();
        }

        public void OnRegister() {
            ShadowMaterial.color = ShadowMaterial.color.WithAlpha(0.4f);
        }
    }

    public struct ShadowRenderState {
        public MeshData16<HeatMapVertex> MeshData;
        public UnsafeSpan<OffsetLengthU16> TileToVertexRangeMap;
        public UnsafeSpan<ushort> TileRegions;
    }

    static public class BorderRenderUtility {
        //static public void UpdateTile(PhosphorusHeatMap heatMap, int tileIndex, int phosphorusCount) {
        //    OffsetLengthU16 vertexRange = heatMap.TileToVertexRangeMap[tileIndex];
        //    MeshData16<HeatMapVertex> meshData = heatMap.MeshData;

        //    float color = (float)phosphorusCount / PhosphorusSim.MaxPhosphorusPerTile;
        //    for (int i = vertexRange.Offset; i < vertexRange.End; i++) {
        //        meshData.Vertex(i).Color = color;
        //    }
        //}

        static public OffsetLengthU16 AddTile(ShadowRenderState shadow, HexGridWorldSpace world, int tileIndex, ushort regionIndex, float height, float color) {
            Vector3 pos = HexVector.ToWorld(tileIndex, height, world);
            pos.y += 0.2f;

            HeatMapVertex vert = new HeatMapVertex() {
                Position = pos,
                Color = color
            };

            OffsetLengthU16 range = shadow.TileToVertexRangeMap[tileIndex];
            if (range.Length > 0) {
                shadow.MeshData.Vertex(range.Offset) = vert;
                return range;
            }

            int nextVert = shadow.MeshData.VertexCount;

            shadow.MeshData.AddVertex(vert);

            range = new OffsetLengthU16((ushort)nextVert, 1);
            shadow.TileToVertexRangeMap[tileIndex] = range;
            shadow.TileRegions[tileIndex] = regionIndex;
            return range;
        }

        //static public void AddEdgeFalloff(ShadowRenderState shadow, HexGridWorldSpace world, HexGridSize grid, ushort targetRegion, UnsafeSpan<RegionEdgeInfo> edgeIndices, SimBuffer<ushort> heights, SimBitSet nonVoids) {
        //    foreach (var edge in edgeIndices) {
        //        int height = heights[edge.Index];
        //        foreach (var dir in edge.Directions) {
        //            if (grid.IsValidIndexOffset(edge.Index, dir, out int neighbor) && !nonVoids[neighbor]) {
        //                AddTile(shadow, world, neighbor, targetRegion, height, 0);
        //            }
        //        }
        //    }
        //}

        static public void AddEdgeFalloffIgnoreNonVoids(ShadowRenderState shadow, HexGridWorldSpace world, HexGridSize grid, ushort targetRegion, UnsafeSpan<RegionEdgeInfo> edgeIndices, SimBuffer<ushort> heights) {
            foreach (var edge in edgeIndices) {
                int height = heights[edge.Index];
                foreach (var dir in edge.Directions) {
                    if (grid.IsValidIndexOffset(edge.Index, dir, out int neighbor)) {
                        AddTile(shadow, world, neighbor, targetRegion, height, 0);
                    }
                }
            }
        }

        static public void ConnectTilesInner(ShadowRenderState shadow, HexGridSize grid, ushort targetRegion, HexGridSubregion subregion, UnsafeSpan<ushort> regionInfo) {
            foreach (var idx in subregion) {
                if (regionInfo[idx] != targetRegion) {
                    continue;
                }

                OffsetLengthU16 verts = shadow.TileToVertexRangeMap[idx];

                int neighbor;
                int lastTri = -1;
                if (grid.IsValidIndexOffset(idx, TileDirection.NW, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion) {
                        // cool we can connect this
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.SW, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            shadow.MeshData.AddIndices(shadow.TileToVertexRangeMap[neighbor].Offset, shadow.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                        }
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.S, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            shadow.MeshData.AddIndices(shadow.TileToVertexRangeMap[neighbor].Offset, shadow.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
                        }
                        lastTri = neighbor;
                    } else {
                        lastTri = -1;
                    }
                } else {
                    lastTri = -1;
                }

                if (grid.IsValidIndexOffset(idx, TileDirection.SE, out neighbor) && neighbor < idx) {
                    if (regionInfo[neighbor] == targetRegion) {
                        // cool we can connect this
                        if (lastTri != -1) {
                            shadow.MeshData.AddIndices(shadow.TileToVertexRangeMap[neighbor].Offset, shadow.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
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

        //static public void PatchEdges(PhosphorusHeatMap heatMap, HexGridSize grid, ushort targetRegionStart, SimBuffer<ushort> regionInfo, UnsafeSpan<RegionEdgeInfo> edgeIndices, SimBitSet nonVoids) {
        //    foreach (var edge in edgeIndices) {
        //        OffsetLengthU16 verts = heatMap.TileToVertexRangeMap[edge.Index];

        //        int lastTri = -1;
        //        for (int i = 0; i <= 6; i++) {
        //            TileDirection dir = (TileDirection)(1 + i % 6);
        //            if (grid.IsValidIndexOffset(edge.Index, dir, out int neighbor) && nonVoids[neighbor]) {
        //                if (regionInfo[neighbor] >= targetRegionStart && heatMap.NewTiles[neighbor]) {
        //                    // cool we can connect this
        //                    if (lastTri != -1) {
        //                        heatMap.MeshData.AddIndices(heatMap.TileToVertexRangeMap[neighbor].Offset, heatMap.TileToVertexRangeMap[lastTri].Offset, verts.Offset);
        //                    }
        //                }
        //                lastTri = neighbor;
        //            }
        //        }
        //    }
        //}

        static public void PushChanges(ShadowRenderState shadow, ref MeshDataTarget meshTarget, MeshDataUploadFlags flags) {
            shadow.MeshData.Upload(ref meshTarget, flags);
        }
    }
}