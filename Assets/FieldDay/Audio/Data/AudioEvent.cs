using System;
using BeauRoutine.Extensions;
using BeauUtil;
using EasyAssetStreaming;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio Event information.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Event")]
    public class AudioEvent : ScriptableObject {
        public AudioVoiceType VoiceType;
        public ushort BusIndex;

        public AudioClip[] Samples = Array.Empty<AudioClip>();
        [StreamingAudioPath] public string StreamingPath;

        public FloatRange Volume = new FloatRange(1);
        public FloatRange Pitch = new FloatRange(1);
        public FloatRange Pan = new FloatRange(0);
        public FloatRange Delay = new FloatRange(0);
        public bool Loop;
        public bool RandomizeStartLocation;

        [Range(0, 1)]
        public byte Priority = 128;

        public AudioEmitterConfig Spatial = AudioEmitterConfig.Default2D;

        [NonSerialized] public string OverrideStreaming;
        [NonSerialized] internal StringHash32 CachedId;
        [NonSerialized] internal RandomDeck<AudioClip> SampleSelector;
        
        /// <summary>
        /// Returns if this is a valid event.
        /// </summary>
        public bool IsValid() {
            if (BusIndex >= AudioMgr.MaxBuses) {
                return false;
            }

            if (VoiceType == AudioVoiceType.Stream) {
                return !string.IsNullOrEmpty(ResolveStreamingPath());
            } else {
                return Samples.Length > 0;
            }
        }

        public string ResolveStreamingPath() {
            return !string.IsNullOrEmpty(OverrideStreaming) ? OverrideStreaming : StreamingPath;
        }
    }
}