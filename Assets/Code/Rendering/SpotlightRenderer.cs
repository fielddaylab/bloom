using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.Rendering {
    public class SpotlightRenderer : MonoBehaviour {
        public RectTransform Self;
        public RectTransform Left;
        public RectTransform Right;
        public RectTransform Top;
        public RectTransform Bottom;

        [NonSerialized] private ulong m_StateHash;

        public void RecalculateBorders() {
            if (!StateHash.HasChanged(Self.GetStateHash(), ref m_StateHash)) {
                return;
            }

            Rect parentSize = ((RectTransform) Self.parent).rect;
            Rect selfSize = Self.rect;
            selfSize.center += Self.anchoredPosition;
            
            Rect clampSize = parentSize;
            clampSize.xMin -= selfSize.xMin;
            clampSize.xMax -= selfSize.xMax;
            clampSize.yMin -= selfSize.yMin;
            clampSize.yMax -= selfSize.yMax;

            Left.sizeDelta = new Vector2(Mathf.Max(0, -clampSize.xMin), selfSize.height);
            Right.sizeDelta = new Vector2(Mathf.Max(0, clampSize.xMax), selfSize.height);

            Bottom.sizeDelta = new Vector2(parentSize.width, Mathf.Max(0, -clampSize.yMin));
            Top.sizeDelta = new Vector2(parentSize.width, Mathf.Max(0, clampSize.yMax));
            Bottom.anchoredPosition = Top.anchoredPosition = new Vector2(clampSize.center.x, 0);
        }
    }
}