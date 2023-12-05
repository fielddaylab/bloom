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

        [NonSerialized] public Routine m_TransitionRoutine;
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
            cam.m_TransitionRoutine.Replace(PanRoutine(cam));
        }

        private static IEnumerator PanRoutine(SimWorldCamera cam) {
            yield return cam.transform.MoveToWithSpeed(cam.PanTargetPoint, cam.CameraMoveSpeed).Ease(Curve.Smooth);
            // cam.PanTargetPoint;

            yield return null;
        }
    }
}