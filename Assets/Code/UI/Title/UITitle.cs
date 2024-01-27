using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Rendering;
using FieldDay.Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Data;

namespace Zavala.UI {
    public class UITitle : MonoBehaviour, IScenePreload
    {
        private enum Panel {
            Start,
            NewGame,
            LoadGame,
            Credits
        }

        #region Inspector

        [SerializeField] private RectTransform m_PanelBackground;

        [Header("Main Panel")]
        [SerializeField] private CanvasGroup m_StartGroup;
        [SerializeField] private Button m_StartButton; // New Game
        [SerializeField] private Button m_LoadButton;
        [SerializeField] private Button m_CreditsButton;

        [Header("Game Panel")]
        [SerializeField] private CanvasGroup m_GameGroup;
        [SerializeField] private TMP_InputField m_PlayerCodeInput;
        [SerializeField] private Slider m_VolumeSlider;
        [SerializeField] private Toggle m_FullscreenToggle;
        [SerializeField] private Toggle m_HighQualityToggle;
        [SerializeField] private Button m_PlayGameButton;
        [SerializeField] private Button m_BackButton;

        [Header("Credits")]
        [SerializeField] private UICredits m_CreditsPanel;

        [SerializeField] private SceneReference m_MainScene;

        #endregion // Inspector

        [NonSerialized] private Panel m_CurrentPanel;
        [NonSerialized] private Routine m_StartGroupRoutine;
        [NonSerialized] private Routine m_GameGroupRoutine;
        [NonSerialized] private Routine m_PanelBGRoutine;

        #region Unity Callbacks

        private void OnEnable()
        {
            m_StartButton.onClick.AddListener(HandleStartClicked);
            m_LoadButton.onClick.AddListener(HandleLoadClicked);

            m_CreditsButton.onClick.AddListener(HandleCreditsClicked);
            m_FullscreenToggle.onValueChanged.AddListener(HandleFullscreenToggle);
            m_HighQualityToggle.onValueChanged.AddListener(HandleQualityToggle);
            m_PlayGameButton.onClick.AddListener(HandlePlayButton);
            m_BackButton.onClick.AddListener(HandleBackButton);
        }

        private void LateUpdate() {
            bool currentFullscreen = ScreenUtility.GetFullscreen();
            if (currentFullscreen != m_FullscreenToggle.isOn) {
                m_FullscreenToggle.SetIsOnWithoutNotify(currentFullscreen);
            }
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleStartClicked()
        {
            m_CurrentPanel = Panel.NewGame;
            //Game.Scenes.LoadMainScene(m_MainScene);

            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, HidePanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, ShowPanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_GameGroup, 0.2f));
        }

        private void HandleLoadClicked()
        {
            m_CurrentPanel = Panel.LoadGame;
            //Game.Scenes.LoadMainScene(m_MainScene);

            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, HidePanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, ShowPanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_GameGroup, 0.2f));
        }

        private void HandleCreditsClicked()
        {
            m_CreditsPanel.OpenPanel();
        }

        private void HandleFullscreenToggle(bool toggle) {
            ScreenUtility.SetFullscreen(toggle);
        }

        private void HandleQualityToggle(bool toggle) {
            Game.SharedState.Get<UserSettings>().HighQualityMode = toggle;
        }

        private void HandlePlayButton() {

        }

        private void HandleBackButton() {
            m_CurrentPanel = Panel.Start;
            
            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, ShowPanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, HidePanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_StartGroup, 0.2f));
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            m_GameGroup.gameObject.SetActive(false);
            m_GameGroup.alpha = 0;
            yield return null;
            m_StartGroup.gameObject.SetActive(true);
            m_StartGroup.alpha = 1;
            yield return null;
            AlignPanel(m_StartGroup);
        }

        private void AlignPanel(CanvasGroup group) {
            RectTransform t = (RectTransform) group.transform;
            m_PanelBackground.anchoredPosition = t.anchoredPosition;
            m_PanelBackground.sizeDelta = t.sizeDelta;
        }

        private IEnumerator AlignPanelRoutine(CanvasGroup group, float duration) {
            RectTransform t = (RectTransform) group.transform;
            return Routine.Combine(
                m_PanelBackground.AnchorPosTo(t.anchoredPosition, duration).Ease(Curve.CubeOut),
                m_PanelBackground.SizeDeltaTo(t.sizeDelta, duration).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator HidePanelRoutine(CanvasGroup group) {
            group.blocksRaycasts = false;
            yield return group.FadeTo(0, 0.2f);
            group.gameObject.SetActive(false);
        }

        private IEnumerator ShowPanelRoutine(CanvasGroup group) {
            group.blocksRaycasts = false;
            group.gameObject.SetActive(true);
            yield return group.FadeTo(1, 0.2f);
            group.blocksRaycasts = true;
        }

        #endregion // Button Handlers
    }
}
