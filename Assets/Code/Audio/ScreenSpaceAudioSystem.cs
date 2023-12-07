using System.Runtime.InteropServices.WindowsRuntime;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Zavala.Audio {
    [RequireComponent(typeof(AudioSource))]
    public sealed class ScreenSpaceAudioSystem : BatchedComponent, IBaked {
        public Transform Source;

#if UNITY_EDITOR

        int IBaked.Order => 0;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if (!Source) {
                Source = transform.parent;
                return true;
            }

            return false;
        }

#endif // UNITY_EDITOR
    }
}