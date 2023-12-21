using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zavala.Audio;

namespace Zavala.Cards {

    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button Button;
        public TMP_Text Text;
        public Image CardArt;
        public Graphic Background;
        public CardData Data;

        [NonSerialized] public int PolicyIndex; // Which severity index this card corresponds to (also index from left to right)
        [NonSerialized] private Vector2 OriginalAnchorPos;

        public event EventHandler<CardEventArgs> OnCardHover;
        public event EventHandler OnCardHoverExit;

        private void OnDisable() {
            Button.onClick.RemoveAllListeners();
        }
        public void OnPointerEnter(PointerEventData eventData) {
            transform.SetAsLastSibling();
            transform.SetScale(1.1f);
            SfxUtility.PlaySfx("advisor-policy-hover");
            // set text to policy slot
            OnCardHover?.Invoke(this, new CardEventArgs(this.Data));
        }
        public void OnPointerExit(PointerEventData eventData) {
            transform.SetScale(1);
            // remove text from policy slot
            OnCardHoverExit?.Invoke(this, EventArgs.Empty);
        }

        public void ClearHoverListeners()
        {
            OnCardHover = null;
            OnCardHoverExit = null;
        }
    }

    static public class CardUIUtility { 
        static public void PopulateCard(CardUI card, CardData data, SpriteLibrary library, Color32 bgColor) {
            ExtractLocSeverityText(data, out string locText);
            // TODO: extract font effects
            card.Text.SetText(locText);
            ExtractSprite(data, library, out Sprite sprite);
            card.CardArt.sprite = sprite;
            card.PolicyIndex = (int)data.PolicyLevel;
            card.Background.color = bgColor;
            card.Data = data;
        }

        static public void ExtractLocText(CardData data, out string locText) {
            string typeText = Loc.Find("cards." + data.PolicyType.ToString() + ".category");
            string severityText = Loc.Find("cards." + data.PolicyType.ToString() + "." + data.PolicyLevel.ToString().ToLower());
            locText = typeText + ":\n" + severityText.ToUpper();
        }

        static public void ExtractLocHintText(CardData data, out string locText)
        {
            string typeText = Loc.Find("cards." + data.PolicyType.ToString() + ".category");
            string hintText = Loc.Find("cards." + data.PolicyType.ToString() + "." + data.PolicyLevel.ToString().ToLower() + ".hint");
            locText = typeText + ":\n\n" + hintText;

        }

        static public void ExtractLocSeverityText(CardData data, out string locText) {
            string severityText = Loc.Find("cards." + data.PolicyType.ToString() + "." + data.PolicyLevel.ToString().ToLower());
            locText = severityText.ToUpper();
        }

        static public void ExtractSprite(CardData data, SpriteLibrary sprites, out Sprite sprite) {
            // find image path from card definition, load it from resources
            string pathStr = data.ImgPath;
            int extIndex = pathStr.IndexOf(".");
            if (extIndex != -1) {
                pathStr = pathStr.Substring(0, extIndex);
            }
            sprites.TryLookup(pathStr, out sprite);
        }
    }

    public class CardEventArgs : EventArgs
    {
        public CardData Data;

        public CardEventArgs(CardData data)
        {
            Data = data;
        }
    }

}
