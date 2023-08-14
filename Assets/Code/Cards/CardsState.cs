using FieldDay.SharedState;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Advisor;
using System;

namespace Zavala.Cards
{
    public struct CardData
    {
        public string CardID;
        public string Header;
        public PolicyType PolicyType;
        public PolicyLevel PolicyLevel;

        public CardData(string cardID, string header, PolicyType policyType, PolicyLevel level) {
            CardID = cardID;
            Header = header;
            PolicyType = policyType;
            PolicyLevel = level;
        }
    }

    /// <summary>
    /// A generic policy level. Based on the assumption that any policy will have None, Low, High, and an Alt category (Subsidy, Dredge, etc.)
    /// </summary>
    public enum PolicyLevel
    {
        None,
        Low,
        High,
        Alt // Subsidy, Dredge, Etc (determined with level + type)
    }

    [SharedStateInitOrder(10)]
    public sealed class CardsState : SharedStateComponent, IRegistrationCallbacks
    {
        public TextAsset CardDefs;

        [HideInInspector] public Dictionary<string, CardData> AllCards;
        [HideInInspector] public List<string> UnlockedCards;

        #region Card Mappings

        [HideInInspector] public Dictionary<PolicyType, List<string>> CardMap; // maps slot type to list of relevant cards

        #endregion // Card Mappings

        public void OnDeregister() {
        }

        public void OnRegister() {
            // Initialize Lists
            AllCards = new Dictionary<string, CardData>();
            UnlockedCards = new List<string>();

            CardMap = new Dictionary<PolicyType, List<string>>();

            foreach (PolicyType pType in Enum.GetValues(typeof(PolicyType))) {
                CardMap.Add(pType, new List<string>());
            }

            // Populate Card data
            CardsUtility.PopulateCards(this);
        }
    }

    static public class CardsUtility
    {

        #region Card Definition Parsing

        private static string HEADER_TAG = "@header";
        private static string POLICY_LEVEL_TAG = "@level";
        private static string POLICY_TYPE_TAG = "@policytype";

        private static string END_DELIM = "\n";

        #endregion // Card Definition Parsing

        static public void PopulateCards(CardsState cardsState) {
            List<string> cardStrings = TextIO.TextAssetToList(cardsState.CardDefs, "::");

            foreach (string str in cardStrings) {
                try {
                    CardData newCard = ConvertDefToCard(str);

                    cardsState.AllCards.Add(newCard.CardID, newCard);

                    // add card to list of cards that should appear for the given sim id (queried when slot is selected)
                    List<string> relevant = cardsState.CardMap[newCard.PolicyType];
                    relevant.Add(newCard.CardID);
                    cardsState.CardMap[newCard.PolicyType] = relevant;

                    Debug.Log("[CardUtility] added " + newCard.CardID + " to the options for policy type " + newCard.PolicyType.ToString());
                }
                catch (Exception e) {
                    Debug.Log("[CardUtility] Parsing error! " + e.Message);
                }
            }
        }

        static private CardData ConvertDefToCard(string cardDef) {
            Debug.Log("[CardUtility] converting card: " + cardDef);
            string cardID = "";
            string header = "";
            PolicyLevel level = PolicyLevel.None;
            PolicyType policyType = PolicyType.RunoffPolicy;

            // Parse into data

            // First line must be card id
            cardID = cardDef.Substring(0, cardDef.IndexOf(END_DELIM));
            Debug.Log("[CardUtility] parsed card id : " + cardID);

            // Header comes after @Header
            int headerIndex = cardDef.ToLower().IndexOf(HEADER_TAG);
            if (headerIndex != -1) {
                string afterHeader = cardDef.Substring(headerIndex);
                int offset = HEADER_TAG.Length;
                header = cardDef.Substring(headerIndex + offset, afterHeader.IndexOf(END_DELIM) - offset).Trim();
            }
            else {
                // syntax error
                Debug.Log("[CardUtility] header syntax error!");

                throw new Exception("Header");
            }

            // PolicyLevel comes after @PolicyLevel
            int levelIndex = cardDef.ToLower().IndexOf(POLICY_LEVEL_TAG);
            if (levelIndex != -1) {
                string afterLevel = cardDef.Substring(levelIndex);
                int offset = POLICY_LEVEL_TAG.Length;
                string levelStr = cardDef.Substring(levelIndex + offset, afterLevel.IndexOf(END_DELIM) - offset).Trim();
                try {
                    level = (PolicyLevel)Enum.Parse(typeof(PolicyLevel), levelStr, true);
                }
                catch {
                    Debug.Log("[CardUtility] Unrecognized policy level when loading. Defaulting to 'Alt'.");
                    level = PolicyLevel.Alt;
                }
            }
            else {
                // syntax error
                Debug.Log("[CardUtility] policy level syntax error!");

                throw new Exception("Policy Level");
            }

            // PolicyType comes after @SimId
            int simIDIndex = cardDef.ToLower().IndexOf(POLICY_TYPE_TAG);

            if (simIDIndex != -1) {
                string afterSimID = cardDef.Substring(simIDIndex);
                int offset = POLICY_TYPE_TAG.Length;
                string simIDStr = cardDef.Substring(simIDIndex + offset, afterSimID.IndexOf(END_DELIM) - offset).Trim();

                policyType = (PolicyType)Enum.Parse(typeof(PolicyType), simIDStr, true);
            }
            else {
                // syntax error
                Debug.Log("[CardUtility] sim lever syntax error!");

                throw new Exception("Sim Lever");
            }

            return new CardData(cardID, header, policyType, level);
        }


        /*
        public List<CardData> GetUnlockedOptions(PolicyType slotType) {
            List<CardData> allOptions = new List<CardData>();

            List<string> cardIDs = m_cardMap[slotType];

            foreach (string cardID in cardIDs) {
                if (m_unlockedCards.Contains(cardID)) {
                    allOptions.Add(m_allCards[cardID]);
                }
            }

            return allOptions;
        }

        public List<CardData> GetAllOptions(PolicyType slotType) {
            List<CardData> allOptions = new List<CardData>();

            List<string> cardIDs = m_cardMap[slotType];

            foreach (string cardID in cardIDs) {
                allOptions.Add(m_allCards[cardID]);
            }

            return allOptions;
        }

        public CardData GetCardData(string cardID) {
            return m_allCards[cardID];
        }
        */

        /*
        private void HandleChoiceUnlock(object sender, ChoiceUnlockEventArgs args) {
            for (int i = 0; i < args.ToUnlock.Count; i++) {
                if (m_unlockedCards.Contains(args.ToUnlock[i])) {
                    continue;
                }
                m_unlockedCards.Add(args.ToUnlock[i]);
            }
        }
        */
    }
}
