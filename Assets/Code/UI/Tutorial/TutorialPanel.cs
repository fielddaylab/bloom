using System;
using System.Collections;
using BeauRoutine;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.UI;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;

namespace Zavala.UI.Tutorial {
    public class TutorialPanel : SharedRoutinePanel {
        #region Inspector

        [Header("Tutorial")]
        [SerializeField] private TutorialPanelConfigurer m_Configurer;
        [SerializeField] private Animator m_Animator;

        #endregion // Inspector

        [NonSerialized] private string m_QueuedAnimatorState;

        public void Open(string animatorState) {
            m_QueuedAnimatorState = animatorState;
            if (IsShowing()) {
                m_Animator.Play(animatorState);
            } else {
                Show();
            }
        }

        public void Close() {
            Hide();
        }

        public void Close(string stateName) {
            if (m_QueuedAnimatorState == stateName) {
                Hide();
            }
        }

        protected override IEnumerator TransitionToShow() {
            Root.gameObject.SetActive(true);
            CanvasGroup.alpha = 0;
            yield return null;
            if (!string.IsNullOrEmpty(m_QueuedAnimatorState)) {
                m_Animator.Play(m_QueuedAnimatorState);
            }
            yield return CanvasGroup.FadeTo(1, 0.2f);
        }

        protected override IEnumerator TransitionToHide() {
            yield return CanvasGroup.FadeTo(0, 0.2f);
            Root.gameObject.SetActive(false);
        }

        protected override void InstantTransitionToHide() {
            Root.gameObject.SetActive(false);
            CanvasGroup.alpha = 0;
        }

        protected override void InstantTransitionToShow() {
            Root.gameObject.SetActive(true);
            CanvasGroup.alpha = 1;

            if (!string.IsNullOrEmpty(m_QueuedAnimatorState)) {
                m_Animator.Play(m_QueuedAnimatorState);
            }
        }

        static public TutorialContexts GetCurrentContexts() {
            TutorialContexts contexts = default;
            BlueprintState bp = Game.SharedState.Get<BlueprintState>();
            if (bp.IsActive) {
                contexts |= TutorialContexts.Blueprints;
                BuildToolState bt = Game.SharedState.Get<BuildToolState>();
                switch (bt.ActiveTool) {
                    case UserBuildTool.Road: {
                        contexts |= TutorialContexts.BuildRoad;
                        break;
                    }
                    case UserBuildTool.Storage: {
                        contexts |= TutorialContexts.BuildStorage;
                        break;
                    }
                    case UserBuildTool.Digester: {
                        contexts |= TutorialContexts.BuildDigester;
                        break;
                    }
                    case UserBuildTool.Destroy: {
                        contexts |= TutorialContexts.DestroyMode;
                        break;
                    }
                }
            }
            ScriptRuntimeState sc = ScriptUtility.Runtime;
            if (sc.Cutscene.IsRunning()) {
                contexts |= TutorialContexts.Dialogue;
            }
            return contexts;
        }
    }

    [Flags]
    public enum TutorialContexts {
        Blueprints = 0x01,
        BuildRoad = 0x02,
        BuildStorage = 0x04,
        BuildDigester = 0x08,
        DestroyMode = 0x10,
        Dialogue = 0x20
    }
}