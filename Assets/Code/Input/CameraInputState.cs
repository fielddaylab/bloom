using System;
using UnityEngine;
using FieldDay.SharedState;

namespace Zavala.Input {
    public class CameraInputState : SharedStateComponent {
        [NonSerialized] public CameraInputMode InputMode;

        [NonSerialized] public Plane DragPlane;
        [NonSerialized] public Vector2 DragOriginViewport;
        [NonSerialized] public Vector3 DragOriginWorld;
    }

    public enum CameraInputMode {
        None,
        Drag,
        Keyboard,
        Cutscene
    }
}