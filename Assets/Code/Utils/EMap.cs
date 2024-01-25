using System;
using BeauUtil;

namespace Zavala {
    public struct EMap<TKey, TValue>
        where TKey : unmanaged, Enum
    {
        public TValue[] Values;

        public EMap(int capacity) {
            Values = new TValue[capacity];
        }

        public ref TValue this[TKey key] {
            get { return ref Values[Unsafe.FastReinterpret<TKey, int>(key)]; } 
        }

        public ref TValue this[int index] {
            get { return ref Values[index]; }
        }

        public int Length {
            get { return Values.Length; }
        }
    }
}