using System;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace Zavala.Audio {
    [CreateAssetMenu(menuName = "Zavala/Sfx Asset")]
    public class SfxAsset : ScriptableObject {
        public AudioClip[] Clips;
        public FloatRange Volume = new FloatRange(1);
        public FloatRange Pitch = new FloatRange(1);
        public FloatRange Delay = new FloatRange(0);
        public SerializedHash32 Tag;

        [NonSerialized] public RandomDeck<AudioClip> Randomizer;
    }
}