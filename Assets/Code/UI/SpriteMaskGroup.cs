using System;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zavala.UI {
    public class SpriteMaskGroup : MonoBehaviour, IBaked {
        public SpriteMask MaskComponent;
        public SpriteRenderer[] Graphics;

        public void SetState(bool masking) {
            if (MaskComponent) {
                MaskComponent.enabled = masking;
            }

            foreach(var graphic in Graphics) {
                graphic.maskInteraction = masking ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
            }
        }

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order => 100;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            MaskComponent = GetComponentInChildren<SpriteMask>();

            if (MaskComponent) {
                SetState(MaskComponent.enabled);
            }

            return true;
        }

        private void Reset() {
            MaskComponent = GetComponentInChildren<SpriteMask>();
            Graphics = GetComponentsInChildren<SpriteRenderer>(true);
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}