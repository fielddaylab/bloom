using System;
using BeauRoutine;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Tutorial {
    public class TutorialPanelConfigurer : MonoBehaviour {
        public GameObject HexLayout;
        public GameObject UILayout;
        public LocText Label;

        public Graphic[] Hexes;
        public RectTransform[] Anchors;
        public Graphic[] Lines;

        [Header("Colors")]
        public Color GrassColor = Color.white;
        public Color WaterColor = Color.white;
        public Color DeepColor = Color.white;
        public Color InvalidColor = Color.white;

        public void Configure(TutorialLayout config) {
            Assert.NotNull(config);
            Assert.True(Hexes.Length == config.Hexes.Length);

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

            for(int i = 0; i < config.Connections.Length; i++) {
                var pair = config.Connections[i];
                Graphic line = Lines[i];

                line.gameObject.SetActive(true);

                Vector3 posA = Hexes[pair.A].rectTransform.localPosition;
                Vector3 posB = Hexes[pair.B].rectTransform.localPosition;
                line.rectTransform.localPosition = posA;
                line.rectTransform.SetSizeDelta(Vector3.Distance(posA, posB), Axis.X);
                line.rectTransform.SetRotation(Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);
            }

            for(int i = config.Connections.Length; i < Lines.Length; i++) {
                Lines[i].gameObject.SetActive(false);
            }

            for(; anchorsUsed < Anchors.Length; anchorsUsed++) {
                Anchors[anchorsUsed].gameObject.SetActive(false);
            }
        }
    }
}