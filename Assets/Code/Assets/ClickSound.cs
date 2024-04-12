using UnityEngine.EventSystems;
using UnityEngine;
using System;
using UnityEngine.UI;
using BeauUtil;
using UnityEngine.UIElements;
using FieldDay;
using FieldDay.Assets;

namespace Zavala.Audio {
    [RequireComponent(typeof(Selectable))]
    public class ClickSound : UIBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
        [SerializeField] private Selectable m_Selectable;

        [Header("Hover State")]
        [SfxRef] public SerializedHash32 EnterId;
        [SfxRef] public SerializedHash32 ExitId;

        [Header("Click")]
        [SfxRef] public SerializedHash32 DownId;
        [SfxRef] public SerializedHash32 UpId;
        [SfxRef] public SerializedHash32 ClickId = "ui-click";

        [NonSerialized] private bool m_WasInteractable;

        private void TryPlay(StringHash32 id, bool checkPrevState) {
            if (id.IsEmpty) {
                return;
            }

            if (!Game.Input.IsForcingClick()) {
                if (checkPrevState) {
                    if (!m_WasInteractable) {
                        return;
                    }
                } else {
                    if (!m_Selectable.IsInteractable()) {
                        return;
                    }
                }
            }

            SfxUtility.PlaySfx(id);
        }

        #region Handlers

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            TryPlay(ClickId, true);
            m_WasInteractable = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            TryPlay(DownId, false);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            TryPlay(EnterId, false);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            TryPlay(ExitId, false);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            m_WasInteractable = m_Selectable.IsInteractable();
            TryPlay(UpId, false);
        }

        #endregion // Handlers

        protected override void Awake() {
            this.CacheComponent(ref m_Selectable);
        }

#if UNITY_EDITOR

        protected override void Reset() {
            m_Selectable = GetComponent<Selectable>();
        }

        protected override void OnValidate() {
            m_Selectable = GetComponent<Selectable>();
        }

#endif // UNITY_EDITOR
    }
}