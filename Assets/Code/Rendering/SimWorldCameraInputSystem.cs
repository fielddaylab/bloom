using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.UI;

namespace Zavala.World {
    [SysUpdate(GameLoopPhase.UnscaledUpdate, 100)]
    public sealed class SimWorldCameraInputSystem : SharedStateSystemBehaviour<SimWorldCamera, InputState, SimWorldState, InteractionState> {
        public override void ProcessWork(float deltaTime) {
            CameraInputState camInput = Game.SharedState.Get<CameraInputState>();

            if (camInput.InputMode == CameraInputMode.Cutscene) {
                if (!m_StateA.TransitionRoutine) {
                    camInput.InputMode = CameraInputMode.None;
                } else {
                    return;
                }
            }

            if ((m_StateD.AllowedInteractions & InteractionMask.Movement) == 0) {
                camInput.InputMode = CameraInputMode.None;
                // Only move camera when Movement interaction is allowed
                return;
            }

            Vector2 flatMove = default;
            Vector3 worldMove = default;

            Vector2 keyboardVec = m_StateB.NormalizedKeyboardMoveVector;
            if (keyboardVec.x != 0 || keyboardVec.y != 0) {
                camInput.InputMode = CameraInputMode.Keyboard;
                flatMove = keyboardVec * ((m_StateA.CameraMoveSpeed + 0.75f * (m_StateC.RegionCount - 1)) * deltaTime);
            } else if (camInput.InputMode == CameraInputMode.Keyboard) {
                camInput.InputMode = CameraInputMode.None;
            }

            if (camInput.InputMode == CameraInputMode.None) {
                if (m_StateB.ButtonPressed(InputButton.MiddleMouse | InputButton.RightMouse)) {
                    camInput.InputMode = CameraInputMode.Drag;
                    camInput.DragOriginViewport = m_StateB.ViewportMousePos;
                    CastRayToCreatePlane(m_StateB.ViewportMouseRay, out camInput.DragPlane, out camInput.DragOriginWorld);
                }
            } else if (camInput.InputMode == CameraInputMode.Drag) {
                if (!m_StateB.ButtonDown(InputButton.MiddleMouse | InputButton.RightMouse)) {
                    camInput.InputMode = CameraInputMode.None;
                } else if (Vector2.SqrMagnitude(m_StateB.ViewportMousePos - camInput.DragOriginViewport) > 0) {
                    bool rayHit = camInput.DragPlane.Raycast(m_StateB.ViewportMouseRay, out float intersectPoint);
                    Assert.True(rayHit);
                    Vector3 newPos = m_StateB.ViewportMouseRay.GetPoint(intersectPoint);

                    // recalculate old point with new camera matrix
                    // this helps prevent jittering
                    Ray oldRay = m_StateA.Camera.ViewportPointToRay(camInput.DragOriginViewport, Camera.MonoOrStereoscopicEye.Mono);
                    rayHit = camInput.DragPlane.Raycast(oldRay, out intersectPoint);
                    Assert.True(rayHit);
                    camInput.DragOriginWorld = oldRay.GetPoint(intersectPoint); 

                    worldMove = camInput.DragOriginWorld - newPos;
                    worldMove.y = 0;
                    //Log.Msg("moving camera {0}", worldMove);
                    camInput.DragOriginWorld = newPos;
                    camInput.DragOriginViewport = m_StateB.ViewportMousePos;
                }
            }
            
            if (flatMove.sqrMagnitude > 0 || worldMove.sqrMagnitude > 0) {
                Vector3 cameraRotEuler = m_StateA.LookTarget.localEulerAngles;
                Quaternion cameraRot = Quaternion.Euler(0, cameraRotEuler.y, 0);
                Vector3 moveVec = cameraRot * Geom.SwizzleYZ(flatMove) + worldMove;

                Vector3 pos = m_StateA.LookTarget.position + moveVec;

                if (camInput.LockRegion != Tile.InvalidIndex16) {
                    WorldCameraUtility.ClampPositionToBounds(ref pos, camInput.LockedBounds);
                } else {
                    WorldCameraUtility.ClampPositionToBounds(ref pos, m_StateC.CameraBounds);
                }

                m_StateA.LookTarget.position = pos;
                m_StateA.TransitionRoutine.Stop();
            }

            float zoomDelta = m_StateB.ScrollWheel.y * m_StateA.ZoomFactor;
            if (zoomDelta != 0) {
                if (camInput.InputMode == CameraInputMode.Drag) {
                    camInput.InputMode = CameraInputMode.None;
                }
                Vector3 camPos = m_StateA.Camera.transform.localPosition;
                if (zoomDelta > 0) {
                    camPos.z = Mathf.Min(m_StateA.CameraMaxZoomDist, camPos.z + zoomDelta);
                } else {
                    camPos.z = Mathf.Max(m_StateA.CameraMinZoomDist - 1 * (m_StateC.RegionCount - 1), camPos.z + zoomDelta);
                }
                m_StateA.Camera.transform.localPosition = camPos;
            }
        }

        static private void CastRayToCreatePlane(Ray ray, out Plane plane, out Vector3 origin) {
            if (Physics.Raycast(ray, out RaycastHit hit, 1000, LayerMasks.HexTile_Mask, QueryTriggerInteraction.UseGlobal)) {
                origin = hit.point;
                plane = new Plane(Vector3.up, origin);
            } else {
                plane = new Plane(Vector3.up, default(Vector3));
                bool rayHit = plane.Raycast(ray, out float intersect);
                Assert.True(rayHit);
                origin = ray.GetPoint(intersect);
            }
        }
    }
}