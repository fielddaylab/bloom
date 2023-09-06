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
        [SerializeField] private Sprite m_LockedSprite;
        [SerializeField] private Sprite m_UnlockedSprite;

        private Routine m_ChoiceRoutine;
        private List<CardUI> m_DisplayCards;

        private bool m_HandVisible;

        // TODO: collapse menus when background clicked
        // TODO: propagate menu collapse through children

        private void Start() {
            m_Button.onClick.AddListener(HandleSlotClicked);

            m_HandVisible = false;
        }

        public void PopulateSlot(PolicyType newType) {
            m_Type = newType;

            // If this slot type is unlocked, enable button / disable if not
            CardsState state = Game.SharedState.Get<CardsState>();
            bool slotUnlocked = CardsUtility.GetUnlockedOptions(state, m_Type).Count > 0;
            if (slotUnlocked) {
                m_Image.sprite = m_LockedSprite;
                m_Button.enabled = true;
            }
            else {
                m_Image.sprite = m_UnlockedSprite;
                m_Button.enabled = false;
            }

            // TODO: If a policy is already selected, load it
        }



        #region Handlers

        private void HandleSlotClicked() {
            // TODO: pool cards, one for each option
            // Assign listeners to each?
            // Or have each dispatch an event?

            CardsState state = Game.SharedState.Get<CardsState>();
            List<CardData> cardData = CardsUtility.GetUnlockedOptions(state, m_Type);
            if (m_DisplayCards == null) {
                m_DisplayCards = new List<CardUI>();
            }
            else {
                m_DisplayCards.Clear();
            }
            // For each card, allocate a card from the pool

            m_ChoiceRoutine.Replace(ShowHandRoutine());
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator ShowHandRoutine() {
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
                );
            }
            m_HandVisible = true;

            yield return null;
        }


        private IEnumerator HideHandRoutine() {
            // m_hiding = true;

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                Transform cardTransform = m_DisplayCards[i].transform;
                Routine.Start(
                    Routine.Combine(
                    cardTransform.MoveTo(this.transform.position, .3f, Axis.XY),
                    cardTransform.RotateTo(0, .3f, Axis.Z)
                    )
                );
            }

            yield return 0.35f;

            for (int i = 0; i < m_DisplayCards.Count; i++) {
                // TODO: free the card back to the pool
            }
            m_DisplayCards.Clear();

            m_HandVisible = false;
            // m_hiding = false;
        }

        #endregion // Routines
    }
}