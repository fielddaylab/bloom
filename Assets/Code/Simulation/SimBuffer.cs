using System.Runtime.CompilerServices;
using BeauUtil;

namespace Zavala.Sim {
    /// <summary>
    /// Wrapper for a data buffer used for simulation.
    /// </summary>
    public unsafe readonly struct SimBuffer<T> where T : unmanaged {
        public readonly T* Buffer;
        public readonly uint Length;

        public SimBuffer(T* buffer, uint bufferLength) {
            Buffer = buffer;
            Length = bufferLength;
        }

        /// <summary>
        /// Reference to data at this index in the buffer.
        /// </summary>
        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return ref Buffer[index]; }
        }

        /// <summary>
        /// Pointer to data at this index in the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* Ptr(int index) {
            return Buffer + index;
        }

        /// <summary>
        /// Allocates a buffer from the given arena.
        /// </summary>
        static public SimBuffer<T> Create(Unsafe.ArenaHandle arena, uint length) {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) length), length);
        }

        /// <summary>
        /// Allocates a buffer from the given arena, sized to the given grid.
        /// </summary>
        static public SimBuffer<T> Create(Unsafe.ArenaHandle arena, in HexGridSize hexSize) {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) hexSize.Size), hexSize.Size);
        }

        /// <summary>
        /// Allocates a buffer from the given arena, sized to the given grid subregion.
        /// </summary>
        static public SimBuffer<T> Create(Unsafe.ArenaHandle arena, in HexGridSubregion hexSize) {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) hexSize.Size), hexSize.Size);
        }
    }
}