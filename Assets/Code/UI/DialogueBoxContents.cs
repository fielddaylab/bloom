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
        public TMP_Text NextButtonText;

        [Header("Portrait")]
        public DialoguePortrait PortraitBox;
        public Graphic PortraitColorLayer;
        public RawImage PanelBackground;
        public Image PortraitImage;

        #endregion // Inspector
    }

    static public class DialogueUIUtility {
        static public void PopulateBoxText(DialogueBoxContents box, Graphic buttonGraphic, string header, string subheader, string content, Texture2D panelBG, Sprite portraitImg, bool showPortraitDetail, Color boxColor, Color highlightColor, Color nameColor, Color titleColor, Color textColor) {
            // text
            box.Header.TryPopulate(header);
            box.Subheader.TryPopulate(subheader);
            box.Contents.TryPopulate(content);

            // portrait
            box.PanelBackground.gameObject.SetActive(panelBG != null);
            box.PanelBackground.texture = panelBG;
            box.PortraitImage.gameObject.SetActive(portraitImg != null);
            box.PortraitImage.sprite = portraitImg;
            box.PortraitBox.ShowDetails(boxColor, showPortraitDetail);

            // Colors
            box.BoxColorLayer.color = boxColor;
            box.PortraitColorLayer.color = highlightColor;
            buttonGraphic.color = boxColor;
            box.Header.color = nameColor;
            box.Subheader.color = titleColor;
            box.Contents.color = textColor;
        }
        static public void PopulateBoxText(DialogueBoxContents box, Graphic buttonGraphic, string header, string subheader, string content, Sprite portraitImg, bool showPortraitDetail, Color boxColor, Color highlightColor, Color nameColor, Color titleColor, Color textColor) {
            // text
            box.Header.TryPopulate(header);
            box.Subheader.TryPopulate(subheader);
            box.Contents.TryPopulate(content);

            // portrait
            box.PortraitBox.ShowDetails(boxColor, showPortraitDetail);
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

        static public void PopulateBoxText(DialogueBoxContents box, string header, string subheader, string content) {
            // text
            box.Header.TryPopulate(header);
            box.Subheader.TryPopulate(subheader);
            box.Contents.TryPopulate(content);
        }

        static public void SetNextButtonText(DialogueBoxContents box, string buttonText, Color buttonColor) {
            if (!box.NextButtonText.TryPopulate(buttonText)) {
                // box.NextButtonText.SetText("Next");
            };
        }
    }
}