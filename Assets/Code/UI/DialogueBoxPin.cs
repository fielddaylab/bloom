using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI {
    public class DialogueBoxPin : MonoBehaviour {
        public RectTransformPinned Pin;
        public GameObject PinnedVisuals;

        [NonSerialized] private Vector2 m_DefaultPos;
        [NonSerialized] private RectTransform m_CachedTransform;

        private void Awake() {
            this.CacheComponent(ref m_CachedTransform);
            m_DefaultPos = m_CachedTransform.anchoredPosition;
            PinnedVisuals.SetActive(false);
        }

        public void PinTo(Transform t) {
            if (Pin.Pin(t)) {
                PinnedVisuals.SetActive(true);
            }
        }

        public void Unpin() {
            if (Pin.Unpin()) {
                m_CachedTransform.anchoredPosition = m_DefaultPos;
                PinnedVisuals.SetActive(false);
            }
        }
    }
}