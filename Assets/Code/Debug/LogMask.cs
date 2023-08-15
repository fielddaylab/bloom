#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;

namespace Zavala.Debugging
{
    [Flags]
    public enum LogMask : uint
    {
        Input = 1 << 0,
        Scripting = 1 << 2,
        Audio = 1 << 4,
        Loading = 1 << 5,
        Camera = 1 << 6,
        UI = 1 << 8,
        Localization = 1 << 12,

        DEFAULT = Loading,
        ALL = Input | Scripting | Audio | Loading | Camera
            | UI | Localization
    }
}