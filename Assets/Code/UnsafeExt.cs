using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Zavala {
    static public unsafe class UnsafeExt {
        static public NativeArray<T> TempNativeArray<T>(T* buffer, int bufferLength) where T : unmanaged {
            NativeArray<T> native = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, bufferLength, Allocator.None);
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref native, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
            #endif // ENABLE_UNITY_COLLECTIONS_CHECKS
            return native;
        }
    }
}