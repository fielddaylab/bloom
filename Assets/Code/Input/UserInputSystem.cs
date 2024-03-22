using System;
using UnityEngine;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay;
using Zavala.Sim;

namespace Zavala.Input {
    /// <summary>
    /// Input system that relies on user input.
    /// TODO: A system that relies on an external input stream (e.g. for playback)
    /// </summary>
    [SysUpdate(GameLoopPhase.PreUpdate)]
    public class UserInputSystem : SharedStateSystemBehaviour<InputState> {
        public override void ProcessWork(float deltaTime) {
            m_State.ButtonsDownPrev = m_State.ButtonsDown;
            m_State.ConsumedButtons = 0;

            InputButton buttons = 0;

            CheckKeyboard(ref buttons, InputButton.FastForward, KeyCode.F);
            CheckKeyboard(ref buttons, InputButton.FastForward, KeyCode.LeftShift);

            CheckKeyboard(ref buttons, InputButton.Pause, KeyCode.Space);
            CheckKeyboard(ref buttons, InputButton.Pause, KeyCode.P);
            CheckKeyboard(ref buttons, InputButton.Pause, KeyCode.Escape);

            CheckMouse(ref buttons, InputButton.PrimaryMouse, 0);
            CheckMouse(ref buttons, InputButton.RightMouse, 1);
            CheckMouse(ref buttons, InputButton.MiddleMouse, 2);

            CheckKeyboard(ref buttons, InputButton.Left, KeyCode.LeftArrow);
            CheckKeyboard(ref buttons, InputButton.Left, KeyCode.A);
            CheckKeyboard(ref buttons, InputButton.Right, KeyCode.RightArrow);
            CheckKeyboard(ref buttons, InputButton.Right, KeyCode.D);
            CheckKeyboard(ref buttons, InputButton.Up, KeyCode.UpArrow);
            CheckKeyboard(ref buttons, InputButton.Up, KeyCode.W);
            CheckKeyboard(ref buttons, InputButton.Down, KeyCode.DownArrow);
            CheckKeyboard(ref buttons, InputButton.Down, KeyCode.S);

            CheckKeyboard(ref buttons, InputButton.ZoomIn, KeyCode.Equals);
            CheckKeyboard(ref buttons, InputButton.ZoomOut, KeyCode.Minus);

            CheckKeyboard(ref buttons, InputButton.DialogAdvance, KeyCode.X);
            CheckKeyboard(ref buttons, InputButton.DialogAdvance, KeyCode.F);
            CheckKeyboard(ref buttons, InputButton.DialogAdvance, KeyCode.LeftControl);
            CheckMouse(ref buttons, InputButton.DialogAdvance, 0);

            m_State.ButtonsDown = buttons;

            GetMousePosition(ref m_State.ScreenMousePos, ref m_State.ViewportMousePos);
            m_State.ScrollWheel += UnityEngine.Input.mouseScrollDelta;

            if (m_State.ButtonPressed(InputButton.PrimaryMouse)) {
                m_State.MousePressedPosPrev = m_State.ScreenMousePos;
            }
            if (m_State.ButtonDown(InputButton.PrimaryMouse)) {
                if (Vector2.Distance(m_State.ScreenMousePos, m_State.MousePressedPosPrev) > 10) {
                    m_State.MouseDragging = true;
                } 
            } else m_State.MouseDragging = false;

            Vector2 keyboardMoveVector = default;
            if (m_State.ButtonDown(InputButton.Left)) {
                keyboardMoveVector.x -= 1;
            }
            if (m_State.ButtonDown(InputButton.Right)) {
                keyboardMoveVector.x += 1;
            }
            if (m_State.ButtonDown(InputButton.Up)) {
                keyboardMoveVector.y += 1;
            }
            if (m_State.ButtonDown(InputButton.Down)) {
                keyboardMoveVector.y -= 1;
            }

            m_State.RawKeyboardMoveVector = keyboardMoveVector;
            m_State.NormalizedKeyboardMoveVector = keyboardMoveVector.normalized;
        }

        static private void GetMousePosition(ref Vector2 screenPos, ref Vector2 viewportPos) {
            Vector2 pos = UnityEngine.Input.mousePosition;
            screenPos = pos;

            pos.x /= Screen.width;
            pos.y /= Screen.height;
            viewportPos = pos;
        }

        static private void CheckKeyboard(ref InputButton buttonsDown, InputButton button, KeyCode key) {
            if (Game.Input.IsKeyDown(key)) {
                buttonsDown |= button;
            }
        }

        static private void CheckMouse(ref InputButton buttonsDown, InputButton button, int mouseButton) {
            if (Game.Input.IsMouseDown(mouseButton)) {
                buttonsDown |= button;
            }
        }
    }
}