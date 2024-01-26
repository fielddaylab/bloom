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
using Zavala.Data;

namespace Zavala.World {
    [SharedStateInitOrder(100)]
    public sealed class SimWorldCamera : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks {
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

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Camera");
        }

        void IRegistrationCallbacks.OnRegister() {
            ZavalaGame.SaveBuffer.RegisterHandler("Camera", this);
        }

        #endregion // Inspector

        void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            LookTarget.position = reader.Read<Vector3>();
            Camera.transform.SetPosition(reader.Read<float>(), Axis.Z, Space.Self);
        }

        void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write(LookTarget.position);
            writer.Write(Camera.transform.localPosition.z);
        }
    }

    public static class WorldCameraUtility {
        [SharedStateReference] static public SimWorldCamera Cam { get; private set; }

        [LeafMember("PanToBuilding")]
        public static void PanCameraToActor(StringHash32 id, float xzoffset = -1.5f) {
            if (!ScriptUtility.ActorExists(id)) {
                Log.Error("[WorldCameraUtility] Error: tried to pan to nonexistent actor.");
                return;
            }
            PanCameraToPoint(Cam, ScriptUtility.LookupActor(id).transform.position + new Vector3(xzoffset, 0f, xzoffset));
        }

        [LeafMember("PanToRegionCity")]
        public static void PanCameraToRegionCity(int regionOneIndexed) {
            string cityId = "region" + regionOneIndexed.ToStringLookup() + "_city1";
            PanCameraToActor(cityId);
        }

        public static void PanCameraToTransform(Transform t) {
            PanCameraToPoint(Cam, t.position);
        }

        public static void PanCameraToPoint(Vector3 pt) {
            PanCameraToPoint(Cam, pt);
        }


        public static void PanCameraToPoint(SimWorldCamera cam, Vector3 pt) {
            cam.PanTargetPoint = pt + cam.PanTargetOffset;
            cam.TransitionRoutine.Replace(cam, PanRoutine(cam)).SetPhase(RoutinePhase.Update);
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