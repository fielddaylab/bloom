using FieldDay.SharedState;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Advisor;
using System;
using BeauUtil;
using Leaf.Runtime;
using System.Runtime.CompilerServices;

namespace Zavala.Cards
{
    public struct CardData
    {
        public SerializedHash32 CardID;
        public PolicyType PolicyType;
        public PolicyLevel PolicyLevel;
        public string ImgPath;

        public CardData(SerializedHash32 cardID, string header, PolicyType policyType, PolicyLevel level, string imgPath) {
            CardID = cardID;
            PolicyType = policyType;
            PolicyLevel = level;
            ImgPath = imgPath;
        }
    }

    /// <summary>
    /// A generic policy level. Based on the assumption that any policy will have None, Low, High, and an Alt category (Subsidy, Dredge, etc.)
    /// </summary>
    public enum PolicyLevel
    {
        Low,
        Med,
        High,
        Alt // Subsidy, Dredge, Etc (determined with level + type)
    }

    [SharedStateInitOrder(10)]
    public sealed class CardsState : SharedStateComponent, IRegistrationCallbacks
    {
        public TextAsset CardDefs;

        [HideInInspector] public Dictionary<SerializedHash32, CardData> AllCards;
        [HideInInspector] public List<SerializedHash32> UnlockedCards;

        #region Card Mappings

        [HideInInspector] public Dictionary<PolicyType, List<SerializedHash32>> CardMap; // maps slot type to list of relevant cards

        #endregion // Card Mappings

        public void OnDeregister() {
        }

        public void OnRegister() {
            // Initialize Lists
            AllCards = new Dictionary<SerializedHash32, CardData>();
            UnlockedCards = new List<SerializedHash32>();

            CardMap = new Dictionary<PolicyType, List<SerializedHash32>>();

            foreach (PolicyType pType in Enum.GetValues(typeof(PolicyType))) {
                CardMap.Add(pType, new List<SerializedHash32>());
            }

            // Populate Card data
            CardsUtility.PopulateCards(this);

            // TEMP Unlock initial cards
            // CardsUtility.UnlockCardsByType(this, PolicyType.RunoffPolicy);
            // CardsUtility.UnlockCardsByType(this, PolicyType.SkimmingPolicy);
            // CardsUtility.UnlockCardsByType(this, PolicyType.SalesTaxPolicy);
            // CardsUtility.UnlockCardsByType(this, PolicyType.ImportTaxPolicy);

        }
    }

    static public class CardsUtility {

        #region Card Definition Parsing

        private static string POLICY_LEVEL_TAG = "@level";
        private static string POLICY_TYPE_TAG = "@policytype";
        private static string IMAGE_PATH_TAG = "@path";

        private static string END_DELIM = "\n";

        #endregion // Card Definition Parsing

        public static Dictionary<AdvisorType, PolicyType[]> AdvisorPolicyMap = new Dictionary<AdvisorType, PolicyType[]>() {
            {
                AdvisorType.Ecology,
                new PolicyType[] { PolicyType.RunoffPolicy, PolicyType.SkimmingPolicy }
            },
            {
                AdvisorType.Economy,
                new PolicyType[] { PolicyType.SalesTaxPolicy, PolicyType.ImportTaxPolicy /*, PolicyType.ExportTaxPolicy*/ }
            }
        };

        static public void PopulateCards(CardsState cardsState) {
            List<string> cardStrings = TextIO.TextAssetToList(cardsState.CardDefs, "::");

            foreach (string str in cardStrings) {
                try {
                    CardData newCard = ConvertDefToCard(str);

                    cardsState.AllCards.Add(newCard.CardID, newCard);

                    // add card to list of cards that should appear for the given sim id (queried when slot is selected)
                    List<SerializedHash32> relevant = cardsState.CardMap[newCard.PolicyType];
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
            SerializedHash32 cardID = "";
            string header = "";
            PolicyLevel level = PolicyLevel.Low;
            PolicyType policyType = PolicyType.RunoffPolicy;
            string imgPath = "";

            // Parse into data

            // First line must be card id
            cardID = cardDef.Substring(0, cardDef.IndexOf(END_DELIM));
            Debug.Log("[CardUtility] parsed card id : " + cardID);

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


            // Image Path comes after @Path
            int imgPathIndex = cardDef.ToLower().IndexOf(IMAGE_PATH_TAG);

            if (imgPathIndex != -1) {
                string afterPathIndex = cardDef.Substring(imgPathIndex);
                int offset = IMAGE_PATH_TAG.Length;
                string imgPathStr = cardDef.Substring(imgPathIndex + offset, afterPathIndex.IndexOf(END_DELIM) - offset).Trim();

                imgPath = imgPathStr;
            }
            else {
                // syntax error
                Debug.Log("[CardUtility] image path syntax error!");

                throw new Exception("Image Path");
            }

            return new CardData(cardID, header, policyType, level, imgPath);
        }


        
        static public List<CardData> GetUnlockedOptions(CardsState state, PolicyType slotType) {
            List<CardData> allOptions = new List<CardData>();

            List<SerializedHash32> cardIDs = state.CardMap[slotType];

            foreach (SerializedHash32 cardID in cardIDs) {
                if (state.UnlockedCards.Contains(cardID)) {
                    allOptions.Add(state.AllCards[cardID]);
                }
            }

            return allOptions;
        }

        static public List<CardData> GetAllOptions(CardsState state, PolicyType type) {
            List<CardData> allOptions = new List<CardData>();

            List<SerializedHash32> cardIDs = state.CardMap[type];

            foreach (SerializedHash32 cardID in cardIDs) {
                allOptions.Add(state.AllCards[cardID]);
            }

            return allOptions;
        }

        /*
        public CardData GetCardData(SerializedHash32 cardID) {
            return m_allCards[cardID];
        }
        */

        [LeafMember("UnlockCards")]
        static public void UnlockCardsLeaf(PolicyType type) {
            CardsUtility.UnlockCardsByType(Game.SharedState.Get<CardsState>(), type);
        }


        static public void UnlockCardsByType(CardsState state, PolicyType type) {

            List<SerializedHash32> unlockIds = state.CardMap[type];

            foreach (SerializedHash32 id in unlockIds) {
                if (state.UnlockedCards.Contains(id)) {
                    continue;
                }
                state.UnlockedCards.Add(id);
            }
        }

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
