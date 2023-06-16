using System;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Zavala {
    static public unsafe class UnsafeExt {
        /// <summary>
        /// Shuffles an unmanaged buffer's contents.
        /// </summary>
        static public void Shuffle<T>(T* buffer, int length, Random rng) where T : unmanaged {
            Assert.NotNull(rng, "Random is null");
            Assert.True(buffer != null, "BUffer is null");
            int i = length, j;
            while(--i > 0) {
                T old = buffer[i];
                buffer[i] = buffer[j = rng.Next(0, i + 1)];
                buffer[j] = old;
            }
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