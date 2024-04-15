using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Rendering;
using FieldDay.Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Audio;
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
        [SerializeField] private CanvasGroup m_Raycaster;
        [SerializeField, StreamingVideoPath] private string m_BackgroundVideoPath;
        [SerializeField] private StreamingQuadTexture m_BackgroundRenderer;

        [Header("Music")]
        [SerializeField] private MusicState m_TitleMusic;
        [SerializeField, StreamingAudioPath] private string[] m_AllTitleSongs;
        [SerializeField, StreamingAudioPath] private string[] m_AllCreditsSongs;

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
        [SerializeField] private CanvasGroup m_NotFoundGroup;

        [Header("Credits")]
        [SerializeField] private UICredits m_CreditsPanel;

        [SerializeField] private SceneReference m_MainScene;

        #endregion // Inspector

        [NonSerialized] private Panel m_CurrentPanel;
        [NonSerialized] private Routine m_StartGroupRoutine;
        [NonSerialized] private Routine m_GameGroupRoutine;
        [NonSerialized] private Routine m_PanelBGRoutine;
        [NonSerialized] private Routine m_NotFoundRoutine;

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

            m_VolumeSlider.onValueChanged.AddListener(HandleSliderChanged);

            m_PlayerCodeInput.onValueChanged.AddListener(HandlePlayerCodeUpdated);

            m_NotFoundGroup.alpha = 0;

            if (GameplayMetadata.Instance && GameplayMetadata.Instance.GameWinToTitle) {
                LoadFromGameWin();
            }
        }

        private void LateUpdate() {
            bool currentFullscreen = ScreenUtility.GetFullscreen();
            if (currentFullscreen != m_FullscreenToggle.isOn) {
                m_FullscreenToggle.SetIsOnWithoutNotify(currentFullscreen);
            }
        }

        private void Start()
        {
            ZavalaGame.Events.Register(GameEvents.CreditsExited, HandleCreditsExited);
        }

        private void OnDestroy() {
            if (!Game.IsShuttingDown) {
                Game.Events.Deregister(GameEvents.CreditsExited, HandleCreditsExited);
            }
        }

        #endregion // Unity Callbacks

        #region Button Handlers

        private void HandleStartClicked()
        {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.NewGameClicked);
            m_CurrentPanel = Panel.NewGame;
            m_PlayerCodeInput.SetTextWithoutNotify(string.Empty);
            m_PlayerCodeInput.readOnly = true;
            m_PlayGameButton.interactable = false;

            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, HidePanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, ShowPanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_GameGroup, 0.2f));

            m_NotFoundGroup.alpha = 0;
            m_NotFoundRoutine.Stop();

            OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
        }

        private void HandleLoadClicked()
        {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.ResumeGameClicked);
            m_CurrentPanel = Panel.LoadGame;
            m_PlayerCodeInput.SetTextWithoutNotify(Game.SharedState.Get<UserSettings>().PlayerCode);
            m_PlayerCodeInput.readOnly = false;
            m_PlayGameButton.interactable = true;

            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, HidePanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, ShowPanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_GameGroup, 0.2f));

            m_NotFoundGroup.alpha = 0;
            m_NotFoundRoutine.Stop();
        }

        private void HandleCreditsClicked()
        {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.CreditsButtonClicked);
            m_CreditsPanel.OpenPanel();
            MusicUtility.SetAllSongs(m_TitleMusic, m_AllCreditsSongs);
        }

        private void HandleCreditsExited()
        {
            MusicUtility.SetAllSongs(m_TitleMusic, m_AllTitleSongs);
        }

        private void HandleFullscreenToggle(bool toggle) {
            ZavalaGame.Events.Dispatch(GameEvents.FullscreenToggled, toggle);
            ScreenUtility.SetFullscreen(toggle);
        }

        private void HandleQualityToggle(bool toggle) {
            ZavalaGame.Events.Dispatch(GameEvents.QualityToggled, toggle);
            Game.SharedState.Get<UserSettings>().HighQualityMode = toggle;
        }

        private void HandlePlayButton() {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.PlayGameClicked);
            m_Raycaster.blocksRaycasts = false;
            if (m_CurrentPanel == Panel.NewGame) {
                ZavalaGame.SaveBuffer.Clear();
                OGD.Player.ClaimId(m_PlayerCodeInput.text, null, HandlePlayAccepted, HandleClaimNewIdError);
            } else {
                Future f = SaveUtility.LoadFromServer(m_PlayerCodeInput.text);
                f.OnComplete(HandlePlayAccepted);
                f.OnFail(HandleLoadError);
            }
        }

        private void HandleSliderChanged(float val) {
            Game.SharedState.Get<UserSettings>().MusicVolume = val/10f;
        }

        private void HandleBackButton() {
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.ReturnedToMainMenu);
            m_CurrentPanel = Panel.Start;
            
            m_StartGroup.blocksRaycasts = false;
            m_StartGroupRoutine.Replace(this, ShowPanelRoutine(m_StartGroup));

            m_GameGroup.blocksRaycasts = false;
            m_GameGroupRoutine.Replace(this, HidePanelRoutine(m_GameGroup));

            m_PanelBGRoutine.Replace(this, AlignPanelRoutine(m_StartGroup, 0.2f));
        }

        private void HandleNewPlayerId(string id) {
            if (m_CurrentPanel == Panel.NewGame) {
                m_PlayerCodeInput.SetTextWithoutNotify(id);
                m_PlayGameButton.interactable = true;
                HandlePlayerCodeUpdated(id);
            }
        }

        private void HandleNewPlayerIdError(OGD.Core.Error err) {
            if (m_CurrentPanel == Panel.NewGame) {
                OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
            }
        }

        private void HandlePlayAccepted() {
            m_NotFoundRoutine.Stop();
            m_TitleMusic.Step = MusicPlaybackStep.FadeOut;
            Game.SharedState.Get<UserSettings>().PlayerCode = m_PlayerCodeInput.text;
            ZavalaGame.Events.Dispatch(GameEvents.ProfileStarting, m_PlayerCodeInput.text);
            Game.Scenes.LoadMainScene(m_MainScene);
            ZavalaGame.Events.Dispatch(GameEvents.MainMenuInteraction, MenuInteractionType.GameStarted);
        }

        private void HandleClaimNewIdError(OGD.Core.Error err) {
            m_Raycaster.blocksRaycasts = true;
            Debug.LogError(err.ToString());
        }

        private void HandleLoadError() {
            m_Raycaster.blocksRaycasts = true;
            Debug.LogError("load from server failed");
            m_NotFoundRoutine.Replace(CodeNotFoundRoutine());
        }

        private void HandlePlayerCodeUpdated(string text) {
            m_PlayGameButton.interactable = text.Length > 1;
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            m_BackgroundRenderer.Path = m_BackgroundVideoPath;

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

        private IEnumerator CodeNotFoundRoutine()
        {
            m_NotFoundGroup.blocksRaycasts = false;
            yield return m_NotFoundGroup.FadeTo(1, 0.2f);
            yield return 3f;
            yield return m_NotFoundGroup.FadeTo(0, 0.2f);
        }


        #endregion // Button Handlers

        private void LoadFromGameWin()
        {
            Debug.Log("[Meta] opening from game win");
            // load credits immediately
            m_CreditsPanel.OpenPanelImmediate();
            MusicUtility.SetAllSongs(m_TitleMusic, m_AllCreditsSongs);
            GameplayMetadata.Instance.GameWinToTitle = false;
        }
    }
}
