using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio playback properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AudioPropertyBlock {
        public float Volume;
        public float Pitch;
        public float Pan;
        public bool Pause;
        public bool Mute;

        /// <summary>
        /// Returns if these parameters result in an audible waveform.
        /// </summary>
        public bool IsAudible() {
            return Volume > 0 && !Mathf.Approximately(Pitch, 0) && !Mute && !Pause;
        }

        /// <summary>
        /// Resets to the default block.
        /// </summary>
        public void Reset() {
            this = s_Default;
        }

        #region Combinations

        /// <summary>
        /// Combines two property blocks into one.
        /// </summary>
        static public void Combine(in AudioPropertyBlock sourceA, in AudioPropertyBlock sourceB, ref AudioPropertyBlock target) {
            target.Volume = sourceA.Volume * sourceB.Volume;
            target.Pitch = sourceA.Pitch * sourceB.Pitch;
            target.Pan = sourceA.Pan + sourceB.Pan;
            target.Pause = sourceA.Pause || sourceB.Pause;
            target.Mute = sourceA.Mute || sourceB.Mute;
        }

        /// <summary>
        /// Modifies the given property block 
        /// </summary>
        static public void Mix(ref AudioPropertyBlock target, float mixFactor) {
            if (mixFactor <= 0) {
                target = s_Default;
            } else if (mixFactor < 1) {
                target.Volume = MixVal(target.Volume, mixFactor);
                target.Pitch = MixVal(target.Pitch, mixFactor);
                target.Pan *= mixFactor;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float MixVal(float val, float t) {
            return 1 + (val - 1) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float MixVal(float defaultVal, float val, float t) {
            return defaultVal + (val - defaultVal) * t;
        }

        #endregion // Combinations

        #region Defaults

        static private readonly AudioPropertyBlock s_Default = new AudioPropertyBlock() {
            Volume = 1,
            Pitch = 1,
            Pause = false,
            Mute = false
        };

        /// <summary>
        /// Default property block.
        /// </summary>
        static public AudioPropertyBlock Default { get { return s_Default; } }

        #endregion // Defaults
    }
}