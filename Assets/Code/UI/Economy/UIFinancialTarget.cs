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
        public Slider Slider;
        public Image TargetLine;
        public Image DirArrow;

        public void SetRatio(float value) {
            Slider.value = value;
        }

        public void SetTargetLine(float ratio) {
            float maxHeight = m_Root.rect.height;
            TargetLine.rectTransform.anchoredPosition = new Vector2(
                TargetLine.rectTransform.anchoredPosition.x,
                maxHeight * ratio
                );
        }

        public void SetArrowDir(int yScale) {
            DirArrow.rectTransform.SetScale(yScale, Axis.Y);
        }
    }
}