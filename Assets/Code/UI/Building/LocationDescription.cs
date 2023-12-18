using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI.Info {
    [DisallowMultipleComponent]
    public class LocationDescription : MonoBehaviour {
        public TextId TitleLabel;           // main header
        public TextId OverrideTitleLabel;   // main header (for when UI does not use character Title as the UI title)
        public TextId InfoLabel;            // sub header
        public TextId DescriptionLabel;     // text within description box
        [NonSerialized] public StringHash32 CharacterId;
        [NonSerialized] public StringHash32 RegionId;
        public Sprite Icon;
    }
}