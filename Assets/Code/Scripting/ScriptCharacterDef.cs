using UnityEngine;

namespace Zavala.Scripting {

    [CreateAssetMenu(menuName = "Zavala/Scripting/Character Definition")]
    public class ScriptCharacterDef : ScriptableObject {
        #region Inspector

        [Header("Name")]
        public string NameId;
        public string TitleId;

        [Header("Color Schemes")]
        public Color32 BackgroundColor;
        public Color32 NameColor;
        public Color32 TitleColor;
        public Color32 TextColor;

        [Header("Portrait")]
        public Sprite PortraitArt;
        public Sprite PortraitBackground;

        #endregion // Inspector
    }
}