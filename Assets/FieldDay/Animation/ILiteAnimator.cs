using System.Runtime.InteropServices;
using BeauUtil;

namespace FieldDay.Animation {
    public interface ILiteAnimator {
        bool UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(object target, ref LiteAnimatorState state);
    }

    public struct LiteAnimatorState {
        public float TimeRemaining;
        public float Duration;
        public BitSet32 Flags;
        public int StateId;
        public LiteAnimatorStateParam StateParams;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LiteAnimatorStateParam {
        [FieldOffset(0)] public bool Bool;
        [FieldOffset(0)] public int Int;
        [FieldOffset(0)] public float Float;
    }
}