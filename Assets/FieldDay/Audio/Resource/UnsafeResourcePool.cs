using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Audio {
    internal unsafe struct UnsafeResourcePool<T> where T : unmanaged {
        private UnsafeBitSet m_BitMap;
        private UnsafeSpan<T> m_Data;
        private int m_AllocHead;

        public void Create(Unsafe.ArenaHandle arena, int amount) {
            m_BitMap.Bits = arena.AllocSpan<byte>(Unsafe.AlignUp8(amount) / 8);
            m_BitMap.Clear();

            m_Data = arena.AllocSpan<T>(amount);
        }

        /// <summary>
        /// Allocates a new instance from the 
        /// </summary>
        /// <returns></returns>
        public T* Alloc() {
            while(!m_BitMap.IsSet(m_AllocHead)) {
                m_AllocHead = (m_AllocHead + 1) % m_Data.Length;
            }
            T* ptr = m_Data.Ptr + m_AllocHead;
            m_BitMap.Set(m_AllocHead);
            m_AllocHead = (m_AllocHead + 1) % m_Data.Length;
            *ptr = default(T);
            return ptr;
        }

        public void Free(T* ptr) {
            int chunkIdx = (int) (ptr - m_Data.Ptr);
            Assert.True(chunkIdx >= 0 && chunkIdx < m_Data.Length);
            m_BitMap.UnSet(chunkIdx);
        }
    }

    internal unsafe struct UnsafeBitSet {
        public UnsafeSpan<byte> Bits;

        public void Clear() {
            Unsafe.Clear(Bits);
        }

        public bool IsSet(int bit) {
            return ((Bits[bit >> 3]) & (1u << (bit & 0x7))) != 0;
        }

        public void Set(int bit) {
            Bits[bit >> 3] |= (byte) (1u << (bit & 0x7));
        }

        public void UnSet(int bit) {
            Bits[bit >> 3] &= (byte) ~(1u << (bit & 0x7));
        }
    }
}