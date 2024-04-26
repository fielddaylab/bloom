using BeauRoutine;
using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class UIPolicyBoxPopup : MonoBehaviour
    {
        private const float DISPLAY_TIME = 1.4f;

        #region Inspector

        public TMP_Text AmountText;
        public Graphic AmountBG;
        public LayoutGroup Layout;

        public CanvasGroup Group;
        public RectTransform Rect;

        public Routine PopupRoutine;

        #endregion // Inspector

        #region Routines

        public IEnumerator DisplayPopupRoutine(UIPolicyBoxPopup popup)
        {
            popup.Group.alpha = 0;

            yield return Routine.Combine(
                popup.Group.FadeTo(1, .1f)
                );

            yield return DISPLAY_TIME;

            yield return Routine.Combine(
                popup.Group.FadeTo(0, .1f)
                );
        }

        #endregion // Routines

    }
}