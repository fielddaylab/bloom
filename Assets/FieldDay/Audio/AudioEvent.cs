using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio Event Type
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Event")]
    public class AudioEvent : ScriptableObject {
        public AudioSampleType SampleType;
        public AudioContainerType PlaybackType;

        public AudioClip[] Clips;
        //public string StreamingPath; // TODO: Implement

        public FloatRange Volume = new FloatRange(1);
        public FloatRange Pitch = new FloatRange(1);
        public FloatRange Delay = new FloatRange(0);
    }

    /// <summary>
    /// Type of audio resource.
    /// </summary>
    public enum AudioSampleType {
        AudioClip,
        Stream
    }

    public enum AudioContainerType {
        OneShot,
        Loop,
    }
}