using BeauRoutine;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Zavala.UI {

    public class AlertFloatSystem : ComponentSystemBehaviour<UIAlert> {
        public float FloatPeriod = 1.2f;
        public float FloatDistance = 0.07f;

        // scale float distance by alpha
        public override void ProcessWorkForComponent(UIAlert component, float deltaTime) {
            component.HoverCycle = (component.HoverCycle + deltaTime / FloatPeriod)%2;
            component.MoveRoot.SetPosition(FloatDistance * Mathf.Sin(component.HoverCycle * Mathf.PI) * component.AlertBase.GetAlpha(), Axis.Y, Space.Self);
        }
    }
}