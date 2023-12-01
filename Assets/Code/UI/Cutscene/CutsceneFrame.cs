using System.Collections;
using BeauRoutine;
using EasyAssetStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    [DefaultExecutionOrder(-1)]
    public class CutsceneFrame : MonoBehaviour {
        public StreamingUGUITexture Texture;
        public LayoutOffset Offset;

        public Routine Animation;

        public void Clear() {
            Animation.Stop();
            Texture.enabled = false;
            Texture.Alpha = 0;
            gameObject.SetActive(false);
        }

        public IEnumerator AnimateOn(float delay) {
            Texture.enabled = true;
            Texture.Alpha = 0;
            Offset.Offset2 = new Vector2(0, 16);

            if (delay > 0) {
                yield return delay;
            }

            yield return Routine.Combine(
                Tween.Float(Texture.Alpha, 1, (f) => Texture.Alpha = f, 0.2f),
                Tween.Vector(Offset.Offset2, default, (v) => Offset.Offset2 = v, 0.25f).Ease(Curve.BackOut)
                );
        }

        public IEnumerator AnimateOff(float delay) {
            Animation.Stop();
            if (Texture.enabled) {
                if (delay > 0) {
                    yield return delay;
                }

                yield return Routine.Combine(
                    Tween.Float(Texture.Alpha, 0, (f) => Texture.Alpha = f, 0.25f),
                    Tween.Vector(Offset.Offset2, new Vector2(0, -16), (v) => Offset.Offset2 = v, 0.25f).Ease(Curve.BackIn)
                    );
                Texture.enabled = false;
            }
        }
    }
}