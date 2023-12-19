using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Tutorial {
    [CreateAssetMenu(menuName = "Zavala/Tutorial Configuration")]
    public class TutorialConfig : ScriptableObject {
        public string AnimationName;
        public TutorialHexType[] Hexes = new TutorialHexType[13];
        [AutoEnum] public TutorialContexts ActiveContext;
        [AutoEnum] public TutorialContexts IgnoreContext;
    }

    public enum TutorialHexType {
        Grass,
        Water,
        DeepWater,
        Invalid,
        Anchor,
        Hidden
    }

    [Flags]
    public enum TutorialContexts {
        Blueprints = 0x01,
        BuildRoad = 0x02,
        BuildStorage = 0x04,
        BuildDigester = 0x08,
        DestroyMode = 0x10,
        Dialogue = 0x20
    }
}