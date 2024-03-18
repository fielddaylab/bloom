using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using Mono.Cecil;

namespace Zavala {
    /// <summary>
    /// Simulation bit set.
    /// </summary>
    public struct SimBitSet : IBitSet {
        private readonly UnsafeSpan<uint> m_Bits;

        public SimBitSet(UnsafeSpan<uint> handle) {
            m_Bits = handle;
        }

        /// <summary>
        /// Resets the bit set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() {
            Unsafe.Clear(m_Bits);
        }

        /// <summary>
        /// Creates a bit set of the given size.
        /// </summary>
        static public SimBitSet Create(uint length) {
            return new SimBitSet(SimAllocator.AllocSpan<uint>((int) Unsafe.AlignUp32(length) >> 5));
        }

        #region IBitSet

        public int Capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Bits.Length * 32; }
        }

        public int Count {
            get {
                int count = 0;
                for(int i = 0; i < m_Bits.Length; i++) {
                    count += Bits.Count(m_Bits[i]);
                }
                return count;
            }
        }

        public bool IsEmpty {
            get {
                for (int i = 0; i < m_Bits.Length; i++) {
                    if (m_Bits[i] != 0) {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool this[int inIndex] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return IsSet(inIndex); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Set(inIndex, value); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int inIndex) {
            Assert.True(inIndex >= 0 && inIndex < Capacity);
            return Bits.Contains(m_Bits[inIndex >> 5], inIndex & 0x1F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int inIndex) {
            Assert.True(inIndex >= 0 && inIndex < Capacity);
            Bits.Add(ref m_Bits[inIndex >> 5], inIndex & 0x1F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int inIndex, bool inValue) {
            Assert.True(inIndex >= 0 && inIndex < Capacity);
            Bits.Set(ref m_Bits[inIndex >> 5], inIndex & 0x1F, inValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unset(int inIndex) {
            Assert.True(inIndex >= 0 && inIndex < Capacity);
            Bits.Remove(ref m_Bits[inIndex >> 5], inIndex & 0x1F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Reset();
        }

        #endregion // IBitSet
    }
}