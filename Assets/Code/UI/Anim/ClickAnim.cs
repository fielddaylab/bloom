using System;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zavala.UI {
    public class ClickAnim : MonoBehaviour, ILiteAnimator, IPointerClickHandler, IPointerUpHandler {
        [SerializeField, Required] private LayoutOffset m_LayoutOffset;
        [SerializeField] private Selectable m_Selectable;
        [NonSerialized] private bool m_WasSelectable;

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            Game.Animation.CancelLiteAnimator(this);
            m_LayoutOffset.Offset1 = default;
        }

        public void Ping() {
            Game.Animation.AddLiteAnimator(this, 0.15f);
        }

        public bool UpdateAnimation(object _, ref LiteAnimatorState state, float deltaTime) {
            state.TimeRemaining = Math.Max(0, state.TimeRemaining - deltaTime);
            float amt = state.TimeRemaining / state.Duration;
            m_LayoutOffset.Offset1 = new Vector2(0, amt * -2);
            return state.TimeRemaining > 0;
        }

        public void ResetAnimation(object _, ref LiteAnimatorState state) {
            m_LayoutOffset.Offset1 = default;
        }

        void IPointerUpHandler.OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) {
            m_WasSelectable = !m_Selectable || m_Selectable.IsInteractable();
        }

        void IPointerClickHandler.OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) {
            if (eventData.button != 0) {
                return;
            }

            if (m_WasSelectable || Game.Input.IsForcingClick()) {
                Ping();
            }
        }

#if UNITY_EDITOR

        private void Reset() {
            m_Selectable = GetComponent<Selectable>();
            m_LayoutOffset = GetComponent<LayoutOffset>();
        }

        private void OnValidate() {
            if (!m_Selectable) {
                m_Selectable = GetComponent<Selectable>();
            }
        }

#endif // UNITY_EDITOR
    }
}