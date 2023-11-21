using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class ShopToggleButton : MonoBehaviour, IScenePreload, IBaked {
        #region Inspector

        [Header("Interactions")]
        [SerializeField] private Button m_Button;

        [Header("Background Graphic")]
        [SerializeField] private RectTransform m_Slider;
        [SerializeField] private Graphic m_SliderGraphic;

        [Header("Icons")]
        [SerializeField] private Graphic m_PlayModeIcon;
        [SerializeField] private Color32 m_PlayModeSliderColor;
        [SerializeField] private Graphic m_BuildModeIcon;
        [SerializeField] private Color32 m_BuildModeSliderColor;

        #endregion // Inspector

        [NonSerialized] private bool m_InBlueprintMode;
        [NonSerialized] private bool m_Locked;
        private Routine m_SliderRoutine;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            m_Button.onClick.AddListener(HandleClick);
            return null;
        }

        #region Accessors 

        public void SetLocked(bool locked) {
            m_Locked = locked;
        }

        #endregion // Accessors

        #region Handlers

        private void HandleClick() {
            if (m_Locked) {
                // TODO: Play locked animation
                return;
            }

            m_InBlueprintMode = !m_InBlueprintMode;
            m_SliderRoutine.Replace(this, AppearanceTransition());

            if (m_InBlueprintMode) { Game.Events.Dispatch(GameEvents.BlueprintModeStarted); }
            else { Game.Events.Dispatch(GameEvents.BlueprintModeEnded); }
        }

        private IEnumerator AppearanceTransition() {
            if (m_InBlueprintMode) {
                yield return Routine.Combine(
                    m_PlayModeIcon.ColorTo(ZavalaColors.InterfaceBackgroundMid, 0.1f),
                    m_BuildModeIcon.ColorTo(ZavalaColors.InterfaceBackgroundLight, 0.1f),
                    m_Slider.AnchorPosTo(m_BuildModeIcon.rectTransform.anchoredPosition, 0.15f).Ease(Curve.CubeOut),
                    m_SliderGraphic.ColorTo(m_BuildModeSliderColor, 0.1f)
                    );
            } else {
                yield return Routine.Combine(
                    m_PlayModeIcon.ColorTo(ZavalaColors.InterfaceBackgroundLight, 0.1f),
                    m_BuildModeIcon.ColorTo(ZavalaColors.InterfaceBackgroundMid, 0.1f),
                    m_Slider.AnchorPosTo(m_PlayModeIcon.rectTransform.anchoredPosition, 0.15f).Ease(Curve.CubeOut),
                    m_SliderGraphic.ColorTo(m_PlayModeSliderColor, 0.1f)
                    );
            }
        }

        #endregion // Handlers

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order => 0;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            m_Slider.anchoredPosition = m_PlayModeIcon.rectTransform.anchoredPosition;
            m_SliderGraphic.color = m_PlayModeSliderColor;
            m_PlayModeIcon.color = ZavalaColors.InterfaceBackgroundLightest;
            m_BuildModeIcon.color = ZavalaColors.InterfaceBackgroundMid;
            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}