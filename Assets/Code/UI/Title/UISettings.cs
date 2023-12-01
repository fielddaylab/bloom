using BeauRoutine;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI
{
    public class UISettings : MonoBehaviour
    {
        private static float TRANSITION_Y = 700;
        private static float TRANSITION_TIME = 0.5f;

        #region Inspector

        [SerializeField] private Button m_ReturnButton;
        [SerializeField] private RectTransform m_Rect;

        #endregion // Inspector

        private Routine m_TransitionRoutine;

        #region Unity Callbacks

        private void OnEnable()
        {
            m_ReturnButton.onClick.AddListener(HandleReturnClicked);
        }

        #endregion // Unity Callbacks

        #region External

        public void OpenPanel()
        {
            m_TransitionRoutine.Replace(this, EnterRoutine());
        }

        #endregion // External

        #region Button Handlers

        private void HandleReturnClicked()
        {
            m_TransitionRoutine.Replace(this, ExitRoutine());
        }

        #endregion // Button Handlers


        #region Routines

        private IEnumerator EnterRoutine()
        {
            yield return m_Rect.AnchorPosTo(0, TRANSITION_TIME, Axis.Y).Ease(Curve.CubeOut);
        }

        private IEnumerator ExitRoutine()
        {
            yield return m_Rect.AnchorPosTo(TRANSITION_Y, TRANSITION_TIME, Axis.Y).Ease(Curve.CubeOut);
        }

        #endregion // Routines
    }
}