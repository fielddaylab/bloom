using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class LensButton : MonoBehaviour {
        public CanvasGroup Group;
        public RectTransform Rect;

        [Header("Button")]
        public Button Button;
        public Graphic Icon;
        public Graphic CloseIcon;

        [Header("Label")]
        public Color LabelBGColor;
        public Color LabelTextColor;
        public TextId LabelText;

        [NonSerialized] public bool CurrentState;
        public Routine StateRoutine;
    }
}