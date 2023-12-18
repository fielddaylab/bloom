using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI.Info {
    [DisallowMultipleComponent]
    public class LocationDescription : MonoBehaviour {
        public TextId TitleLabel;           // main header
        public TextId InfoLabel;            // sub header
        public TextId DescriptionLabel;     // text within description box
        [NonSerialized] public StringHash32 CharacterId;
        public Sprite Icon;
    }
}