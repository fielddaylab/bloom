using BeauRoutine;
using FieldDay.SharedState;
using System.Collections;
using Leaf.Runtime;
using UnityEngine;
using BeauUtil;
using Zavala.Scripting;
using FieldDay.Scripting;
using Zavala.Input;
using System;

namespace Zavala.World {
    [SharedStateInitOrder(100)]
    public sealed class SimWorldCamera : SharedStateComponent {
        #region Inspector

        [Header("Camera Positioning")]
        public Camera Camera;
        public Transform LookTarget;

        [Header("Camera Movement")]
        public float CameraMoveSpeed;
        public float CameraMaxZoomDist;
        public float CameraMinZoomDist;
        public int ZoomFactor;

        [NonSerialized] public Routine TransitionRoutine;
        // public Transform PanTarget;
        [NonSerialized] public Vector3 PanTargetPoint;
        #endregion // Inspector
    }

    public static class WorldCameraUtility {

        [LeafMember("PanToBuilding")]
        public static void PanCameraToBuilding(StringHash32 id) {
            PanCameraToPoint(ZavalaGame.SharedState.Get<SimWorldCamera>(), ScriptUtility.LookupActor(id).transform.position);
        }

        public static void PanCameraToTransform(Transform t) {
            PanCameraToPoint(ZavalaGame.SharedState.Get<SimWorldCamera>(), t.position);
        }

        public static void PanCameraToPoint(SimWorldCamera cam, Vector3 pt) {
            cam.PanTargetPoint = pt;
            cam.TransitionRoutine.Replace(PanRoutine(cam)).SetPhase(RoutinePhase.Update);
        }

        private static IEnumerator PanRoutine(SimWorldCamera cam) {
            yield return cam.LookTarget.MoveToWithSpeed(cam.PanTargetPoint, cam.CameraMoveSpeed, Axis.XZ).Ease(Curve.Smooth);
            // cam.PanTargetPoint;

            yield return null;
        }

        static public void ClampPositionToBounds(ref Vector3 position, Rect rect) {
            Vector2 min = rect.min, max = rect.max;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.z = Mathf.Clamp(position.z, min.y, max.y);
        }
    }
}