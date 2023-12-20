using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.UI;
using Leaf.Runtime;
using UnityEngine;

namespace Zavala.Rendering {
    public class SpotlightPanel : SharedPanel {
        public Canvas Canvas;
        public CanvasGroup Group;
        public SpotlightRenderer Renderer;
        public TweenSettings ActivateAnim;
        public TweenSettings DeactivateAnim;

        [NonSerialized] public bool VisibleState;
        [NonSerialized] public float DefaultAlpha;
        [NonSerialized] public Vector2 DefaultSize;
        [NonSerialized] public CanvasSpaceTransformation SpaceHelper;
        [NonSerialized] public Routine Anim;
        [NonSerialized] public ulong LastCameraStateHash;

        protected override void Awake() {
            base.Awake();

            SpaceHelper.CanvasCamera = Game.Gui.PrimaryCamera;
            SpaceHelper.CanvasSpace = (RectTransform) Renderer.Self.parent;
            Canvas.enabled = false;

            DefaultSize = Renderer.Self.sizeDelta;
            DefaultAlpha = Group.alpha;
        }

        public void FocusOn(Transform transform, Vector2 size, float alpha) {
            bool snap = !Canvas.enabled;
            Canvas.enabled = true;

            GetSize(ref size);
            Vector2 localPos = GetLocation(transform);

            if (alpha <= 0) {
                alpha = DefaultAlpha;
            }

            if (snap) {
                Renderer.Self.sizeDelta = size;
                Renderer.Self.localPosition = localPos;
                //Renderer.RecalculateBorders();
                Group.alpha = 0;
            }

            Anim.Replace(this, Activate(localPos, size, alpha));
            VisibleState = true;
        }

        public void Hide(bool instant = false) {
            if (instant) {
                Anim.Stop();
                Canvas.enabled = false;
                VisibleState = false;
                return;
            }

            if (!VisibleState) {
                return;
            }

            Anim.Replace(this, Deactivate());
            VisibleState = false;
        }

        private void GetSize(ref Vector2 size) {
            if (size.x <= 0) {
                size.x = DefaultSize.x;
            }
            if (size.y <= 0) {
                size.y = DefaultSize.y;
            }
        }

        private Vector2 GetLocation(Transform target) {
            Vector3 pos;
            target.TryGetCamera(out SpaceHelper.WorldCamera);
            SpaceHelper.TryConvertToLocalSpace(target, out pos);
            return (Vector2) pos;
        }

        private IEnumerator Activate(Vector2 pos, Vector2 size, float alpha) {
            return Routine.Combine(
                Renderer.Self.SizeDeltaTo(size, ActivateAnim),
                Renderer.Self.MoveTo(pos, ActivateAnim, Axis.XY, Space.Self),
                Group.FadeTo(alpha, ActivateAnim.Time)
            );
        }

        private IEnumerator Deactivate() {
            yield return Group.FadeTo(0, DeactivateAnim.Time);
            Canvas.enabled = false;
        }

        #region Leaf

        [LeafMember("FocusHighlightOn")]
        static private void LeafFocus(StringHash32 objId, float width = 0, float height = 0, float alpha = 0) {
            var panel = Game.Gui?.GetShared<SpotlightPanel>();
            if (panel != null) {
                RectTransform transform = Game.Gui.LookupNamed(objId);
                if (transform != null) {
                    panel.FocusOn(transform, new Vector2(width, height), alpha);
                } else {
                    Log.Warn("[SpotlightPanel] No target found with id '{0}'", objId);
                }
            }
        }

        [LeafMember("FocusClear")]
        static private void LeafClear() {
            Game.Gui?.GetShared<SpotlightPanel>()?.Hide();
        }

        #endregion // Leaf
    }
}