using BeauUtil;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zavala.Rendering {
    static public class TileRendering {
        private struct RadiusOffsetBlock {
            public Vector3 R0;
            public Vector3 R1;
            public Vector3 R2;
            public Vector3 R3;
            public Vector3 R4;
            public Vector3 R5;
        }

        static private RadiusOffsetBlock s_Normalized;
        static private RadiusOffsetBlock s_RadiusOffsets;
        static private float s_Radius;

        private const float AngleIncrement = 60 * Mathf.Deg2Rad;

        static TileRendering() {
            unsafe {
                fixed (Vector3* verts = &s_Normalized.R0) {
                    for (int i = 0; i < 6; i++) {
                        // xz plane
                        verts[i] = new Vector3(Mathf.Cos(AngleIncrement * i), 0, Mathf.Sin(AngleIncrement * i));
                    }
                }
            }

            SetTileRadius(1);
        }

        /// <summary>
        /// Sets the global tile radius.
        /// </summary>
        static public void SetTileRadius(float radius) {
            s_Radius = radius;

            unsafe {
                fixed (Vector3* verts = &s_RadiusOffsets.R0)
                fixed (Vector3* src = &s_Normalized.R0) {
                    for (int i = 0; i < 6; i++) {
                        // xz plane
                        verts[i] = src[i] * radius;
                    }
                }
            }
        }

        /// <summary>
        /// Generates mesh data for a tile at the given origin.
        /// </summary>
        static public void GenerateTileMeshData(Vector3 origin, float radiusMultiplier, Color32 color, MeshData16<TileVertexFormat> meshBuilder) {
            int vertBase = (ushort) meshBuilder.VertexCount;

            meshBuilder.Preallocate(6, 12);

            TileVertexFormat a, b, c, d, e, f, g;
            a.Color = b.Color = c.Color = d.Color = e.Color = f.Color = color;

            a.Position = origin + s_RadiusOffsets.R0 * radiusMultiplier;
            b.Position = origin + s_RadiusOffsets.R1 * radiusMultiplier;
            c.Position = origin + s_RadiusOffsets.R2 * radiusMultiplier;
            d.Position = origin + s_RadiusOffsets.R3 * radiusMultiplier;
            e.Position = origin + s_RadiusOffsets.R4 * radiusMultiplier;
            f.Position = origin + s_RadiusOffsets.R5 * radiusMultiplier;

            meshBuilder.AddVertices(a, b, c, d);
            meshBuilder.AddVertices(e, f);

            meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 2), (ushort) (vertBase + 1));
            meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 3), (ushort) (vertBase + 2));
            meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 4), (ushort) (vertBase + 3));
            meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 5), (ushort) (vertBase + 4));
        }

        /// <summary>
        /// Generates mesh data for a tile outline at the given origin.
        /// </summary>
        static public void GenerateTileBorderMeshData(Vector3 origin, TileAdjacencyMask mask, float radiusMultiplier, float outlineStart, float outlineThickness, Color32 colorStart, Color32 colorEnd, MeshData16<TileVertexFormat> meshBuilder) {
            int vertBase = meshBuilder.VertexCount;

            int count = mask.Count;

            meshBuilder.Preallocate(count * 4, count * 6);

            float outlineDist = s_Radius * radiusMultiplier + outlineStart;
            float outlineDistEnd = outlineDist + outlineThickness;

            TileVertexFormat a, b, c, d;
            a.Color = colorStart;
            b.Color = colorStart;
            c.Color = colorEnd;
            d.Color = colorEnd;

            foreach(var dir in mask) {
                GetNormalizedOffsets(dir, out Vector3 v0, out Vector3 v1);
                a.Position = origin + v0 * outlineDist;
                b.Position = origin + v1 * outlineDist;
                c.Position = origin + v1 * outlineDistEnd;
                d.Position = origin + v0 * outlineDistEnd;

                meshBuilder.AddVertices(a, b, c, d);
                meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 1), (ushort) (vertBase + 2));
                meshBuilder.AddIndices((ushort) (vertBase + 0), (ushort) (vertBase + 2), (ushort) (vertBase + 3));

                vertBase += 4;
            }
        }

        static unsafe private void GetNormalizedOffsets(TileDirection dir, out Vector3 v0, out Vector3 v1) {
            if (dir == TileDirection.Self) {
                v0 = v1 = default;
            }

            fixed(Vector3* verts = &s_Normalized.R0) {
                int idx0 = (4 + (int) dir - 1) % 6;
                int idx1 = (idx0 + 5) % 6;
                v0 = verts[idx0];
                v1 = verts[idx1];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TileVertexFormat {
        [VertexAttr(VertexAttribute.Position)] public Vector3 Position;
        [VertexAttr(VertexAttribute.Color)] public Color32 Color;
    }
}