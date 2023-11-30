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
            PanCameraToPoint(ZavalaGame.SharedState.Get<SimWorldCamera>(), ZavalaGame.SharedState.Get<InteractionState>(), ScriptUtility.LookupActor(id).transform);
        }

        public static void PanCameraToPoint(SimWorldCamera cam, InteractionState interactions, Transform t) {
            cam.PanTarget = t;
            cam.m_TransitionRoutine.Replace(PanRoutine(cam, interactions));
        }

        private static IEnumerator PanRoutine(SimWorldCamera cam, InteractionState interactions) {
            InteractionUtility.DisableInteraction(interactions, InteractionMask.Movement);

            yield return cam.transform.MoveToWithSpeed(cam.PanTarget.position, cam.CameraMoveSpeed).Ease(Curve.Smooth);
            cam.PanTarget = null;

            InteractionUtility.EnableInteraction(interactions, InteractionMask.Movement);

            yield return null;
        }
    }
}