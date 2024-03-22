using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.Scripting {

    [CreateAssetMenu(menuName = "Zavala/Scripting/Character Definition")]
    public class ScriptCharacterDef : ScriptableObject {
        #region Inspector

        [Header("Name")]
        public string NameId;
        public string TitleId;
        public bool IsAdvisor;
        public bool IsEcon;

        [Header("Color Schemes")]
        public Color32 BackgroundColor;
        public Color32 PanelColor;
        public Color32 HighlightColor;
        public Color32 NameColor;
        public Color32 TitleColor;
        public Color32 TextColor;

        [Header("Portrait")]
        public Sprite PortraitArt;
        public Texture2D PanelBackground;

        #endregion // Inspector

        [NonSerialized] public StringHash32 CachedId;
    }
}