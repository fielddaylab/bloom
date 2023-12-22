using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;

namespace Zavala.UI {
    public class MarketShareGraphRenderer : ShapeGraphic {
        public Color ManureColor;
        public Color CFertilizerColor;
        public Color MFertilizerColor;
        public Color OutlineColor;
        public float OutlineThickness;
        [Range(3, 100)] public int MaxTicks = 20;

        public Action OnDataUpdated;

        [NonSerialized] private float[] m_MFertilizerPoints;
        [NonSerialized] private float[] m_ManurePoints;
        [NonSerialized] private float[] m_DFertilizerPoints;
        [NonSerialized] private int m_TickCount;

        // output data
        private readonly float[] m_AvgPositions = new float[3];
        [NonSerialized] private byte m_ValuesPresent;

        protected override void Awake() {
            base.Awake();

            m_ManurePoints = new float[MaxTicks];
            m_MFertilizerPoints = new float[MaxTicks];
            m_DFertilizerPoints = new float[MaxTicks];
        }

        #region Input

        public unsafe void Populate(float* mFertilizerPoints, float* manurePoints, float* dFertilizerPoints, int tickCount) {
            // TEMPORARY FIX: clamp the tick count to max ticks. These CopyArrays were going out of bounds. I'm not sure why yet.
            tickCount = Math.Clamp(tickCount, 0, MaxTicks);
            m_TickCount = tickCount;
            Unsafe.CopyArray(mFertilizerPoints, tickCount, m_MFertilizerPoints, 0);
            Unsafe.CopyArray(manurePoints, tickCount, m_ManurePoints, 0);
            Unsafe.CopyArray(dFertilizerPoints, tickCount, m_DFertilizerPoints, 0);

            SetVerticesDirty();
        }

        public void Clear() {
            m_TickCount = 0;
            SetVerticesDirty();
        }

        #endregion // Input

        #region Output

        public bool TryGetLocalYCenter(ResourceId resource, out float localY) {
            switch (resource) {
                case ResourceId.Manure: {
                    localY = m_AvgPositions[1];
                    return (m_ValuesPresent & 2) != 0;
                }
                case ResourceId.MFertilizer: {
                    localY = m_AvgPositions[0];
                    return (m_ValuesPresent & 1) != 0;
                }
                case ResourceId.DFertilizer: {
                    localY = m_AvgPositions[2];
                    return (m_ValuesPresent & 4) != 0;
                }
                default: {
                    localY = -1;
                    return false;
                }
            }
            
        }

        #endregion // Output

        #region Rendering

        protected override unsafe void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            if (m_TickCount <= 1) {
                Array.Clear(m_AvgPositions, 0, m_AvgPositions.Length);
                m_ValuesPresent = 0;
                return;
            }

            Rect rect = GetPixelAdjustedRect();

            float columnWidth = rect.width / MaxTicks;

            Vector2* mFertilizerPoints = stackalloc Vector2[m_TickCount * 2];
            Vector2* manurePoints = stackalloc Vector2[m_TickCount * 2];
            Vector2* dFertilizerPoints = stackalloc Vector2[m_TickCount * 2];

            float maxValue = 0;

            float right = rect.xMax;
            for(int i = 0; i < m_TickCount; i++) {
                int vertexOffset = i * 2;

                float mFertilizerVal = m_MFertilizerPoints[i];
                mFertilizerPoints[vertexOffset] = new Vector2(right - i * columnWidth, mFertilizerVal);

                float manureVal = m_ManurePoints[i];
                manurePoints[vertexOffset] = new Vector2(right - i * columnWidth, manureVal + mFertilizerVal);

                float dFertilizerVal = m_DFertilizerPoints[i];
                dFertilizerPoints[vertexOffset] = new Vector2(right - i * columnWidth, dFertilizerVal + manureVal + mFertilizerVal);

                maxValue = Math.Max(maxValue, mFertilizerVal + manureVal + dFertilizerVal);
            }

            maxValue = Math.Max(4, (float) Math.Ceiling(maxValue / 4) * 4);

            float scaling = rect.height / maxValue;
            float baseY = rect.yMin;

