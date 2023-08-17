using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class DialogueBoxContents : MonoBehaviour {
        #region Inspector

        [Header("Text")]
        public RectTransform TextBox;
        public TMP_Text Header;
        public TMP_Text Subheader;
        public TMP_Text Contents;

        [Header("Portrait")]
        public RectTransform PortraitBox;
        public Graphic PortraitColorLayer;
        public Image PortraitBackground;
        public Image PortraitImage;

        [Header("Settings")]
        public RectOffset BoxDefaultOffset;
        public RectOffset BoxWithPortraitOffset;

        #endregion // Inspector
    }

    static public class DialogueUIUtility {
        static public void PopulateBoxText(DialogueBoxContents box, string header, string subheader, string content) {
            box.Header.TryPopulate(header);
            box.Subheader.TryPopulate(subheader);
            box.Contents.TryPopulate(content);
        }
    }
}