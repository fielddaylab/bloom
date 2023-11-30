using BeauRoutine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;

namespace Zavala.UI {
    public class UIPolicyBox : MonoBehaviour
    {
        private const float DISPLAY_TIME = 1.4f;

        #region Inspector

        [SerializeField] private TMP_Text LevelText;
        public UIPolicyBoxPopup Popup;

        #endregion // Inspector

        public PolicyType PolicyType;

        private Routine m_PopupRoutine;

        public void PlayPopupRoutine()
        {
            m_PopupRoutine.Replace(DisplayPopupRoutine());
        }

        #region Routines

        private IEnumerator DisplayPopupRoutine()
        {
            Popup.Group.alpha = 0;

            yield return Routine.Combine(
                Popup.Group.FadeTo(1, .1f)
                );

            yield return DISPLAY_TIME;

            yield return Routine.Combine(
                Popup.Group.FadeTo(0, .1f)
                );
        }

        #endregion // Routines
    }
}