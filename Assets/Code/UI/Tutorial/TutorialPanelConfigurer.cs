using System;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Tutorial {
    public class TutorialPanelConfigurer : MonoBehaviour {
        public Graphic[] Hexes;
        public RectTransform[] Anchors;
        public Animator Animator;

        [Header("Colors")]
        public Color GrassColor = Color.white;
        public Color WaterColor = Color.white;
        public Color DeepColor = Color.white;
        public Color InvalidColor = Color.white;

        [NonSerialized] public string AnimatorState;

        public void Configure(TutorialConfig config) {
            Assert.NotNull(config);
            Assert.True(Hexes.Length == config.Hexes.Length);

            AnimatorState = config.AnimationName;
            if (Animator.isActiveAndEnabled) {
                Animator.Play(config.AnimationName);
            }

            int anchorsUsed = 0;
            for(int i = 0; i < config.Hexes.Length; i++) {
                Graphic hex = Hexes[i];
                var hexType = config.Hexes[i];
                hex.enabled = hexType != TutorialHexType.Hidden;
                switch (hexType) {
                    case TutorialHexType.Grass: {
                        hex.color = GrassColor;
                        break;
                    }
                    case TutorialHexType.Water: {
                        hex.color = WaterColor;
                        break;
                    }
                    case TutorialHexType.DeepWater: {
                        hex.color = DeepColor;
                        break;
                    }
                    case TutorialHexType.Invalid: {
                        hex.color = InvalidColor;
                        break;
                    }
                    case TutorialHexType.Anchor: {
                        hex.color = GrassColor;
                        if (anchorsUsed < Anchors.Length) {
                            Transform anchor = Anchors[anchorsUsed++];
                            anchor.gameObject.SetActive(true);
                            anchor.localPosition = hex.rectTransform.localPosition;
                        }
                        break;
                    }
                }
            }

            for(; anchorsUsed < Anchors.Length; anchorsUsed++) {
                Anchors[anchorsUsed].gameObject.SetActive(false);
            }
        }
    }
}