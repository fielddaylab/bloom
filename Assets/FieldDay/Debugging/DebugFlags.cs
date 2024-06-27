#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeauUtil;

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug flags.
    /// </summary>
    static public class DebugFlags {
        #region Scene Launch

#if UNITY_EDITOR
        static private bool s_LaunchedFromScene = true;

        /// <summary>
        /// Detects whether the game was launched from this scene.
        /// </summary>
        static public bool LaunchedFromThisScene {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return s_LaunchedFromScene; }
        }
#else
        public const bool LaunchedFromThisScene = false;
#endif // UNITY_EDITOR

        [Conditional("UNITY_EDITOR")]
        static internal void MarkNewSceneLoaded() {
#if UNITY_EDITOR
            s_LaunchedFromScene = false;
#endif // UNITY_EDITOR
        }

        #endregion // Scene Launch

        #region Flags

#if DEVELOPMENT
        static private BitSet256 s_Flags;
#endif // DEVELOPMENT

        /// <summary>
        /// Returns if the given debug flag is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsFlagSet<T>(T index) where T : unmanaged, Enum {
#if DEVELOPMENT
            return s_Flags.IsSet(Enums.ToInt(index));
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag. Returns the previous value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool SetFlag<T>(T index, bool value) where T : unmanaged, Enum {
#if DEVELOPMENT
            int idx = Enums.ToInt(index);
            bool val = s_Flags.IsSet(idx);
            s_Flags.Set(idx, value);
            return val;
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Returns if the given debug flag is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsFlagSet(int index) {
#if DEVELOPMENT
            return s_Flags.IsSet(index);
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Sets the given debug flag. Returns the previous value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool SetFlag(int index, bool value) {
#if DEVELOPMENT
            bool val = s_Flags.IsSet(index);
            s_Flags.Set(index, value);
            return val;
#else
            return false;
#endif // DEVELOPMENT
        }

        #endregion // Flags
    }
}