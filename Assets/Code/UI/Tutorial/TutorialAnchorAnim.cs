using System;
using BeauRoutine;
using FieldDay;
using FieldDay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Tutorial {
    public class TutorialAnchorAnim : MonoBehaviour, IOnGuiUpdate {
        private const float Duration = 2;

        public Graphic RingA;
        public Graphic RingB;
        public Graphic RingC;

        [NonSerialized] public float EffectTime;

        private void OnEnable() {
            Game.Gui.RegisterUpdate(this);
            EffectTime = 0;
            ApplyAnim(0);
        }

        private void OnDisable() {
            Game.Gui?.DeregisterUpdate(this);
        }

        private void ApplyAnim(float time) {
            float animA = time / Duration;
            float animB = (animA + 0.333f) % 1;
            float animC = (animB + 0.333f) % 1;

            RingA.rectTransform.SetScale(Mathf.Lerp(0.2f, 1f, Curve.QuadOut.Evaluate(animA)), Axis.XY);
            RingB.rectTransform.SetScale(Mathf.Lerp(0.2f, 1f, Curve.QuadOut.Evaluate(animB)), Axis.XY);
            RingC.rectTransform.SetScale(Mathf.Lerp(0.2f, 1f, Curve.QuadOut.Evaluate(animC)), Axis.XY);

            RingA.SetAlpha(Mathf.Sin(Mathf.PI * animA));
            RingB.SetAlpha(Mathf.Sin(Mathf.PI * animB));
            RingC.SetAlpha(Mathf.Sin(Mathf.PI * animC));
        }

        public void OnGuiUpdate() {
            EffectTime = (EffectTime + Frame.DeltaTime) % Duration;
            ApplyAnim(EffectTime);
        }
    }
}