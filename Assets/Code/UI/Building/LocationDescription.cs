using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI.Info {
    [RequireComponent(typeof(OccupiesTile)), DisallowMultipleComponent]
    public class LocationDescription : MonoBehaviour {
        public TextId TitleLabel;
        public TextId InfoLabel;
        public StringHash32 CharacterId;
        public Sprite Icon;

        [NonSerialized] public OccupiesTile Position;

        private void Awake() {
            Position = GetComponent<OccupiesTile>();
        }
    }
}