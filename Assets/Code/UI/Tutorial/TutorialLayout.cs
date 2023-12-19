using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Tutorial {
    [CreateAssetMenu(menuName = "Zavala/Tutorial Layout")]
    public class TutorialLayout : ScriptableObject {
        [Serializable]
        public struct ConnectionPair {
            public ushort A;
            public ushort B;
        }

        public TutorialHexType[] Hexes = new TutorialHexType[13];
        public ConnectionPair[] Connections = new ConnectionPair[0];
    }

    public enum TutorialHexType {
        Grass,
        Water,
        DeepWater,
        Invalid,
        Anchor,
        Hidden
    }
}