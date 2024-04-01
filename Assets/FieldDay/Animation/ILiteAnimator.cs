using System.Runtime.InteropServices;
using BeauUtil;

namespace FieldDay.Animation {
    public interface ILiteAnimator {
        bool UpdateAnimation(ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(ref LiteAnimatorState state);
    }

    public struct LiteAnimatorState {
        public float TimeRemaining;
        public float Duration;
        public BitSet32 Flags;
        public int StateId;
        public LiteAnimatorStateParams StateParams;
    }

    public struct LiteAnimatorStateParams {
        public int Int;
        public float Float;
    }
}