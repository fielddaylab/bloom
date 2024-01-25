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
using BeauUtil.Debugger;
using FieldDay;

namespace Zavala.World {
    [SharedStateInitOrder(100)]
    public sealed class SimWorldCamera : SharedStateComponent {
        #region Inspector

        [Header("Camera Positioning")]
        public Camera Camera;
        public Transform LookTarget;
        public Vector3 PanTargetOffset;

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
        [SharedStateReference] static public SimWorldCamera Cam { get; private set; }

        [LeafMember("PanToBuilding")]
        public static void PanCameraToActor(StringHash32 id) {
            if (!ScriptUtility.ActorExists(id))
            {
                Log.Error("[WorldCameraUtility] Error: tried to pan to nonexistent actor.");
                return;
            }
            PanCameraToPoint(Cam, ScriptUtility.LookupActor(id).transform.position);
        }

        [LeafMember("PanToBuildingOffset")]
        public static void PanCameraToActor(StringHash32 id, int xzoffset) {
            if (!ScriptUtility.ActorExists(id)) {
                Log.Error("[WorldCameraUtility] Error: tried to pan to nonexistent actor.");
                return;
            }
            PanCameraToPoint(Cam, ScriptUtility.LookupActor(id).transform.position + new Vector3(xzoffset, 0f, xzoffset));
        }

        [LeafMember("PanToRegionCity")]
        public static void PanCameraToRegionCity(int regionOneIndexed) {
            string cityId = "region" + regionOneIndexed.ToStringLookup() + "_city1";
            PanCameraToActor(cityId, -1);
        }

        public static void PanCameraToTransform(Transform t) {
            PanCameraToPoint(Cam, t.position);
        }

        public static void PanCameraToPoint(Vector3 pt) {
            PanCameraToPoint(Cam, pt);
        }


        public static void PanCameraToPoint(SimWorldCamera cam, Vector3 pt) {
            cam.PanTargetPoint = pt + cam.PanTargetOffset;
            cam.TransitionRoutine.Replace(PanRoutine(cam)).SetPhase(RoutinePhase.Update);
        }

        private static IEnumerator PanRoutine(SimWorldCamera cam) {
            // yield return cam.LookTarget.MoveTo(cam.PanTargetPoint, 0.5f, Axis.XZ).Ease(Curve.Smooth);
            yield return cam.LookTarget.MoveToWithSpeed(cam.PanTargetPoint, cam.CameraMoveSpeed, Axis.XZ).Ease(Curve.CubeOut);
            yield return null;
        }

        static public void ClampPositionToBounds(ref Vector3 position, Rect rect) {
            Vector2 min = rect.min, max = rect.max;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.z = Mathf.Clamp(position.z, min.y, max.y);
        }
    }
}