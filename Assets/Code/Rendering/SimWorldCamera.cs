using BeauRoutine;
using FieldDay.SharedState;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

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