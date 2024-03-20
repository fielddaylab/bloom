using BeauUtil;
using BeauUtil.Debugger;

namespace Zavala {
    /// <summary>
    /// Simulation allocator.
    /// </summary>
    static public class SimAllocator {
        static private Unsafe.ArenaHandle s_Allocator;

        static public void Initialize(int amount) {
            Unsafe.TryDestroyArena(ref s_Allocator);
            s_Allocator = Unsafe.CreateArena(amount, "Sim");
        }

        static public unsafe byte* Alloc(int count) {
            return (byte*) Unsafe.Alloc(s_Allocator, count);
        }

        static public unsafe T* Alloc<T>() where T : unmanaged {
            return Unsafe.Alloc<T>(s_Allocator);
        }

        static public unsafe T* Alloc<T>(int count) where T : unmanaged {
            return Unsafe.AllocArray<T>(s_Allocator, count);
        }

        static public unsafe UnsafeSpan<T> AllocSpan<T>(int count) where T : unmanaged {
            return Unsafe.AllocSpan<T>(s_Allocator, count);
        }

        static public unsafe Unsafe.ArenaHandle AllocArena(int size) {
            return Unsafe.CreateArena(s_Allocator, size);
        }

        static public unsafe Unsafe.ArenaHandle AllocArena<T>(int size) where T : unmanaged {
            return Unsafe.CreateArena(s_Allocator, size * sizeof(T));
        }

        static public void Reset() {
            s_Allocator.Reset();
        }

        static public void Destroy() {
            Unsafe.TryDestroyArena(ref s_Allocator);
        }

        static public void DumpStats() {
            Log.Msg("[SimAllocator] Remaining: {0}", Unsafe.FormatBytes(s_Allocator.FreeBytes()));
        }
    }
}