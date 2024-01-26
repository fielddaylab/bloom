using FieldDay.SharedState;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Advisor;
using System;
using BeauUtil;
using Leaf.Runtime;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using Zavala.Data;

namespace Zavala.Cards
{
    public struct CardData
    {
        public SerializedHash32 CardID;
        public PolicyType PolicyType;
        public PolicyLevel PolicyLevel;
        public string ImgPath;
        public bool IsValid;

        public CardData(SerializedHash32 cardID, string header, PolicyType policyType, PolicyLevel level, string imgPath) {
            CardID = cardID;
            PolicyType = policyType;
            PolicyLevel = level;
            ImgPath = imgPath;
            IsValid = true;
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
    public sealed class CardsState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject
    {
        public TextAsset CardDefs;
        public SpriteLibrary Sprites;

        [Header("Slot Colors")]
        public Color EconLockedSlot;
        public Color EconUnlockedSlot;
        public Color EcolLockedSlot;
        public Color EcolUnlockedSlot;

        [HideInInspector] public Dictionary<StringHash32, CardData> AllCards;
        [HideInInspector] public List<StringHash32> UnlockedCards;

        #region Card Mappings

        [HideInInspector] public EMap<PolicyType, List<StringHash32>> CardMap; // maps slot type to list of relevant cards

        #endregion // Card Mappings

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Cards");
        }

        public void OnRegister() {
            // Initialize Lists
            AllCards = new Dictionary<StringHash32, CardData>();
            UnlockedCards = new List<StringHash32>();

            CardMap = new EMap<PolicyType, List<StringHash32>>(4);
            CardMap[0] = new List<StringHash32>();
            CardMap[1] = new List<StringHash32>();
            CardMap[2] = new List<StringHash32>();
            CardMap[3] = new List<StringHash32>();

            // Populate Card data
            CardsUtility.PopulateCards(this);

            ZavalaGame.SaveBuffer.RegisterHandler("Cards", this);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            int count = reader.Read<int>();

            UnlockedCards.Clear();
            ListUtils.EnsureCapacityPow2(ref UnlockedCards, count);

            for(int i = 0; i < count; i++) {
                UnlockedCards.Add(reader.Read<StringHash32>());
            }
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write(UnlockedCards.Count);

            for(int i = 0; i < UnlockedCards.Count; i++) {
                writer.Write(UnlockedCards[i]);
            }
        }
    }

    static public class CardsUtility {

        static CardsUtility() {
            AdvisorPolicyMap = new EMap<AdvisorType, PolicyType[]>(3);
            AdvisorPolicyMap[AdvisorType.Ecology] = new PolicyType[] { PolicyType.RunoffPolicy, PolicyType.SkimmingPolicy };
            AdvisorPolicyMap[AdvisorType.Economy] = new PolicyType[] { PolicyType.SalesTaxPolicy, PolicyType.ImportTaxPolicy /*, PolicyType.ExportTaxPolicy*/ };
        }

        #region Card Definition Parsing

        private static readonly string POLICY_LEVEL_TAG = "@level";
        private static readonly string POLICY_TYPE_TAG = "@policytype";
        private static readonly string IMAGE_PATH_TAG = "@path";

        private static readonly string END_DELIM = "\n";

        #endregion // Card Definition Parsing

        public static readonly EMap<AdvisorType, PolicyType[]> AdvisorPolicyMap;

        static public void PopulateCards(CardsState cardsState) {
            List<string> cardStrings = TextIO.TextAssetToList(cardsState.CardDefs, "::");

            foreach (string str in cardStrings) {
                try {
                    CardData newCard = ConvertDefToCard(str);

                    cardsState.AllCards.Add(newCard.CardID, newCard);

                    // add card to list of cards that should appear for the given sim id (queried when slot is selected)
                    List<StringHash32> relevant = cardsState.CardMap[newCard.PolicyType];
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
            PolicyLevel level = PolicyLevel.None;
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

            List<StringHash32> cardIDs = state.CardMap[slotType];

            foreach (SerializedHash32 cardID in cardIDs) {
                if (state.UnlockedCards.Contains(cardID)) {
                    allOptions.Add(state.AllCards[cardID]);
                }
            }

            return allOptions;
        }

        static public List<CardData> GetAllOptions(CardsState state, PolicyType type) {
            List<CardData> allOptions = new List<CardData>();

            List<StringHash32> cardIDs = state.CardMap[type];

            foreach (SerializedHash32 cardID in cardIDs) {
                allOptions.Add(state.AllCards[cardID]);
            }

            return allOptions;
        }

        [LeafMember("UnlockCards")]
        static public void UnlockCardsLeaf(PolicyType type) {
            CardsUtility.UnlockCardsByType(Game.SharedState.Get<CardsState>(), type);
        }

        [LeafMember("PolicyIsUnlocked")]
        static public bool PolicyIsUnlockedLeaf(PolicyType type) {
            
            return PolicyIsUnlocked(Game.SharedState.Get<CardsState>(), type);
        }

        static public bool PolicyIsUnlocked(CardsState state, PolicyType type) {
            List<StringHash32> unlockIds = state.CardMap[type];
            foreach (StringHash32 id in unlockIds) {
                if (state.UnlockedCards.Contains(id)) {
                    // if UnlockedCards contains any of the IDs of this policy, return true
                    return true;
                }
            }
            return false;
        }

        [LeafMember("NumCardsUnlocked")]
        static public int NumCardsUnlocked() {
            return Game.SharedState.Get<CardsState>().UnlockedCards.Count;
        }


        static public void UnlockCardsByType(CardsState state, PolicyType type) {

            List<StringHash32> unlockIds = state.CardMap[type];

            foreach (StringHash32 id in unlockIds) {
                if (state.UnlockedCards.Contains(id)) {
                    continue;
                }
                state.UnlockedCards.Add(id);
            }

            Game.Events.Dispatch(GameEvents.PolicyTypeUnlocked);
        }

        [DebugMenuFactory]
        static private DMInfo PolicyUnlockDebugMenu()
        {
            DMInfo info = new DMInfo("Policies");
            info.AddButton("Unlock Sales Tax", () => {
                var c = Game.SharedState.Get<CardsState>();
                CardsUtility.UnlockCardsByType(c, PolicyType.SalesTaxPolicy);
            }, () => Game.SharedState.TryGet(out CardsState c));

            info.AddButton("Unlock Import Tax", () => {
                var c = Game.SharedState.Get<CardsState>();
                CardsUtility.UnlockCardsByType(c, PolicyType.ImportTaxPolicy);
            }, () => Game.SharedState.TryGet(out CardsState c));

            info.AddButton("Unlock Runoff Penalty", () => {
                var c = Game.SharedState.Get<CardsState>();
                CardsUtility.UnlockCardsByType(c, PolicyType.RunoffPolicy);
            }, () => Game.SharedState.TryGet(out CardsState c));

            info.AddButton("Unlock Skimmers", () => {
                var c = Game.SharedState.Get<CardsState>();
                CardsUtility.UnlockCardsByType(c, PolicyType.SkimmingPolicy);
            }, () => Game.SharedState.TryGet(out CardsState c));
            return info;
        }
    }
}
