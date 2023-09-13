using BeauUtil;
using System;
using UnityEngine;

namespace Zavala.Scripting {

    [Serializable]
    public struct CharRemapData {
        public RegionId Region;
        public ScriptCharacterDef CharDef;
    }

    /// <summary>
    /// Remaps the generic character id to a local equivalent
    /// </summary>
    [CreateAssetMenu(menuName = "Zavala/Scripting/Character Remap")]
    public class ScriptCharacterRemap : ScriptableObject {
        #region Inspector

        public CharRemapData[] RemapTo;

        #endregion // Inspector

        public bool Contains(string charName) {
            for (int i = 0; i < RemapTo.Length; i++) {
                if (RemapTo[i].CharDef.name.Equals(charName)) {
                    return true;
                }
            }
            return false;
        }
    }
}