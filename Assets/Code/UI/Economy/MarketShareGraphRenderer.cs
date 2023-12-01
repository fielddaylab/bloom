using System;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class MarketShareGraphRenderer : ShapeGraphic {
        public Color ManureColor;
        public Color CFertilizerColor;
        public Color DFertilizerColor;
        public Color OutlineColor;
        public float OutlineThickness;
        public int MaxTicks = 20;

        private GraphPoint[] m_ManurePoints;
        private GraphPoint[] m_CFertilizerPoints;
        private GraphPoint[] m_DFertilizerPoints;
        private readonly float[] m_AvgPositions = new float[3];

        [NonSerialized] private int m_TickCount;

        protected override unsafe void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            if (m_TickCount <= 1) {
                Array.Clear(m_AvgPositions, 0, m_AvgPositions.Length);
                return;
            }

            Rect rect = GetPixelAdjustedRect();

            float columnWidth = rect.width / MaxTicks;

            Vector2* cFertilizerPoints = stackalloc Vector2[m_TickCount * 2];
            Vector2* manurePoints = stackalloc Vector2[m_TickCount * 2];
            Vector2* dFertilizerPoints = stackalloc Vector2[m_TickCount * 2];

            float maxValue = 0;

            float right = rect.xMax;
            for(int i = 0; i < m_TickCount; i++) {
                int vertexOffset = i * 2;

                float cFertilizerVal = m_CFertilizerPoints[i].Value;
                cFertilizerPoints[vertexOffset] = new Vector2(right - i * columnWidth, cFertilizerVal);

                float manureVal = m_ManurePoints[i].Value;
                manurePoints[vertexOffset] = new Vector2(right - i * columnWidth, manureVal + cFertilizerVal);

                float dFertilizerVal = m_DFertilizerPoints[i].Value;
                dFertilizerPoints[vertexOffset] = new Vector2(right - i * columnWidth, dFertilizerVal + manureVal + cFertilizerVal);

                maxValue = Math.Max(maxValue, cFertilizerVal + manureVal + dFertilizerVal);
            }

            maxValue = Math.Max(4, 2 + (float) Math.Ceiling(maxValue / 4) * 4);

            float scaling = rect.height / maxValue;
            float baseY = rect.yMin;

            // generate actual y values and outline points
            for(int i = 0; i < m_TickCount; i++) {
                int vertexOffset = i * 2;

                ref float cFert = ref cFertilizerPoints[vertexOffset].y;
                ref float manure = ref manurePoints[vertexOffset].y;
                ref float dFert = ref dFertilizerPoints[vertexOffset].y;

                Scale(ref cFert, baseY, scaling);
                Scale(ref manure, baseY, scaling);
                Scale(ref dFert, baseY, scaling);

                cFertilizerPoints[vertexOffset + 1] = cFertilizerPoints[vertexOffset];
                manurePoints[vertexOffset + 1] = manurePoints[vertexOffset];
                dFertilizerPoints[vertexOffset + 1] = dFertilizerPoints[vertexOffset];

                OffsetForOutline(ref cFertilizerPoints[vertexOffset + 1].y, 0, OutlineThickness);
                OffsetForOutline(ref manurePoints[vertexOffset + 1].y, cFert, OutlineThickness);
                OffsetForOutline(ref dFertilizerPoints[vertexOffset + 1].y, dFert, OutlineThickness);
            }

            for(int i = 0; i < m_TickCount; i++) {
                int vertOffset = i * 2;
                Vector3 baseline = cFertilizerPoints[vertOffset];
                baseline.y = baseY;
                
                // cfert region
                vh.AddVert(baseline, CFertilizerColor, m_TextureRegion.UVCenter);
                vh.AddVert(cFertilizerPoints[vertOffset + 1], CFertilizerColor, m_TextureRegion.UVCenter);

                // cfert outline
                vh.AddVert(cFertilizerPoints[vertOffset + 1], OutlineColor, m_TextureRegion.UVCenter);
                vh.AddVert(cFertilizerPoints[vertOffset], OutlineColor, m_TextureRegion.UVCenter);

                // manure region
                vh.AddVert(cFertilizerPoints[vertOffset], ManureColor, m_TextureRegion.UVCenter);
            }
        }

        static private void Scale(ref float y, float baseline, float scale) {
            y = baseline + y * scale;
        }

        static private void OffsetForOutline(ref float y, float baseline, float width) {
            y = Math.Max(Math.Max(0, y - width), baseline);
        }

        public struct GraphPoint {
            public float Value;
        }
    }
}