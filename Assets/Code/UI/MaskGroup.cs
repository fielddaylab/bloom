using System;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zavala.UI {
    [RequireComponent(typeof(IClipper))]
    public class MaskGroup : MonoBehaviour, IBaked {
        public UIBehaviour MaskComponent;
        public MaskableGraphic[] Graphics;

        public void SetState(bool masking) {
            if (MaskComponent) {
                MaskComponent.enabled = masking;
            }

            foreach(var graphic in Graphics) {
                graphic.maskable = masking;
            }
        }

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order => 100;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Reset();

            if (MaskComponent) {
                SetState(MaskComponent.enabled);
            }

            return true;
        }

        private void Reset() {
            MaskComponent = GetComponent<Mask>();
            if (!MaskComponent) {
                MaskComponent = GetComponent<RectMask2D>();
            }
            Graphics = GetComponentsInChildren<MaskableGraphic>(true);
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}