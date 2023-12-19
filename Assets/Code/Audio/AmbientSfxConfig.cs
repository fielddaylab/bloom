using BeauRoutine.Extensions;
using UnityEngine;

namespace Zavala.Audio {
    [CreateAssetMenu(menuName = "Zavala/Ambient Sfx Config")]
    public class AmbientSfxConfig : ScriptableObject {
        [Header("Background Loop")]
        public AudioClip Loop;
        [Range(0, 1)] public float LoopVolume = 1;
        [Range(0.001f, 3)] public float LoopPitch = 1;
        
        [Header("Random Sounds")]
        public SfxAsset[] BirdSounds;
        public FloatRange Delay = new FloatRange(15);
        [Range(0, 1)] public float DelayShortChance = 0.1f;
        public FloatRange DelayShort = new FloatRange(5);
    }
}