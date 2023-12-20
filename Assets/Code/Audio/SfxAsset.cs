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

        [Header("Positional")]
        [Range(0, 360)] public float Spread;
        [Range(0.3f, 4)] public float Range = 1;

        [NonSerialized] public RandomDeck<AudioClip> Randomizer;
    }

    public class SfxRef : AssetNameAttribute {
        public SfxRef() : base(typeof(SfxAsset), true) {
        }

        protected override string Name(UnityEngine.Object obj) {
            return obj.name.Replace("-", "/");
        }
    }
}