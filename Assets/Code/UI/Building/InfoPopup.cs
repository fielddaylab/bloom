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

namespace Zavala.UI.Info {
    public class InfoPopup : SharedRoutinePanel {
        #region Inspector

        [SerializeField] private RectTransformPinned m_Pin;
        [SerializeField] private LayoutGroup m_Layout;

        [Header("Header")]
        [SerializeField] private TMP_Text m_HeaderLabel;
        [SerializeField] private TMP_Text m_SubheaderLabel;

        [Header("Portrait")]
        [SerializeField] private Image m_CharacterPortrait;
        [SerializeField] private GameObject m_PortraitGroup;

        [Header("Population")]
        [SerializeField] private InfoPopupPopulation m_PopulationContents;
        [SerializeField] private InfoPopupMarket m_MarketContents;

        #endregion // Inspector

        #region State

        [NonSerialized] private BuildingType m_Mode;
        [NonSerialized] private ResourceSupplier m_SelectedSupplier;
        [NonSerialized] private ResourceRequester m_SelectedRequester;
        [NonSerialized] private StressableActor m_SelectedCity;
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

            Game.Events.Register(GameEvents.MarketCycleTickCompleted, OnMarketTickCompleted);
            Game.SharedState.Get<RoadNetwork>().OnConnectionsReevaluated.Register(OnRoadNetworkRebuilt);
        }

        #endregion // Unity Events

        #region Load

        public void LoadTarget(HasInfoPopup thing) {
            m_Mode = thing.Position.Type;
            switch (m_Mode) {
                case BuildingType.GrainFarm: {
                    m_SelectedCity = null;
                    m_SelectedRequester = thing.GetComponent<ResourceRequester>();
                    m_SelectedSupplier = null;
                    m_PopulationContents.gameObject.SetActive(false);
                    m_MarketContents.gameObject.SetActive(true);
                    break;
                }

                case BuildingType.DairyFarm: {
                    m_SelectedCity = null;
                    m_SelectedRequester = null;
                    m_SelectedSupplier = thing.GetComponent<ResourceSupplier>();
                    m_PopulationContents.gameObject.SetActive(false);
                    m_MarketContents.gameObject.SetActive(true);
                    break;
                }

                case BuildingType.City: {
                    m_SelectedCity = thing.GetComponent<StressableActor>();
                    m_SelectedRequester = null;
                    m_SelectedSupplier = null;
                    m_MarketContents.gameObject.SetActive(false);
                    m_PopulationContents.gameObject.SetActive(true);
                    break;
                }
            }

            m_Pin.Pin(thing.transform);
            PopulateHeader();
            UpdateData(true);

            Show();
        }

        private void PopulateHeader() {

        }

        private void PopulateShipping() {
            int count = Math.Min(m_QueryResults.Count, 3);
            ConfigureRowsAndDividers(count);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();

            for (int i = 0; i < count; i++) {
                var results = m_QueryResults[i];
                InfoPopupMarketUtility.LoadLocationIntoRow(m_MarketContents.Locations[i], results.Supplier.Position, results.Requester.Position);
                InfoPopupMarketUtility.LoadProfitIntoRow(m_MarketContents.Locations[i], results, config);
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
            }
        }

        private void PopulatePurchasing() {
            int count = Math.Min(m_QueryResults.Count, 3);
            ConfigureRowsAndDividers(count);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();

            for(int i = 0; i < count; i++) {
                var results = m_QueryResults[i];
                InfoPopupMarketUtility.LoadLocationIntoRow(m_MarketContents.Locations[i], results.Supplier.Position, results.Requester.Position);
                InfoPopupMarketUtility.LoadCostsIntoRow(m_MarketContents.Locations[i], results, config);
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
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
                    m_QueryResults.Sort(SortResultsAscending);
                    PopulatePurchasing();
                    break;
                }
            }

            m_ConnectionsDirty = false;
        }

        #endregion // Load

        #region Handlers

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            m_Mode = BuildingType.None;
            m_SelectedCity = null;
            m_SelectedRequester = null;
            m_SelectedSupplier = null;
            m_ConnectionsDirty = false;
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            m_Pin.Unpin();
        }

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);

            m_Layout.ForceRebuild(true);
        }

        private void OnMarketTickCompleted() {
            UpdateData(false);
        }

        private void OnRoadNetworkRebuilt() {
            m_ConnectionsDirty = true;
        }

        #endregion // Handlers
    }
}