using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class DialogueBoxContents : MonoBehaviour {
        #region Inspector

        [Header("Text")]
        public TMP_Text Header;
        public TMP_Text Subheader;
        public TMP_Text Contents;
        public Graphic BoxColorLayer;

        [Header("Portrait")]
        public RectTransform PortraitBox;
        public Graphic PortraitColorLayer;
        public Image PortraitBackground;
        public Image PortraitImage;

        #endregion // Inspector
    }

    static public class DialogueUIUtility {
        static public void PopulateBoxText(DialogueBoxContents box, Graphic buttonGraphic, string header, string subheader, string content, Sprite portraitBG, Sprite portraitImg, Color boxColor, Color highlightColor, Color nameColor, Color titleColor, Color textColor) {
            // text
            box.Header.TryPopulate(header);
            box.Subheader.TryPopulate(subheader);
            box.Contents.TryPopulate(content);

            // portrait
            box.PortraitBackground.gameObject.SetActive(portraitBG != null);
            box.PortraitBackground.sprite = portraitBG;
            box.PortraitImage.gameObject.SetActive(portraitImg != null);
            box.PortraitImage.sprite = portraitImg;

            // Colors
            box.BoxColorLayer.color = boxColor;
            box.PortraitColorLayer.color = highlightColor;
            buttonGraphic.color = boxColor;
            box.Header.color = nameColor;
            box.Subheader.color = titleColor;
            box.Contents.color = textColor;
        }
    }
}