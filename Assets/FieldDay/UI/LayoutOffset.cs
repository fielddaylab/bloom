using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace FieldDay.UI {
    /// <summary>
    /// Offset for a RectTransform's anchoredPosition.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class LayoutOffset : MonoBehaviour, ILayoutElement, ILayoutSelfController {
        #region Inspector

        [SerializeField] private Vector2 m_Offset0;
        [SerializeField] private Vector2 m_Offset1;
        [SerializeField] private Vector2 m_Offset2;

        #endregion // Inspector

        [NonSerialized] private RectTransform m_Rect;
        [NonSerialized] private Vector2 m_Applied;

        #region Properties

        public Vector2 Offset0 {
            get { return m_Offset0; }
            set {
                if (m_Offset0 != value) {
                    m_Offset0 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        public Vector2 Offset1 {
            get { return m_Offset1; }
            set {
                if (m_Offset1 != value) {
                    m_Offset1 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        public Vector2 Offset2 {
            get { return m_Offset2; }
            set {
                if (m_Offset2 != value) {
                    m_Offset2 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        #endregion // Properties

        #region Events

        private void OnEnable() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
            ApplyCurrentOffset();
        }

        private void OnDisable() {
            if (m_Rect) {
                ApplyOffset(default(Vector2));
            }
        }

        private void OnTransformParentChanged() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
        }

        [Preserve]
        private void OnDidApplyAnimationProperties() {
            ApplyCurrentOffset();
        }

#if UNITY_EDITOR

        private void OnValidate() {
            if (!Frame.IsActive(this)) {
                return;
            }

            ApplyCurrentOffset();
        }

#endif // UNITY_EDITOR

        #endregion // Events

        private void ApplyCurrentOffset() {
            ApplyOffset(m_Offset0 + m_Offset1 + m_Offset2);
        }

        private void ApplyOffset(Vector2 offset) {
            Vector2 delta = offset - m_Applied;
            m_Applied = offset;

            if (delta.x != 0 || delta.y != 0) {
                if (object.ReferenceEquals(m_Rect, null)) {
                    m_Rect = (RectTransform) transform;
                }
                m_Rect.anchoredPosition += delta;
            }
        }

        #region ILayout

        float ILayoutElement.minWidth { get { return -1; } }
        float ILayoutElement.preferredWidth { get { return -1; } }
        float ILayoutElement.flexibleWidth { get { return -1; } }

        float ILayoutElement.minHeight { get { return -1; } }
        float ILayoutElement.preferredHeight { get { return -1; } }
        float ILayoutElement.flexibleHeight { get { return -1; } }

        int ILayoutElement.layoutPriority { get { return -10000; } }

        void ILayoutElement.CalculateLayoutInputHorizontal() {
            ApplyOffset(default(Vector2));
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
            // Ignore
        }

        void ILayoutController.SetLayoutHorizontal() {
            // Ignore
        }

        void ILayoutController.SetLayoutVertical() {
            ApplyCurrentOffset();
        }

        #endregion // ILayout
    }
}