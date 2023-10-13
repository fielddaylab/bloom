using System;
using System.Collections;
using BeauRoutine;
using FieldDay;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Sim;

namespace Zavala {
    public class PauseFader : MonoBehaviour {
        [SerializeField] private Graphic m_DefaultFader;
        [SerializeField] private Graphic m_BlueprintsFader;

        [NonSerialized] private float m_DefaultFaderAlpha;
        [NonSerialized] private float m_BlueprintFaderAlpha;
        [NonSerialized] private bool m_DesiredDefault;
        [NonSerialized] private bool m_DesiredBlueprints;

        [NonSerialized] private Routine m_DefaultFadeRoutine;
        [NonSerialized] private Routine m_BlueprintsFadeRoutine;

        private void Awake() {
            m_DefaultFader.enabled = false;
            m_BlueprintsFader.enabled = false;

            m_DefaultFaderAlpha = m_DefaultFader.color.a;
            m_BlueprintFaderAlpha = m_BlueprintsFader.color.a;

            m_DefaultFader.SetAlpha(0);
            m_BlueprintsFader.SetAlpha(0);

            SimTimeUtility.OnPauseUpdated.Register(OnPauseUpdated);
        }

        private void OnDestroy() {
            SimTimeUtility.OnPauseUpdated.Deregister(OnPauseUpdated);
        }

        private void OnPauseUpdated(SimPauseFlags flags) {
            bool hadBlueprints = m_DesiredBlueprints;
            bool hadDefault = m_DesiredDefault;

            m_DesiredBlueprints = (flags & SimPauseFlags.Blueprints) != 0;
            m_DesiredDefault = !m_DesiredBlueprints & flags != 0;

            if (!hadBlueprints && m_DesiredBlueprints) {
                m_BlueprintsFadeRoutine.Replace(this, FadeIn(m_BlueprintsFader, m_BlueprintFaderAlpha, 0.3f));
            } else if (hadBlueprints && !m_DesiredBlueprints) {
                m_BlueprintsFadeRoutine.Replace(this, FadeOut(m_BlueprintsFader, 0.3f));
            }

            if (!hadDefault && m_DesiredDefault) {
                m_DefaultFadeRoutine.Replace(this, FadeIn(m_DefaultFader, m_DefaultFaderAlpha, 0.3f));
            } else if (hadDefault && !m_DesiredDefault) {
                m_DefaultFadeRoutine.Replace(this, FadeOut(m_DefaultFader, 0.3f));
            }
        }

        static private IEnumerator FadeIn(Graphic graphic, float alpha, float duration) {
            graphic.enabled = true;
            return graphic.FadeTo(alpha, duration);
        }

        static private IEnumerator FadeOut(Graphic graphic, float duration) {
            yield return graphic.FadeTo(0, duration);
            graphic.enabled = false;
        }
    }
}