using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class MarketShareGraphRenderer : MaskableGraphic {
        private GraphPoint[] m_ManurePoints;
        private GraphPoint[] m_CFertilizerPoints;
        private GraphPoint[] m_DFertilizerPoints;
        private readonly float[] m_AvgPositions = new float[3];

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