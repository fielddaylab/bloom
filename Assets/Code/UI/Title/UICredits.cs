using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeauRoutine.Extensions;
using Zavala.Cards;
using TMPro;

namespace Zavala.UI
{
    public class UICredits : MonoBehaviour
    {
        private static float TRANSITION_X = 1060;
        private static float TRANSITION_TIME = 0.5f;

        #region Inspector

        [SerializeField] private Button m_ReturnButton;
        [SerializeField] private RectTransform m_Rect;

        [Space(5)]
        [Header("Text")]
        [SerializeField] private TextAsset m_CreditsTextAsset;
        [SerializeField] private TMP_Text m_CreditsText;

        #endregion // Inspector

        private Routine m_TransitionRoutine;

        #region Unity Callbacks

        private void OnEnable()
        {
            m_ReturnButton.onClick.AddListener(HandleReturnClicked);

            ParseCredits();
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleReturnClicked()
        {
            m_TransitionRoutine.Replace(this, ExitRoutine());
        }

        #endregion // Button Handlers

        #region External

        public void OpenPanel()
        {
            m_TransitionRoutine.Replace(this, EnterRoutine());
        }

        #endregion // External

        #region Routines

        private IEnumerator EnterRoutine()
        {
            yield return m_Rect.AnchorPosTo(0, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);
        }

        private IEnumerator ExitRoutine()
        {
            yield return m_Rect.AnchorPosTo(TRANSITION_X, TRANSITION_TIME, Axis.X).Ease(Curve.CubeOut);

            ResetLayout();
        }

        #endregion // Routines

        private void ResetLayout()
        {
            
        }

        #region Credits Parsing

        private void ParseCredits()
        {
            List<string> creditsBlocks = TextIO.TextAssetToList(m_CreditsTextAsset, "::");



            m_CreditsText.SetText(creditsBlocks[0]);
        }

        #endregion // Credits Parsing
    }
}
