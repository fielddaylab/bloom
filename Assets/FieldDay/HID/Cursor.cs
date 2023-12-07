using BeauUtil;
using UnityEngine;

namespace FieldDay.HID {
    static public class CursorUtility {
        /// <summary>
        /// Returns if the main cursor is within the game window.
        /// </summary>
        static public bool IsCursorWithinGameWindow() {
            Vector2 mousePos = Input.mousePosition;
            return mousePos.x >= 0 && mousePos.x < Screen.width
                && mousePos.y >= 0 && mousePos.y < Screen.height;
        }
    }
}