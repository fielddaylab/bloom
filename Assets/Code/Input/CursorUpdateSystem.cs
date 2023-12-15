using UnityEngine;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEditor;
using FieldDay.HID;

namespace Zavala.Input {
    [SysUpdate(GameLoopPhase.ApplicationPreRender, 10000)]
    public class CursorUpdateSystem : SharedStateSystemBehaviour<CursorState, InputState> {
        #region Inspector

        [SerializeField] private Vector3 m_DownScale = new Vector3(0.75f, 0.75f, 1);

        #endregion // Inspector

        public override void ProcessWork(float deltaTime) {
#if UNITY_EDITOR
            if (GameLoop.IsFocused() && CursorUtility.IsCursorWithinGameWindow()) {
                Cursor.visible = false;
            } else {
                Cursor.visible = true;
            }
#endif // UNITY_EDITOR

            Vector2 pos = m_StateB.ScreenMousePos;
            pos += m_StateA.IconOffset;

            bool isDown = (m_StateB.ButtonsDown & InputButton.PrimaryMouse) != 0;

            m_StateA.IconTransform.position = pos;
            m_StateA.IconTransform.localScale = isDown ? m_DownScale : Vector3.one;
        }

        public override void Initialize() {
            base.Initialize();
        }

        public override void Shutdown() {
            base.Shutdown();
        }
    }
}