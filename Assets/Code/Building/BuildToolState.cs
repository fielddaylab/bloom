using System;
using UnityEngine;
using FieldDay.SharedState;

namespace Zavala.Building {
    public class BuildToolState : SharedStateComponent {
        [NonSerialized] public UserBuildTool ActiveTool = UserBuildTool.Road;
        [NonSerialized] public HexVector VecPrev;
    }

    public enum UserBuildTool : byte {
        None = 0,
        Destroy = 1,
        Road = 2,
        Storage = 3,
        Digester = 4,
    }
}