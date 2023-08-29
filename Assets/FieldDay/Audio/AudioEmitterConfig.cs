using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {

    /// <summary>
    /// Control spatialized audio emission.
    /// </summary>
    [Serializable]
    public struct AudioEmitterConfig {
        /// <summary>
        /// How this emitter is positioned.
        /// </summary>
        [AutoEnum] public AudioEmitterMode Mode;

        /// <summary>
        /// Rolloff type.
        /// </summary>
        [Tooltip("How audio will roll off over distance.")]
        [AutoEnum] public AudioRolloffMode Rolloff;

        /// <summary>
        /// Rolloff minimum distance.
        /// </summary>
        [Tooltip("Minimum rolloff distance")]
        [Range(0, 300)] public float MinDistance;

        /// <summary>
        /// Rolloff maximum distance.
        /// </summary>
        [Tooltip("Maximum rolloff distance")]
        [Range(0, 300)] public float MaxDistance;

        /// <summary>
        /// Factor by which the audio is "despatialized".
        /// </summary>
        [Tooltip("Adjusts the impact of positioning on playback.\n0 = Full 3D, 1 = Completely Flat")]
        [Range(0, 1)] public float DespatializeFactor;
    }

    /// <summary>
    /// How sounds are positioned.
    /// </summary>
    [LabeledEnum(false)]
    public enum AudioEmitterMode : byte {
        [Label("2D")]
        Flat,

        [Label("3D")]
        World,

        [Label("3D (Relative to Listener)")]
        ListenerRelative
    }
}