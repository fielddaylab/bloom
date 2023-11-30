using BeauRoutine;
using FieldDay.SharedState;
using System.Collections;
using Leaf.Runtime;
using UnityEngine;
using BeauUtil;
using Zavala.Scripting;
using FieldDay.Scripting;
using Zavala.Input;

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

        public Routine m_TransitionRoutine;
        public Transform PanTarget;
        #endregion // Inspector
    }

    public static class CameraUtility {

        [LeafMember("PanToBuilding")]
        public static void PanCameraToBuilding(StringHash32 id) {
            PanCameraToPoint(ZavalaGame.SharedState.Get<SimWorldCamera>(), ScriptUtility.LookupActor(id).transform);
        }

        public static void PanCameraToPoint(SimWorldCamera cam, Transform t) {
            cam.PanTarget = t;
            cam.m_TransitionRoutine.Replace(PanRoutine(cam));
        }

        private static IEnumerator PanRoutine(SimWorldCamera cam) {
            yield return cam.transform.MoveToWithSpeed(cam.PanTarget.position, cam.CameraMoveSpeed).Ease(Curve.Smooth);
            cam.PanTarget = null;

            yield return null;
        }
    }
}