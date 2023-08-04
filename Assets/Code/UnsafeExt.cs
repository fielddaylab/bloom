using System;
using BeauUtil;
using BeauUtil.Debugger;

namespace Zavala {
    static public unsafe class UnsafeExt {
        /// <summary>
        /// Shuffles an unmanaged buffer's contents.
        /// </summary>
        static public void Shuffle<T>(T* buffer, int length, Random rng) where T : unmanaged {
            rng.Shuffle(buffer, length);
        }

        /// <summary>
        /// Removes an element from the given unmanaged array using the swap method.
        /// </summary>
        static public void FastRemoveAt<T>(T* buffer, ref int length, int index) where T : unmanaged {
            Assert.True(buffer != null, "Buffer is null");
            if (index != length - 1) {
                buffer[index] = buffer[length - 1];
            }
            length--;
        }
    }
}