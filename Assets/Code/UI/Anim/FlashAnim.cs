using System;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zavala.UI {
    public class FlashAnim : MonoBehaviour, ILiteAnimator {

        [SerializeField] private Graphic m_Graphic;
        [NonSerialized] private float m_GraphicAlpha;

        private void Awake() {
            m_GraphicAlpha = m_Graphic.GetAlpha();
            m_Graphic.enabled = false;
        }

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            Game.Animation.CancelLiteAnimator(this);
        }

        public void Ping() {
            Game.Animation.AddLiteAnimator(this, 0.2f);
        }

        public bool UpdateAnimation(object _, ref LiteAnimatorState state, float deltaTime) {
            state.TimeRemaining = Math.Max(0, state.TimeRemaining - deltaTime);
            float amt = state.TimeRemaining / state.Duration;
            m_Graphic.SetAlpha(TweenUtil.Evaluate(Curve.CubeOut, amt) * m_GraphicAlpha);
            m_Graphic.enabled = amt > 0;
            return state.TimeRemaining > 0;
        }

        public void ResetAnimation(object _, ref LiteAnimatorState state) {
            m_Graphic.SetAlpha(0);
            m_Graphic.enabled = false;
        }

#if UNITY_EDITOR

        private void Reset() {
            m_Graphic = GetComponent<Graphic>();
        }

        private void OnValidate() {
            if (!m_Graphic) {
                m_Graphic = GetComponent<Graphic>();
            }
        }

#endif // UNITY_EDITOR
    }
}