using System;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Scripting;

namespace Zavala.UI.Info {
    public class InfoPopup : SharedRoutinePanel {
        static private readonly Color GrainFarmColor = Colors.Hex("#C8E295");
        static private readonly Color DairyFarmColor = Colors.Hex("#E2CD95");
        static private readonly Color CityColor = Colors.Hex("#CDF6ED");

        #region Inspector

        [SerializeField] private RectTransformPinned m_Pin;
        [SerializeField] private RectTransform m_PinTransform;
        [SerializeField] private LayoutGroup m_Layout;
        [SerializeField] private RectTransform m_LayoutTransform;

        [Header("Header")]
        [SerializeField] private Graphic m_HeaderBG;
        [SerializeField] private TMP_Text m_HeaderLabel;
        [SerializeField] private TMP_Text m_SubheaderLabel;

        [Header("Portrait")]
        [SerializeField] private Image m_CharacterPortrait;
        [SerializeField] private GameObject m_PortraitGroup;

        [Header("Population")]
        [SerializeField] private InfoPopupPurchaser m_PurchaseContents;
        [SerializeField] private InfoPopupMarket m_MarketContents;

        #endregion // Inspector

        #region State

        [NonSerialized] private HasInfoPopup m_SelectedThing;
        [NonSerialized] private BuildingType m_Mode;
        [NonSerialized] private LocationDescription m_SelectedLocation;
        [NonSerialized] private ResourceSupplier m_SelectedSupplier;
        [NonSerialized] private ResourceRequester m_SelectedRequester;
        [NonSerialized] private ResourcePurchaser m_SelectedPurchaser;
        [NonSerialized] private bool m_ConnectionsDirty;

        private readonly RingBuffer<MarketQueryResultInfo> m_QueryResults = new RingBuffer<MarketQueryResultInfo>(16, RingBufferMode.Expand);

        static public readonly Comparison<MarketQueryResultInfo> SortResultsDescending = (x, y) => {
            return y.Profit - x.Profit;
        };

        static public readonly Comparison<MarketQueryResultInfo> SortResultsAscending = (x, y) => {
            return x.Profit - y.Profit;
        };

        #endregion // State

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            Game.Events.Register(GameEvents.MarketPrioritiesRebuilt, OnMarketPrioritiesUpdated);
            Game.SharedState.Get<RoadNetwork>().OnConnectionsReevaluated.Register(OnRoadNetworkRebuilt);
        }

        #endregion // Unity Events

        #region Load

        public void LoadTarget(HasInfoPopup thing) {
            if (m_SelectedThing == thing) {
                return;
            }

            m_SelectedThing = thing;
            m_Mode = thing.Position.Type;
            m_SelectedLocation = thing.GetComponent<LocationDescription>();
            switch (m_Mode) {
                case BuildingType.GrainFarm: {
                    m_SelectedPurchaser = null;
                    m_SelectedRequester = thing.GetComponent<ResourceRequester>();
                    m_SelectedSupplier = null;
                    m_PurchaseContents.gameObject.SetActive(false);
                    m_MarketContents.gameObject.SetActive(true);
                    m_HeaderBG.color = GrainFarmColor;
                    break;
                }

                case BuildingType.DairyFarm: {
                    m_SelectedPurchaser = null;
                    m_SelectedRequester = null;
                    m_SelectedSupplier = thing.GetComponent<ResourceSupplier>();
                    m_PurchaseContents.gameObject.SetActive(false);
                    m_MarketContents.gameObject.SetActive(true);
                    m_HeaderBG.color = DairyFarmColor;
                    break;
                }

                case BuildingType.City: {
                    m_SelectedPurchaser = thing.GetComponent<ResourcePurchaser>();
                    m_SelectedRequester = null;
                    m_SelectedSupplier = null;
                    m_MarketContents.gameObject.SetActive(false);
                    m_PurchaseContents.gameObject.SetActive(true);
                    m_HeaderBG.color = CityColor;
                    break;
                }
            }

            m_Pin.Pin(thing.transform);
            PopulateHeader();
            UpdateData(true);

            Show();
        }

