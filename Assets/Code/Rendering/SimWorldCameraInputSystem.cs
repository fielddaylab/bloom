using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.UI;

namespace Zavala.World {
    [SysUpdate(GameLoopPhase.UnscaledUpdate, 100)]
    public sealed class SimWorldCameraInputSystem : SharedStateSystemBehaviour<SimWorldCamera, InputState> {
        public override void ProcessWork(float deltaTime) {
            Vector2 moveDist = m_StateB.NormalizedKeyboardMoveVector * (m_StateA.CameraMoveSpeed * deltaTime);
            if (moveDist.sqrMagnitude > 0) {
                Vector3 cameraRotEuler = m_StateA.LookTarget.localEulerAngles;
                Quaternion cameraRot = Quaternion.Euler(0, cameraRotEuler.y, 0);
                Vector3 moveVec = cameraRot * Geom.SwizzleYZ(moveDist);
                m_StateA.LookTarget.Translate(moveVec, Space.World);
                // TODO: better solution?
                // temp: close context menu when moving
                BuildingPopup.instance.CloseMenu();
            }
        }
    }
}