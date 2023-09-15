using System;
using UnityEngine;
using FieldDay.SharedState;

namespace Zavala.Input {
    public class InputState : SharedStateComponent {
        // mouse state
        [NonSerialized] public Vector2 ScreenMousePos;
        [NonSerialized] public Vector2 ViewportMousePos;
        [NonSerialized] public Ray ViewportMouseRay;
        [NonSerialized] public Vector2 ScrollWheel;
        [NonSerialized] public Vector2 MousePressedPosPrev;
        [NonSerialized] public bool MouseDragging;

        // keyboard movement
        [NonSerialized] public Vector2 RawKeyboardMoveVector;
        [NonSerialized] public Vector2 NormalizedKeyboardMoveVector;
        [NonSerialized] public Ray ViewportCenterRay;

        // button states
        [NonSerialized] public InputButton ButtonsDown;
        [NonSerialized] public InputButton ButtonsDownPrev;


        #region Checks

        public bool ButtonDown(InputButton button) {
            return (ButtonsDown & button) != 0;
        }

        public bool ButtonPressed(InputButton button) {
            return (ButtonsDown & button) != 0 && (ButtonsDownPrev & button) == 0;
        }

        public bool ButtonReleased(InputButton button) {
            return (ButtonsDown & button) == 0 && (ButtonsDownPrev & button) != 0;
        }

        #endregion // Checks
    }

    [Flags]
    public enum InputButton : uint {
        PrimaryMouse = 0x01,
        RightMouse = 0x02,
        Left = 0x04,
        Right = 0x08,
        Up = 0x10,
        Down = 0x20,

        Pause = 0x100,
        FastForward = 0x200,
    }
}