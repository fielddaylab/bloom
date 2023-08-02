using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using BeauUtil;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay {
    /// <summary>
    /// Frame tracker and per-frame allocations.
    /// </summary>
    static public class Frame {
        #region Index

        public const ushort InvalidIndex = ushort.MaxValue;
        private const ushort MaxIndex = (ushort) (1u << 15);

        /// <summary>
        /// 16-bit frame index.
        /// </summary>
        static public ushort Index;

        /// <summary>
        /// 8-bit frame index.
        /// </summary>
        static public byte Index8 {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (byte) (Index & 0xFF); }
        }

        /// <summary>
        /// Returns whether the current frame is on the given interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Interval(int interval) {
            return (Index % interval) == 0;
        }

        /// <summary>
        /// Returns whether the current frame is on the given offset interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Interval(int interval, int offset) {
            return ((Index - offset) % interval) == 0;
        }

        /// <summary>
        /// Increments the frame counter.
        /// </summary>
        static internal void IncrementFrame() {
            Index = (ushort) ((Index + 1) % MaxIndex);
        }

        #endregion // Index

        #region Allocator

        internal const int EditorHeapSize = 1024;
        internal const int RuntimeHeapSize = 256;

        static private bool s_AllocatorReady;
        static private Unsafe.ArenaHandle s_Allocator;

        #region Lifetime

        /// <summary>
        /// Creates the per-frame allocator with the default size.
        /// </summary>
        static internal void CreateAllocator() {
            #if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                CreateAllocator(EditorHeapSize);
                return;
            }
            #endif // UNITY_EDITOR

            CreateAllocator(RuntimeHeapSize);
        }

        /// <summary>
        /// Creates the per-frame allocator with the specified number of kilobytes.
        /// </summary>
        static internal void CreateAllocator(int size) {
            DestroyAllocator();

            s_Allocator = Unsafe.CreateArena(size * 1024, "Frame");
            s_AllocatorReady = true;
            Log.Msg("[Frame] Initialized per-frame allocator; size={0}KiB", size);
        }

        /// <summary>
        /// Clears the per-frame allocator.
        /// </summary>
        static internal void ResetAllocator() {
            #if UNITY_EDITOR
            if (!s_AllocatorReady) {
                return;
            }
            if (s_Allocator.FreeBytes() < s_Allocator.Size() / 4) {
                Log.Warn("[Frame] Per-frame allocator hit 75%+ capacity");
            }
            #endif // UNITY_EDITOR
            s_Allocator.Reset();
        }

        /// <summary>
        /// Destroys the per-frame allocator.
        /// </summary>
        static internal void DestroyAllocator() {
            if (Unsafe.TryDestroyArena(ref s_Allocator)) {
                Log.Msg("[Frame] Destroyed per-frame allocator");
                s_AllocatorReady = false;
            }
        }

        #endregion // Lifetime

        #region Allocations

        /// <summary>
        /// Allocates an instance of an ummanaged type from the per-frame allocator.
        /// </summary>
        static public unsafe T* Alloc<T>() where T : unmanaged {
            T* addr = Unsafe.Alloc<T>(s_Allocator);
            Assert.True(addr != null, "Per-frame allocator out of space");
            return addr;
        }

        /// <summary>
        /// Allocates an array of an ummanaged type from the per-frame allocator.
        /// </summary>
        static public unsafe T* AllocArray<T>(int size) where T : unmanaged {
            T* addr = Unsafe.AllocArray<T>(s_Allocator, size);
            Assert.True(addr != null, "Per-frame allocator out of space");
            return addr;
        }

        /// <summary>
        /// Allocates an arbitrarily-sized buffer from the per-frame allocator.
        /// </summary>
        static public unsafe void* Alloc(int size) {
            void* addr = Unsafe.Alloc(s_Allocator, size);
            Assert.True(addr != null, "Per-frame allocator out of space");
            return addr;
        }

        #endregion // Allocations

        #endregion // Allocator

        #region Active Checks

        /// <summary>
        /// Is the given Unity Object active and ready to access frame functions
        /// </summary>
        static public bool IsActive(UnityEngine.Object obj) {
            if (!s_AllocatorReady) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(obj) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        /// <summary>
        /// Is the given Behavior active and ready to access frame functions
        /// </summary>
        static public bool IsActive(Behaviour behavior) {
            if (!s_AllocatorReady || !behavior.isActiveAndEnabled) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(behavior) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        /// <summary>
        /// Is the given GameObject active and ready to access frame functions
        /// </summary>
        static public bool IsActive(GameObject gameObject) {
            if (!s_AllocatorReady || !gameObject.activeInHierarchy) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        #endregion // Active Checks
    
        #region Editor

        #if UNITY_EDITOR

        [InitializeOnLoadMethod]
        static private void EditorInitialize() {
            EditorApplication.playModeStateChanged += (state) => {
                if (state == PlayModeStateChange.ExitingEditMode) {
                    DestroyAllocator();
                    EditorApplication.update -= EditorAdvance;
                } else if (state == PlayModeStateChange.EnteredEditMode) {
                    CreateAllocator();
                    EditorApplication.update += EditorAdvance;
                }
            };

            EditorApplication.quitting += DestroyAllocator;
            AppDomain.CurrentDomain.DomainUnload += (_, __) => DestroyAllocator();

            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            EditorApplication.update += EditorAdvance;
            CreateAllocator();
        }

        static private void EditorAdvance() {
            IncrementFrame();
            ResetAllocator();
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}