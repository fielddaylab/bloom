using System;
using System.Runtime.CompilerServices;

namespace FieldDay.Animation {
    static public class AnimUtility {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool CrossedThreshold(float currentTime, float desiredTime, float deltaTime) {
            return Math.Sign(currentTime - desiredTime) != Math.Sign(currentTime - deltaTime - desiredTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool CrossedThresholdNormalized(float currentTime, float duration, float normalizedValue, float deltaTime) {
            return CrossedThreshold(currentTime, duration * normalizedValue, deltaTime);
        }
    }
}