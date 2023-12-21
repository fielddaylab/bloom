using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.World;

namespace Zavala.UI {
    public class LensUI : SharedPanel, IScenePreload {
        #region Inspector

        [Header("Buttons")]
        [SerializeField] private RectTransform m_ButtonRect;
        [SerializeField] private CanvasGroup m_ButtonGroup;
        [SerializeField] private LensButton m_EcoButton;
        [SerializeField] private LensButton m_EconButton;

        [Header("Market")]
        [SerializeField] private CanvasGroup m_MarketPanel;

        [Header("Foldout")]
        [SerializeField] private CanvasGroup m_FoldoutGroup;
        [SerializeField] private RectTransform m_FoldoutRect;
        [SerializeField] private LayoutOffset m_FoldoutOffset;
        [SerializeField] private RectTransform m_FoldoutLayout;
        [SerializeField] private MaskGroup m_FoldoutMask;
        [SerializeField] private Graphic m_FoldoutBG;
        [SerializeField] private TMP_Text m_FoldoutLabel;

        #endregion // Inspector

        [NonSerialized] private AdvisorType m_Mode = AdvisorType.None;
        private Routine m_FoldoutRoutine;
        private Routine m_MarketRoutine;
        private Routine m_Transition;
        [NonSerialized] private bool m_FoldoutState;
        [NonSerialized] private bool m_TransitionState;

        [NonSerialized] private bool m_HasEco = false;
        [NonSerialized] private bool m_HasEcon = false;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Events.Register(GameEvents.BlueprintModeStarted, OnBlueprintStarted)
                .Register(GameEvents.BlueprintModeEnded, OnBlueprintEnded);

            m_EcoButton.Button.onClick.AddListener(OnEcoClicked);
            m_EconButton.Button.onClick.AddListener(OnEconClicked);

            m_EconButton.gameObject.SetActive(false);
            m_EcoButton.gameObject.SetActive(false);

            m_FoldoutGroup.gameObject.SetActive(false);
            m_FoldoutMask.SetState(false);

            m_MarketPanel.gameObject.SetActive(false);
            m_MarketPanel.alpha = 0;

            m_TransitionState = true;
            return null;
        }

        private void OnDisable() {
            Game.Events?.DeregisterAllForContext(this);
        }

        #region Handlers

        private void OnBlueprintStarted() {
            HidePhosphorus(true);
            HideMarket(true);

            Hide();
        }

        private void OnBlueprintEnded() {
            Show();
        }

        private void OnEcoClicked() {
            HideMarket(false);

            SimWorldState worldState = Game.SharedState.Get<SimWorldState>();
            if (m_Mode != AdvisorType.Ecology) {
                worldState.Overlays |= SimWorldOverlayMask.Phosphorus;
                SetButtonActive(m_EcoButton);
                m_Mode = AdvisorType.Ecology;
                ShowFoldout(m_EcoButton);
            } else {
                HidePhosphorus(true);
            }
        }

        private void OnEconClicked() {
            HidePhosphorus(false);

            if (m_Mode != AdvisorType.Economy) {
                SetButtonActive(m_EconButton);
                m_Mode = AdvisorType.Economy;
                ShowFoldout(m_EconButton);

                m_MarketRoutine.Replace(this, ShowMarket());
            } else {
                HideMarket(true);
            }
        }

        #endregion // Handlers

        #region Buttons

        private void SetButtonActive(LensButton button) {
            if (button.CurrentState) {
                return;
            }

            button.CurrentState = true;
            button.StateRoutine.Replace(this, CrossFadeGraphics(button.Icon, button.CloseIcon, 0.1f));
        }

        private void SetButtonInactive(LensButton button) {
            if (!button.CurrentState) {
                return;
            }

            button.CurrentState = false;
            button.StateRoutine.Replace(this, CrossFadeGraphics(button.CloseIcon, button.Icon, 0.1f));
        }

        static private Tween CrossFadeGraphics(Graphic a, Graphic b, float duration) {
            return Tween.ZeroToOne((f) => {
                a.SetAlpha(1 - f);
                b.SetAlpha(f);
                a.enabled = f < 1;
                b.enabled = f > 0;
            }, duration);
        }

        #endregion // Buttons

        #region Hide

        private void HideMarket(bool hideFoldout) {
            if (m_Mode == AdvisorType.Economy) {
                SetButtonInactive(m_EconButton);
                m_Mode = AdvisorType.None;

                if (hideFoldout) {
                    HideFoldout();
                }

                m_MarketRoutine.Replace(this, HideMarket());
            }
        }