        private void PopulateHeader() {
            m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
            m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.InfoLabel));

            ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
            ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, m_SelectedLocation.CharacterId);

            m_PortraitGroup.SetActive(charDef != null);
            if (charDef != null) {
                m_CharacterPortrait.sprite = charDef.PortraitArt;
            }
        }

        private void PopulateShipping() {
            int count = Math.Min(m_QueryResults.Count, 3);
            ConfigureRowsAndDividers(count);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();

            for (int i = 0; i < count; i++) {
                var results = m_QueryResults[i];
                InfoPopupMarketUtility.LoadLocationIntoRow(m_MarketContents.Locations[i], results.Requester.Position, results.Supplier.Position);
                InfoPopupMarketUtility.LoadProfitIntoRow(m_MarketContents.Locations[i], results, config, i > 0);
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void PopulatePurchasing() {
            int count = Math.Min(m_QueryResults.Count, 3);
            ConfigureRowsAndDividers(count);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();

            for(int i = 0; i < count; i++) {
                var results = m_QueryResults[i];
                InfoPopupMarketUtility.LoadLocationIntoRow(m_MarketContents.Locations[i], results.Supplier.Position, results.Requester.Position);
                InfoPopupMarketUtility.LoadCostsIntoRow(m_MarketContents.Locations[i], results, config, i > 0);
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void PopulateCity() {
            int purchaseAmount = m_SelectedPurchaser.RequestAmount.Milk;
            m_PurchaseContents.Number.SetText(purchaseAmount.ToStringLookup());

            float average = 0;
            foreach(var amt in m_SelectedPurchaser.RequestAmountHistory) {
                average += amt.Milk;
            }
            average /= m_SelectedPurchaser.RequestAmountHistory.Count;

            if (purchaseAmount > average) {
                m_PurchaseContents.Arrow.gameObject.SetActive(true);
                m_PurchaseContents.Arrow.color = InfoPopupMarketUtility.PositiveColor;
                m_PurchaseContents.Arrow.rectTransform.SetRotation(90, Axis.Z, Space.Self);
            } else if (purchaseAmount < average) {
                m_PurchaseContents.Arrow.gameObject.SetActive(true);
                m_PurchaseContents.Arrow.color = InfoPopupMarketUtility.NegativeColor;
                m_PurchaseContents.Arrow.rectTransform.SetRotation(-90, Axis.Z, Space.Self);
            } else {
                m_PurchaseContents.Arrow.gameObject.SetActive(false);
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void ConfigureRowsAndDividers(int count) {
            for (int i = 0; i < m_MarketContents.Dividers.Length; i++) {
                m_MarketContents.Dividers[i].SetActive(count > i + 1);
            }

            for (int i = 0; i < m_MarketContents.Locations.Length; i++) {
                m_MarketContents.Locations[i].gameObject.SetActive(count > i);
            }
        }

        private void UpdateData(bool forceRefresh) {
            switch (m_Mode) {
                case BuildingType.DairyFarm: {
                    if (m_ConnectionsDirty || forceRefresh) {
                        m_QueryResults.Clear();
                        MarketUtility.GatherShippingSources(m_SelectedSupplier, m_QueryResults, ResourceMask.Manure);
                    } else {
                        MarketUtility.UpdateShippingSources(m_SelectedSupplier, m_QueryResults, ResourceMask.Manure);
                    }
                    m_QueryResults.Sort(SortResultsDescending);
                    PopulateShipping();
                    break;
                }

                case BuildingType.GrainFarm: {
                    if (m_ConnectionsDirty || forceRefresh) {
                        m_QueryResults.Clear();
                        MarketUtility.GatherPurchaseSources(m_SelectedRequester, m_QueryResults, ResourceMask.Phosphorus);
                    } else {
                        MarketUtility.UpdatePurchaseSources(m_SelectedRequester, m_QueryResults);
                    }
                    m_QueryResults.Sort((a, b) => {
                        int dif = a.CostToBuyer - b.CostToBuyer;
                        if (dif == 0)
                        {
                            // tie break
                            if (b.Supplier.Position.IsExternal && a.Supplier.Position.IsExternal)
                            {
                                // no additional tiebreaker (random)
                                return 0;
                            }
                            else if (b.Supplier.Position.IsExternal)
                            {
                                // favor a
                                return -1;
                            }
                            else if (a.Supplier.Position.IsExternal)
                            {
                                // favor b
                                return 1;
                            }
                            else
                            {
                                // favor closest option
                                return b.Distance - a.Distance;
                            }
                        }
                        else
                        {
                            return dif;
                        }
                    });
                    PopulatePurchasing();
                    break;
                }

                case BuildingType.City: {
                    PopulateCity();
                    break;
                }
            }

            m_ConnectionsDirty = false;
        }

        #endregion // Load

        #region Handlers

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            m_SelectedThing = null;
            m_Mode = BuildingType.None;
            m_SelectedPurchaser = null;
            m_SelectedRequester = null;
            m_SelectedSupplier = null;
            m_ConnectionsDirty = false;
            m_SelectedLocation = null;
            m_QueryResults.Clear();
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            m_Pin.Unpin();
        }

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);

            m_Layout.ForceRebuild(true);
            m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
        }

        private void OnMarketPrioritiesUpdated() {
            UpdateData(false);
        }

        private void OnRoadNetworkRebuilt() {
            m_ConnectionsDirty = true;
        }

        #endregion // Handlers
    }
}