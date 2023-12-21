using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.Sim;
using Zavala.UI.Tutorial;

namespace Zavala {
    public class PauseFader : MonoBehaviour {
        private enum Mode {
            None,
            Cutscene,
            UserPause,
            Blueprints,
            Destroy
        }

        [SerializeField] private Graphic m_DefaultFader;
        [SerializeField] private CanvasGroup m_BorderFader;
        [SerializeField] private Graphic[] m_BorderGraphics;

        [Header("Config")]
        [SerializeField] private float m_FadeTime = 0.3f;
        [SerializeField] private Color m_UserPauseColor = Color.gray;
        [SerializeField] private Color m_BlueprintPauseColor = Color.blue;
        [SerializeField] private Color m_DestroyPauseColor = Color.red;

        [NonSerialized] private float m_DefaultFaderAlpha;
        [NonSerialized] private bool m_DefaultState;
        [NonSerialized] private bool m_BorderState;

        [NonSerialized] private Color m_BorderColor;
        [NonSerialized] private Routine m_DefaultFadeRoutine;
        [NonSerialized] private Routine m_BorderFadeRoutine;
        [NonSerialized] private Routine m_BorderColorRoutine;

        [NonSerialized] private UserBuildTool m_LastTool;
        [NonSerialized] private SimPauseFlags m_LastFlags;
        [NonSerialized] private Mode m_LastMode;

        private void Awake() {
            m_DefaultFader.enabled = false;
            m_BorderFader.gameObject.SetActive(false);

            m_DefaultFaderAlpha = m_DefaultFader.color.a;

            m_DefaultFader.SetAlpha(0);
            m_BorderFader.alpha = 0;

            SimTimeUtility.OnPauseUpdated.Register(OnPauseUpdated);
            Game.Events.Register(GameEvents.BuildToolSelected, OnBuildToolUpdated)
                .Register(GameEvents.BuildToolDeselected, OnBuildToolUpdated);
        }

        private void OnDestroy() {
            SimTimeUtility.OnPauseUpdated.Deregister(OnPauseUpdated);
            Game.Events?.DeregisterAllForContext(this);
        }

        private void OnPauseUpdated(SimPauseFlags flags) {
            m_LastFlags = flags;
            UpdateState();
        }

        private void OnBuildToolUpdated() {
            m_LastTool = Game.SharedState.Get<BuildToolState>().ActiveTool;
            UpdateState();
        }

        private void UpdateState() {
            Mode nextMode = GetMode(m_LastTool, m_LastFlags);
            if (nextMode == m_LastMode) {
                return;
            }

            m_LastMode = nextMode;

            if (nextMode == Mode.Cutscene) {
                m_DefaultState = true;
                m_DefaultFadeRoutine.Replace(this, FadeIn(m_DefaultFader, m_DefaultFaderAlpha, m_FadeTime));
            } else if (m_DefaultState) {
                m_DefaultState = false;
                m_DefaultFadeRoutine.Replace(this, FadeOut(m_DefaultFader, m_FadeTime));
            }

            if (nextMode == Mode.None || nextMode == Mode.Cutscene) {
                if (m_BorderState) {
                    m_BorderState = false;
                    m_BorderColorRoutine.Stop();
                    m_BorderFadeRoutine.Replace(this, FadeOut(m_BorderFader, m_FadeTime));
                    TutorialState.HidePanel();
                }
            } else {
                Color c;
                if (nextMode == Mode.Blueprints) {
                    c = m_BlueprintPauseColor;
                } else if (nextMode == Mode.Destroy) {
                    c = m_DestroyPauseColor;
                } else {
                    c = m_UserPauseColor;
                }

                if (!m_BorderState) {
                    m_BorderState = true;
                    m_BorderFadeRoutine.Replace(this, FadeIn(m_BorderFader, 1, m_FadeTime));
                }

                if (m_BorderFader.alpha <= 0) {
                    m_BorderColor = c;
                    SetAllColor(m_BorderGraphics, c);
                } else {
                    m_BorderColorRoutine.Replace(this, Tween.Color(m_BorderColor, c, OnBorderColorTweenUpdate, m_FadeTime));
                }
            }
        }

        static private Mode GetMode(UserBuildTool tool, SimPauseFlags flags) {
            if (tool == UserBuildTool.Destroy) {
                return Mode.Destroy;
            } else if ((flags & SimPauseFlags.Blueprints) != 0) {
                return Mode.Blueprints;
            } else if ((flags & SimPauseFlags.User) != 0) {
                return Mode.UserPause;
            } else if ((flags & (SimPauseFlags.Cutscene | SimPauseFlags.Scripted | SimPauseFlags.DialogBox | SimPauseFlags.PendingGlobalAlert)) != 0) {
                return Mode.Cutscene;
            } else {
                return Mode.None;
            }
        }

        private void OnBorderColorTweenUpdate(Color c) {
            m_BorderColor = c;
            SetAllColor(m_BorderGraphics, c);
        }

        static private void SetAllColor(Graphic[] graphics, Color c) {
            for(int i = 0; i < graphics.Length; i++) {
                graphics[i].SetColor(c);
            }
        }

        static private IEnumerator FadeIn(Graphic graphic, float alpha, float duration) {
            graphic.enabled = true;
            return graphic.FadeTo(alpha, duration);
        }

        static private IEnumerator FadeIn(CanvasGroup group, float alpha, float duration) {
            group.gameObject.SetActive(true);
            return group.FadeTo(alpha, duration);
        }

        static private IEnumerator FadeOut(Graphic graphic, float duration) {
            yield return graphic.FadeTo(0, duration);
            graphic.enabled = false;
        }

        static private IEnumerator FadeOut(CanvasGroup group, float duration) {
            yield return group.FadeTo(0, duration);
            group.gameObject.SetActive(false);
        }
    }
}