        private void HidePhosphorus(bool hideFoldout) {
            if (m_Mode == AdvisorType.Ecology) {
                SetButtonInactive(m_EcoButton);
                Game.SharedState.Get<SimWorldState>().Overlays &= ~SimWorldOverlayMask.Phosphorus;
                m_Mode = AdvisorType.None;

                if (hideFoldout) {
                    HideFoldout();
                }
            }
        }

        #endregion // Hide

        #region Market

        private IEnumerator ShowMarket() {
            m_MarketPanel.gameObject.SetActive(true);
            yield return m_MarketPanel.FadeTo(1, 0.2f);
        }

        private IEnumerator HideMarket() {
            yield return m_MarketPanel.FadeTo(0, 0.2f);
            m_MarketPanel.gameObject.SetActive(false);
        }

        #endregion // Market

        #region Foldout

        private void PopulateFoldout(LensButton target) {
            m_FoldoutGroup.gameObject.SetActive(true);
            m_FoldoutBG.color = target.LabelBGColor;
            m_FoldoutLabel.color = target.LabelTextColor;
            m_FoldoutLabel.SetText(Loc.Find(target.LabelText));
            m_FoldoutRect.SetAnchorPos(target.Rect.anchoredPosition.y, Axis.Y);
            m_FoldoutGroup.gameObject.SetActive(true);
            m_FoldoutGroup.alpha = 1;
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_FoldoutLayout);
            m_FoldoutOffset.Offset0 = new Vector2(m_FoldoutLayout.sizeDelta.x, 0);
            m_FoldoutMask.SetState(true);
        }

        private IEnumerator ActivateFoldout() {
            m_FoldoutMask.SetState(true);
            yield return Tween.Vector(m_FoldoutOffset.Offset0, default, (v) => m_FoldoutOffset.Offset0 = v, 0.2f).Ease(Curve.CubeOut);
            m_FoldoutMask.SetState(false);
        }

        private IEnumerator DeactivateFoldout() {
            m_FoldoutMask.SetState(true);
            yield return Tween.Vector(m_FoldoutOffset.Offset0, new Vector2(m_FoldoutLayout.sizeDelta.x, 0), (v) => m_FoldoutOffset.Offset0 = v, 0.2f).Ease(Curve.CubeIn);
            m_FoldoutMask.SetState(false);
            m_FoldoutGroup.gameObject.SetActive(false);
        }

        private void ShowFoldout(LensButton button) {
            PopulateFoldout(button);
            m_FoldoutState = true;
            m_FoldoutRoutine.Replace(this, ActivateFoldout());
        }

        private void HideFoldout() {
            if (m_FoldoutState) {
                m_FoldoutState = false;
                m_FoldoutRoutine.Replace(this, DeactivateFoldout());
            }
        }

        #endregion // Foldout

        public void Unlock(AdvisorType type) {
            switch (type) {
                case AdvisorType.Ecology: {
                    if (!m_HasEco) {
                        m_HasEco = true;
                        m_EcoButton.gameObject.SetActive(true);
                    }
                    break;
                }

                case AdvisorType.Economy: {
                    if (!m_HasEcon) {
                        m_HasEcon = true;
                        m_EconButton.gameObject.SetActive(true);
                    }
                    break;
                }
            }
        }

        public bool isUnlocked(AdvisorType type) {
            switch (type) {
                case AdvisorType.Ecology: {
                    return m_HasEco;
                }
                case AdvisorType.Economy: {
                    return m_HasEcon;
                }
                default:
                    return false;
            }
        }

        #region IGuiPanel

        public override bool IsTransitioning() {
            return m_Transition;
        }

        public override bool IsShowing() {
            return m_TransitionState;
        }

        public override bool IsVisible() {
            return m_TransitionState;
        }

        public override void Show() {
            if (!m_TransitionState) {
                m_TransitionState = true;
                m_Transition.Replace(this, ShowAnim());
            }
        }

        public override void Hide() {
            if (m_TransitionState) {
                m_TransitionState = false;
                m_Transition.Replace(this, HideAnim());
            }
        }

        private IEnumerator ShowAnim() {
            m_ButtonRect.gameObject.SetActive(true);
            m_ButtonGroup.blocksRaycasts = false;
            yield return m_ButtonRect.AnchorPosTo(0, 0.2f, Axis.X).Ease(Curve.CubeOut);
            m_ButtonGroup.blocksRaycasts = true;
        }

        private IEnumerator HideAnim() {
            m_ButtonGroup.blocksRaycasts = false;
            yield return m_ButtonRect.AnchorPosTo(80, 0.2f, Axis.X).Ease(Curve.QuadIn);
            m_ButtonRect.gameObject.SetActive(false);
        }

        #endregion // IGuiPanel
    }
}