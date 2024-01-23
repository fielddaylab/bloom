using BeauRoutine;
using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Audio;
using Zavala.Input;
using Zavala.Sim;

namespace Zavala.Cards
{
    public class PolicySlot : MonoBehaviour
    {
        [SerializeField] private PolicyType m_Type;
        [SerializeField] private Button m_Button;
        [SerializeField] private TMP_Text m_Text;

        [SerializeField] private Image m_OverlayImage; // The locked/open slot image
        [SerializeField] private Graphic m_SlotBackground;
        [SerializeField] private Sprite m_BlankSprite;
        [SerializeField] private Sprite m_LockedSprite;
        [SerializeField] private Sprite m_UnlockedSprite;

        [SerializeField] private TMP_Text m_HintText;

        private Color32 m_LockedColor;
        private Color32 m_UnlockedColor;
        private Color32 m_SlotColor;

        private Routine m_ChoiceRoutine;
        private List<CardUI> m_DisplayCards;

        private CardData m_LastKnownCard;

        private enum HandState {
            Hidden,
            Showing,
            Visible,
            Hiding
        }

        private HandState m_HandState;

        // TODO: collapse menus when background clicked
        // TODO: propagate menu collapse through children

        private void Start() {
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            m_Button.onClick.AddListener(() => { policyState.PolicySlotClicked?.Invoke(m_Type); });
            policyState.PolicySlotClicked.Register(HandleSlotClicked);
            policyState.PolicyCardSelected.Register(HandlePolicyCardSelected);
            policyState.PolicyCloseButtonClicked.Register(HandlePolicyCloseClicked);

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked.Register(HandleAdvisorButtonClicked);

            m_HandState = HandState.Hidden;
        }

        public void PopulateSlot(PolicyType newType) {
            m_LastKnownCard = new CardData();
            m_LastKnownCard.IsValid = false;

            m_Type = newType;

            // If this slot type is unlocked, enable button / disable if not
            CardsState state = Game.SharedState.Get<CardsState>();
            List<CardData> unlockedCards = CardsUtility.GetUnlockedOptions(state, m_Type);
            bool slotUnlocked = unlockedCards.Count > 0;
            if (slotUnlocked) {
                m_OverlayImage.enabled = true; // set to open slot image
                m_OverlayImage.sprite = m_UnlockedSprite;
                m_OverlayImage.color = m_LockedColor; // 
                m_SlotBackground.color = m_UnlockedColor;
                m_Button.enabled = true;
                m_Text.SetText(Loc.Find("cards." + m_Type.ToString() + ".category"));
            }
            else {
                m_OverlayImage.enabled = true; // set to locked slot image
                m_OverlayImage.sprite = m_LockedSprite;
                m_OverlayImage.color = m_SlotColor;
                m_SlotBackground.color = m_LockedColor; // same color is used for locked background as locked foreground
                m_Button.enabled = false;
                m_Text.SetText("");
            }

            m_HintText.gameObject.SetActive(false);


            // TODO: if we add possibility for no policy to be selected, implement check here

            if (slotUnlocked) {
                bool currentExists = false;
                // load current policy
                PolicyState policyState = Game.SharedState.Get<PolicyState>();
                SimGridState grid = Game.SharedState.Get<SimGridState>();
                PolicyLevel level = policyState.Policies[grid.CurrRegionIndex].Map[(int) newType];
                if (policyState.Policies[grid.CurrRegionIndex].EverSet[(int) newType]) {
                    // Has been set before -- look for current policy
                    for (int i = 0; i < unlockedCards.Count; i++) {
                        if (unlockedCards[i].PolicyLevel == level) {
                            // found current policy
                            MirrorSelectedCard(unlockedCards[i]);
                            currentExists = true;
                            break;
                        }
                    }
                }

                if (currentExists) {
                    // the overlay image acts as both the lock/unlock and the policy image, no need to change it here
                    //m_OverlayImage.enabled = false;
                }
            }
        }

        public void SetColors(bool isEcon, Color panelColor) {
            CardsState cardState = Game.SharedState.Get<CardsState>();
            if (isEcon) {
                // econ advisor
                m_LockedColor = cardState.EconLockedSlot;
                m_UnlockedColor = cardState.EconUnlockedSlot;
                m_SlotColor = panelColor;
            } else {
                // ecol advisor
                m_LockedColor = cardState.EcolLockedSlot;
                m_UnlockedColor = cardState.EcolUnlockedSlot;
                m_SlotColor = panelColor;
            }
        }

        public void InstantHideHand() {
            if (m_HandState == HandState.Hidden) {
                return;
            }

            m_HandState = HandState.Hiding;

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                m_DisplayCards[i].transform.position = this.transform.position;
                m_DisplayCards[i].transform.SetRotation(0, Axis.Z);
            }
            m_HandState = HandState.Hidden;

