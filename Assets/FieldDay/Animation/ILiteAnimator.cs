namespace FieldDay.Animation {
    public interface ILiteAnimator {
        bool UpdateAnimation(ref LiteAnimatorState state, float deltaTime);
    }

    public struct LiteAnimatorState {
        public float TimeRemaining;
        public float Duration;
        public int StateId;
    }
}