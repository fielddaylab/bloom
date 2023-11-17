using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    [RequireComponent(typeof(AudioSource))]
    internal class InternalAudioDataCallback : MonoBehaviour {
        [Flags]
        internal enum CallbackTypes : byte {
            OutputData = 0x01,
            SpectrumData = 0x02
        }

        internal CallbackTypes Callbacks;
    }

    public delegate void AudioOutputPCMCallback(AudioHandle handle, UnsafeSpan<byte> data, int channel, object context);
}