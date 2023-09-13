using BeauRoutine;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;

namespace Zavala.Cards
{
    public class PolicySlot : MonoBehaviour
    {
        [SerializeField] private PolicyType m_Type;
        [SerializeField] private Button m_Button;

        [SerializeField] private Image m_Image;
        [SerializeField] private Graphic m_SlotBackground;
        [SerializeField] private Sprite m_LockedSprite;
        [SerializeField] private Sprite m_UnlockedSprite;

        private Color32 m_LockedColor;
        private Color32 m_UnlockedColor;
        private Color32 m_SlotColor;

        private Routine m_ChoiceRoutine;
        private List<CardUI> m_DisplayCards;

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
            policyState.PolicySlotClicked.AddListener(HandleSlotClicked);
            policyState.PolicyCardSelected.AddListener(HandlePolicyCardSelected);
            policyState.PolicyCloseButtonClicked.AddListener(HandlePolicyCloseClicked);

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked.AddListener(HandleAdvisorButtonClicked);

            m_HandState = HandState.Hidden;
        }

        public void PopulateSlot(PolicyType newType) {
            m_Type = newType;

            // If this slot type is unlocked, enable button / disable if not
            CardsState state = Game.SharedState.Get<CardsState>();
            bool slotUnlocked = CardsUtility.GetUnlockedOptions(state, m_Type).Count > 0;
            if (slotUnlocked) {
                m_Image.sprite = m_UnlockedSprite;
                m_Image.color = m_UnlockedColor;
                m_SlotBackground.color = m_SlotColor;
                m_Button.enabled = true;
            }
            else {
                m_Image.sprite = m_LockedSprite;
                m_Image.color = m_LockedColor;
                m_SlotBackground.color = m_UnlockedColor; // same color is used for locked background as locked foreground
                m_Button.enabled = false;
            }

            // TODO: If a policy is already selected, load it
        }

        public void SetColors(Color slotColor, Color lockedColor, Color unlockedColor) {
            m_SlotColor = slotColor;
            m_LockedColor = lockedColor;
            m_UnlockedColor = unlockedColor;
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
                // For each card, allocate a card from the pool
                for (int i = 0; i < cardData.Count; i++) {
                    CardData data = cardData[i];
                    CardUI card = pools.Cards.Alloc(this.transform.parent != null ? this.transform.parent : this.transform);
                    card.transform.localPosition = this.transform.localPosition;
                    card.PolicyIndex = (int)data.PolicyLevel;
                    m_DisplayCards.Add(card);

                    card.Button.onClick.AddListener(() => { policyState.PolicyCardSelected?.Invoke(data); });
                }

                m_ChoiceRoutine.Replace(ShowHandRoutine());
            }
            else if (m_HandState == HandState.Visible || m_HandState == HandState.Showing) {
                // Hide hand
                m_ChoiceRoutine.Replace(HideHandRoutine());
            }
        }

        private void HandlePolicyCardSelected(CardData data) {
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

        #endregion // Handlers

        #region Routines

        private IEnumerator ShowHandRoutine() {
            m_HandState = HandState.Showing;

            float offset = 0.5f;
            float leftMost = 55 * (m_DisplayCards.Count - 1);
            float rotatedMost = 7.5f * (m_DisplayCards.Count - 1);
            float topMost = 155;
            for (int i = 0; i < m_DisplayCards.Count; i++) {
                Transform cardTransform = m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                        cardTransform.MoveTo(new Vector3(
                            cardTransform.position.x - leftMost + 110 * i,
                            cardTransform.position.y + topMost - 22 * Mathf.Abs((i + offset) - (m_DisplayCards.Count / 2.0f)),
                            cardTransform.position.z
                        ), .3f, Axis.XY),
                        cardTransform.RotateTo(cardTransform.rotation.z + rotatedMost - 15f * i, .3f, Axis.Z)
                    )
                ).OnComplete(() => { m_HandState = HandState.Visible; });
            }

            yield return null;
        }


        private IEnumerator HideHandRoutine() {
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