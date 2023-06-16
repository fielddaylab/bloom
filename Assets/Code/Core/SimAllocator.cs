using BeauUtil;

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

        static public unsafe T* Alloc<T>(int count) where T : unmanaged {
            return Unsafe.AllocArray<T>(s_Allocator, count);
        }

        static public void Reset() {
            s_Allocator.Reset();
        }

        static public void Destroy() {
            Unsafe.TryDestroyArena(ref s_Allocator);
        }
    }
}