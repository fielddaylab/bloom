using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    public class AudioEvent : ScriptableObject {

    }

    public enum AudioSampleType {
        AudioClip,
        Streaming,
        Resources
    }

    public enum AudioContainerType {
        OneShot,
        Loop,
        HoldRelease,
        Blend
    }
}