using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class MarketShareGraphRenderer : MaskableGraphic {
        private const int CategoryCount = 3;

        private GraphPoint[] m_Points;
        private readonly float[] m_MinPositions = new float[CategoryCount];

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            Rect rect = GetPixelAdjustedRect();
        }

        public struct GraphPoint {
            public int Tick;
            public float Value;
        }
    }
}