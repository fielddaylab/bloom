using System.Runtime.CompilerServices;
using BeauUtil;
using System;
using UnityEngine.UIElements;
using BeauUtil.Debugger;

namespace Zavala {
    /// <summary>
    /// Wrapper for a data buffer used for simulation.
    /// </summary>
    public unsafe readonly struct SimBuffer<T> where T : unmanaged {
        public readonly T* Buffer;
        public readonly uint Length;

        public SimBuffer(T* buffer, uint bufferLength, bool zero = false) {
            Buffer = buffer;
            Length = bufferLength;
        }

        /// <summary>
        /// Reference to data at this index in the buffer.
        /// </summary>
        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                Assert.True(index >= 0 && index < Length, "Index out of range");
                return ref Buffer[index];
            }
        }

        /// <summary>
        /// Pointer to data at this index in the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* Ptr(int index) {
            Assert.True(index >= 0 && index < Length, "Index out of range");
            return Buffer + index;
        }
    }

    /// <summary>
    /// Delegate for mapping data from one type to another.
    /// </summary>
    public delegate U SimBufferMapDelegate<T, U>(in T data);

    /// <summary>
    /// SimBuffer utility methods.
    /// </summary>
    static public unsafe class SimBuffer {
        #region Create

        /// <summary>
        /// Allocates a buffer from the global simulation.
        /// </summary>
        static public SimBuffer<T> Create<T>(uint length) where T : unmanaged {
            return new SimBuffer<T>(SimAllocator.Alloc<T>((int) length), length);
        }

        /// <summary>
        /// Allocates a buffer from the given arena.
        /// </summary>
        static public SimBuffer<T> Create<T>(Unsafe.ArenaHandle arena, uint length) where T : unmanaged {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) length), length);
        }

        /// <summary>
        /// Allocates a buffer from the global simulation, sized to the given grid.
        /// </summary>
        static public SimBuffer<T> Create<T>(in HexGridSize hexSize) where T : unmanaged {
            return new SimBuffer<T>(SimAllocator.Alloc<T>((int) hexSize.Size), hexSize.Size);
        }

        /// <summary>
        /// Allocates a buffer from the given arena, sized to the given grid.
        /// </summary>
        static public SimBuffer<T> Create<T>(Unsafe.ArenaHandle arena, in HexGridSize hexSize) where T : unmanaged {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) hexSize.Size), hexSize.Size);
        }

        /// <summary>
        /// Allocates a buffer from the global simulation arena, sized to the given grid subregion.
        /// </summary>
        static public SimBuffer<T> Create<T>(in HexGridSubregion hexSize) where T : unmanaged {
            return new SimBuffer<T>(SimAllocator.Alloc<T>((int) hexSize.Size), hexSize.Size);
        }

        /// <summary>
        /// Allocates a buffer from the given arena, sized to the given grid subregion.
        /// </summary>
        static public SimBuffer<T> Create<T>(Unsafe.ArenaHandle arena, in HexGridSubregion hexSize) where T : unmanaged {
            return new SimBuffer<T>(Unsafe.AllocArray<T>(arena, (int) hexSize.Size), hexSize.Size);
        }

        #endregion // Create

        #region Copy/Map

        /// <summary>
        /// Copies this buffer to another buffer.
        /// </summary>
        static public void Copy<T>(SimBuffer<T> source, SimBuffer<T> target) where T : unmanaged {
            if (target.Length != source.Length) {
                throw new ArgumentException("target buffer must be same length");
            }

            Unsafe.CopyArray(source.Buffer, source.Length, target.Buffer, target.Length);
        }

        /// <summary>
        /// Maps data from this buffer to another buffer.
        /// </summary>
        static public void Map<T, U>(SimBuffer<T> source, SimBuffer<U> target, SimBufferMapDelegate<T, U> mapper) where T : unmanaged where U : unmanaged {
            if (target.Length != source.Length) {
                throw new ArgumentException("target buffer must be same length");
            }


            for (int i = 0; i < source.Length; i++) {
                target.Buffer[i] = mapper(source.Buffer[i]);
            }
        }

        #endregion // Copy/Map

        #region Clear

        /// <summary>
        /// Clears the given buffer.
        /// </summary>
        static public void Clear<T>(SimBuffer<T> source) where T : unmanaged {
            for (int i = 0; i < source.Length; i++) {
                source.Buffer[i] = default(T);
            }
        }

        /// <summary>
        /// Clears the given buffer to the given value.
        /// </summary>
        static public void Clear<T>(SimBuffer<T> source, in T value) where T : unmanaged {
            for (int i = 0; i < source.Length; i++) {
                source.Buffer[i] = value;
            }
        }

        #endregion // Clear
    }
}