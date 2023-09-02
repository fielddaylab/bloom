
using UnityEngine;
using UnityEngine.UI;

public class MultiImageButton : Button
{
    [SerializeField] private Graphic[] m_targetGraphics; // Note: Can only edit in Debug inspector mode

    protected override void Start() {
        base.Start();
    }

    protected override void DoStateTransition(SelectionState state, bool instant) {
        //get the graphics, if it could not get the graphics, return here
        if (m_targetGraphics == null || m_targetGraphics.Length == 0) {
            return;
        }

        var targetColor =
            state == SelectionState.Disabled ? colors.disabledColor :
            state == SelectionState.Highlighted ? colors.highlightedColor :
            state == SelectionState.Normal ? colors.normalColor :
            state == SelectionState.Pressed ? colors.pressedColor :
            state == SelectionState.Selected ? colors.selectedColor : Color.white;

        foreach (var graphic in m_targetGraphics)
            graphic.CrossFadeColor(targetColor, instant ? 0 : colors.fadeDuration, true, true);
    }
}

