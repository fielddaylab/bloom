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
        static private readonly RadiusOffsetBlock s_RadiusOffsets;
        private const float AngleIncrement = 60 * Mathf.Deg2Rad;

        static TileRendering() {
            SetTileRadius(1);
        }

        /// <summary>
        /// Sets the global tile radius.
        /// </summary>
        static public void SetTileRadius(float radius) {
            unsafe {
                fixed (Vector3* verts = &s_RadiusOffsets.R0) {
                    for (int i = 0; i < 6; i++) {
                        // xz plane
                        verts[i] = new Vector3(radius * Mathf.Cos(AngleIncrement * i), 0, radius * Mathf.Sin(AngleIncrement * i));
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
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TileVertexFormat {
        [VertexAttr(VertexAttribute.Position)] public Vector3 Position;
        [VertexAttr(VertexAttribute.Color)] public Color32 Color;
    }
}