            CardPools pools = Game.SharedState.Get<CardPools>();

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                // free the card back to the pool
                pools.Cards.Free(m_DisplayCards[i]);
            }
            m_DisplayCards.Clear();
 
        }

        #region Handlers

        private void HandleSlotClicked(PolicyType policyType) {
            if (policyType != m_Type) {
                // Clicked a different slot
                if (m_HandState == HandState.Visible || m_HandState == HandState.Showing) {
                    // Hide this hand in deference to other hand
                    m_ChoiceRoutine.Replace(HideHandRoutine());
                }
                return;
            }

            if (m_HandState == HandState.Hidden || m_HandState == HandState.Hiding) {
                // Show hand
                CardsState cardState = Game.SharedState.Get<CardsState>();
                CardPools pools = Game.SharedState.Get<CardPools>();
                PolicyState policyState = Game.SharedState.Get<PolicyState>();

                List<CardData> cardData = CardsUtility.GetUnlockedOptions(cardState, m_Type);
                if (m_DisplayCards == null) {
                    m_DisplayCards = new List<CardUI>();
                }
                else {
                    m_DisplayCards.Clear();
                }

                SfxUtility.PlaySfx("advisor-policy-fan");

                // For each card, allocate a card from the pool
                for (int i = 0; i < cardData.Count; i++) {
                    CardData data = cardData[i];
                    CardUI card = pools.Cards.Alloc(this.transform.parent != null ? this.transform.parent : this.transform);
                    CardUIUtility.PopulateCard(card, data, cardState.Sprites, m_UnlockedColor);

                    card.transform.localPosition = this.transform.localPosition;
                    card.transform.SetAsFirstSibling();
                    m_DisplayCards.Add(card);
                    card.Button.onClick.AddListener(() => { OnCardClicked(policyState, data, card); });
                    card.ClearHoverListeners();
                    card.OnCardHover += OnCardHoverStart;
                    card.OnCardHoverExit += OnCardHoverExit;
                }

                m_ChoiceRoutine.Replace(ShowHandRoutine());
            }
            else if (m_HandState == HandState.Visible || m_HandState == HandState.Showing) {
                // Hide hand
                m_ChoiceRoutine.Replace(HideHandRoutine());
            }
        }

        private void HandlePolicyCardSelected(CardData data) {
            if (m_Type == data.PolicyType) {
                SfxUtility.PlaySfx("advisor-policy-select");
                MirrorSelectedCard(data);
            }

            // Hide Hand
            m_ChoiceRoutine.Replace(HideHandRoutine());
        }

        private void HandleAdvisorButtonClicked(AdvisorType advisorType) {
            if (gameObject.activeInHierarchy) {
                // Hide Hand
                m_ChoiceRoutine.Replace(HideHandRoutine());
            }
        }

        private void HandlePolicyCloseClicked() {
            // Hide Hand
            m_ChoiceRoutine.Replace(HideHandRoutine());
        }

        private void OnCardClicked(PolicyState policyState, CardData data, CardUI card) {
            policyState.PolicyCardSelected?.Invoke(data);
            // TODO: hiding/click into place animation
        }

        #endregion // Handlers

        private void MirrorSelectedCard(CardData data) {
            // Set this image and text to selected card's text and image
            CardUIUtility.ExtractSprite(data, Game.SharedState.Get<CardsState>().Sprites, out Sprite sprite);
            CardUIUtility.ExtractLocText(data, out string locText);
            // TODO: extract font effects
            m_OverlayImage.sprite = sprite;
            m_OverlayImage.color = Color.white;
            m_Text.SetText(locText);
            m_OverlayImage.enabled = true;
            m_LastKnownCard = data;
        }

        private void OnCardHoverStart(object sender, CardEventArgs args)
        {
            m_HintText.gameObject.SetActive(true);
            m_OverlayImage.sprite = m_BlankSprite;
            m_OverlayImage.color = m_LockedColor;

            CardUIUtility.ExtractLocHintText(args.Data, out string locText);
            m_HintText.SetText(locText);

            CardUIUtility.ExtractLocText(args.Data, out locText);
            m_Text.SetText(locText);
        }

        private void OnCardHoverExit(object sender, EventArgs args)
        {
            m_HintText.gameObject.SetActive(false);
            m_OverlayImage.sprite = m_UnlockedSprite;

            m_HintText.SetText("");
            m_Text.SetText(Loc.Find("cards." + m_Type.ToString() + ".category"));

            if (m_LastKnownCard.IsValid)
            {
                MirrorSelectedCard(m_LastKnownCard);
            }
        }

        #region Routines

        private IEnumerator ShowHandRoutine() {
            m_HandState = HandState.Showing;

            float offset = 0.5f;
            float leftMost = 60 * (m_DisplayCards.Count - 1);
            float rotatedMost = 8.0f * (m_DisplayCards.Count - 1);
            float topMost = 155;
            for (int i = 0; i < m_DisplayCards.Count; i++) {
                RectTransform cardTransform = (RectTransform) m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                        cardTransform.AnchorPosTo(new Vector2(
                            cardTransform.anchoredPosition.x - leftMost + 120 * i,
                            cardTransform.anchoredPosition.y + topMost - 30 * Mathf.Abs((i + offset) - (m_DisplayCards.Count / 2.0f))
                        ), .3f, Axis.XY),
                        cardTransform.RotateTo(cardTransform.rotation.z + rotatedMost - 15f * i, .3f, Axis.Z)
                    )
                ).OnComplete(() => { m_HandState = HandState.Visible; });
            }

            yield return null;
        }


        private IEnumerator HideHandRoutine() {
            m_HintText.gameObject.SetActive(false);
            m_HintText.SetText("");
            
            // TODO: Hide hint text
            if (m_HandState == HandState.Hidden) {
                yield break;
            }

            m_HandState = HandState.Hiding;

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                Transform cardTransform = m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                    cardTransform.MoveTo(this.transform.position, .3f, Axis.XY),
                    cardTransform.RotateTo(0, .3f, Axis.Z)
                    )
                ).OnComplete(() => { m_HandState = HandState.Hidden; });
            }

            while (m_HandState != HandState.Hidden) {
                yield return null;
            }

            CardPools pools = Game.SharedState.Get<CardPools>();

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                // free the card back to the pool
                pools.Cards.Free(m_DisplayCards[i]);
            }
            m_DisplayCards.Clear();
        }

        #endregion // Routines
    }
}