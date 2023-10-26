using System.Runtime.CompilerServices;
using BeauUtil;

namespace Zavala {
    /// <summary>
    /// Simulation sub-allocator.
    /// </summary>
    public struct SimArena<T> where T : unmanaged {
        private readonly Unsafe.ArenaHandle m_ArenaHandle;

        public SimArena(Unsafe.ArenaHandle handle) {
            m_ArenaHandle = handle;
        }

        /// <summary>
        /// Allocates an unsafe array of a certain size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe UnsafeSpan<T> Alloc(uint count) {
            T* ptr = Unsafe.AllocArray<T>(m_ArenaHandle, (int) count);
            return new UnsafeSpan<T>(ptr, count);
        }

        /// <summary>
        /// Resets the sub-allocator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() {
            m_ArenaHandle.Reset();
        }
    }

    static public class SimArena {
        /// <summary>
        /// Creates an arena of the given size.
        /// </summary>
        static public SimArena<T> Create<T>(uint length) where T : unmanaged {
            return new SimArena<T>(SimAllocator.AllocArena<T>((int) length));
        }
    }
}