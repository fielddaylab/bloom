using BeauRoutine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI
{
    public class UIFinancialTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Root;
        public RectTransform Fill;
        public RectTransform TargetFill;
        public RectTransform TargetLine;

        public void SetRatio(float value) {
            Vector2 max = Fill.anchorMax;
            max.y = value;
            Fill.anchorMax = max;
        }

        public void SetTargetLine(float ratio) {
            Vector2 max = TargetFill.anchorMax;
            max.y = ratio;
            TargetFill.anchorMax = max;

            Vector2 min = TargetLine.anchorMin;
            max = TargetLine.anchorMax;

            min.y = max.y = ratio;

            TargetLine.anchorMin = min;
            TargetLine.anchorMax = max;
        }

        public void SetArrowDir(int yScale) {
            
        }
    }
}