            // generate actual y values and outline points
            for(int i = 0; i < m_TickCount; i++) {
                int vertexOffset = i * 2;

                ref float cFert = ref mFertilizerPoints[vertexOffset].y;
                ref float manure = ref manurePoints[vertexOffset].y;
                ref float dFert = ref dFertilizerPoints[vertexOffset].y;

                Scale(ref cFert, baseY, scaling);
                Scale(ref manure, baseY, scaling);
                Scale(ref dFert, baseY, scaling);

                mFertilizerPoints[vertexOffset + 1] = mFertilizerPoints[vertexOffset];
                manurePoints[vertexOffset + 1] = manurePoints[vertexOffset];
                dFertilizerPoints[vertexOffset + 1] = dFertilizerPoints[vertexOffset];

                OffsetForOutline(ref mFertilizerPoints[vertexOffset + 1].y, baseY, OutlineThickness);
                OffsetForOutline(ref manurePoints[vertexOffset + 1].y, cFert, OutlineThickness);
                OffsetForOutline(ref dFertilizerPoints[vertexOffset + 1].y, manure, OutlineThickness);
            }

            for (int i = 0; i < m_TickCount; i++) {
                int vertOffset = i * 2;
                Vector3 baseline = mFertilizerPoints[vertOffset];
                baseline.y = baseY;
                
                // mfert region
                vh.AddVert(baseline, CFertilizerColor, m_TextureRegion.UVCenter);
                vh.AddVert(mFertilizerPoints[vertOffset + 1], CFertilizerColor, m_TextureRegion.UVCenter);

                // mfert outline
                vh.AddVert(mFertilizerPoints[vertOffset + 1], OutlineColor, m_TextureRegion.UVCenter);
                vh.AddVert(mFertilizerPoints[vertOffset], OutlineColor, m_TextureRegion.UVCenter);

                // manure region
                vh.AddVert(mFertilizerPoints[vertOffset], ManureColor, m_TextureRegion.UVCenter);
                vh.AddVert(manurePoints[vertOffset + 1], ManureColor, m_TextureRegion.UVCenter);

                // manure outline
                vh.AddVert(manurePoints[vertOffset + 1], OutlineColor, m_TextureRegion.UVCenter);
                vh.AddVert(manurePoints[vertOffset], OutlineColor, m_TextureRegion.UVCenter);

                // dfert region
                vh.AddVert(manurePoints[vertOffset], MFertilizerColor, m_TextureRegion.UVCenter);
                vh.AddVert(dFertilizerPoints[vertOffset + 1], MFertilizerColor, m_TextureRegion.UVCenter);

                // dfert outline
                vh.AddVert(dFertilizerPoints[vertOffset + 1], OutlineColor, m_TextureRegion.UVCenter);
                vh.AddVert(dFertilizerPoints[vertOffset], OutlineColor, m_TextureRegion.UVCenter);
            }

            for(int i = 1; i < m_TickCount; i++) {
                int rightVertBase = 12 * (i - 1);
                int leftVertBase = 12 * i;

                for (int v = 0; v < 6; v++) {
                    vh.AddTriangle(leftVertBase + 0, leftVertBase + 1, rightVertBase + 1);
                    vh.AddTriangle(rightVertBase + 1, rightVertBase, leftVertBase + 0);
                    rightVertBase += 2;
                    leftVertBase += 2;
                }
            }

            // output data
            m_AvgPositions[0] = (baseY + mFertilizerPoints[1].y) / 2;
            m_AvgPositions[1] = (mFertilizerPoints[2].y + manurePoints[1].y) / 2;
            m_AvgPositions[2] = (manurePoints[2].y + dFertilizerPoints[1].y) / 2;

            m_ValuesPresent = 0;
            if (m_MFertilizerPoints[0] > 0) {
                m_ValuesPresent |= 1;
            }
            if (m_ManurePoints[0] > 0) {
                m_ValuesPresent |= 2;
            }
            if (m_DFertilizerPoints[0] > 0) {
                m_ValuesPresent |= 4;
            }

            OnDataUpdated?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void Scale(ref float y, float baseline, float scale) {
            y = baseline + y * scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void OffsetForOutline(ref float y, float baseline, float width) {
            y = Math.Max(y - width, baseline);
        }

        #endregion // Rendering
    }
}