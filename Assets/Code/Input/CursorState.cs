using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.Input {
    public class CursorState : SharedStateComponent {
        public RectTransform IconTransform;
        public Image Icon;
        public Vector2 IconOffset;
    }
}