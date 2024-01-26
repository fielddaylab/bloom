using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zavala.Actors;
using Zavala.Advisor;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI.Info {
    public class InfoPopup : SharedRoutinePanel, IScenePreload {
        static private readonly Color GrainFarmColor = Colors.Hex("#C8E295");
        static private readonly Color DairyFarmColor = Colors.Hex("#E2CD95");
        static private readonly Color CityColor = Colors.Hex("#99FFCE");
        static private readonly Color DigesterColor = Colors.Hex("#F2D779");
        static private readonly Color TollColor = Colors.Hex("#E8F279");
        static private readonly Color StorageColor = Colors.Hex("#F2D779");
        static private readonly Color DepotColor = Colors.Hex("#FFC34E");
        static private readonly Color FrameColor = Colors.Hex("FFFBE3");
        static private readonly Color FrameShadedColor = Colors.Hex("EFE9C4");
        static private readonly Color BuyArrowColor = Colors.Hex("9CE978");
        static private readonly Color BuyArrowTextColor = Colors.Hex("3C9A10");
        static private readonly Color SellArrowColor = Colors.Hex("FFDE67");
        static private readonly Color SellArrowTextColor = Colors.Hex("A68201");


        static private readonly int NarrowWidth = 270;
        static private readonly int NarrowHeight = 300;
        static private readonly int WideWidth = 460;
        static private readonly int WideHeight = 273;
        static private readonly int SmallWidth = 220;
        static private readonly int SmallHeight =  150;
        static private readonly int DefaultHeaderRightPadding = 8;
        static private readonly int ResourceHeaderRightPadding = 75;

        static private readonly int WideNumRows = 3;

        #region Inspector

        [SerializeField] private RectTransformPinned m_Pin;
        [SerializeField] private RectTransform m_PinTransform;
        [SerializeField] private LayoutGroup m_Layout;
        [SerializeField] private RectTransform m_LayoutTransform;

        [Header("Tabs")]
        [SerializeField] private GameObject m_TabGroup;
        [SerializeField] private InfoPopupTab[] m_Tabs;

        [Space(5)]
        [SerializeField] private Sprite m_PoorWaterSprite;
        [SerializeField] private Sprite m_FairWaterSprite;
        [SerializeField] private Sprite m_GreatWaterSprite;

        [Space(5)]
        [SerializeField] private Sprite m_TollCapture;
        [SerializeField] private Sprite m_StorageCapture;
        [SerializeField] private Sprite m_DigesterCapture;
        [SerializeField] private Sprite m_DepotCapture;

        [Header("Header")]
        [SerializeField] private Graphic m_HeaderBG;
        [SerializeField] private TMP_Text m_HeaderLabel;
        [SerializeField] private TMP_Text m_SubheaderLabel;
        [SerializeField] private VerticalLayoutGroup m_HeaderLayout;
        [SerializeField] private Image m_ResourceIcon;

        [Header("Portrait")]
        [SerializeField] private Image m_CharacterPortrait;
        [SerializeField] private GameObject m_PortraitGroup;

        [Header("Population")]
        [SerializeField] private InfoPopupPurchaser m_PurchaseContents;
        [SerializeField] private InfoPopupMarket m_MarketContentsRows;
        [SerializeField] private InfoPopupMarket m_MarketContentsCols;
        [SerializeField] private GameObject m_MarketContentsColsGroup;
        [SerializeField] private InfoPopupColumnHeaders m_MarketContentsColHeaders;

        [SerializeField] private GameObject m_DescriptionGroup;
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private Image m_DescriptionImage;

        [SerializeField] private InfoPopupStorageCapacity m_StorageGroup;

        [SerializeField] private BestLocationHeader m_BestOption;

        #endregion // Inspector

        #region State

        [NonSerialized] private HasInfoPopup m_SelectedThing;
        [NonSerialized] private BuildingType m_Mode;
        [NonSerialized] private LocationDescription m_SelectedLocation;
        [NonSerialized] private ResourceSupplier m_SelectedSupplier;
        [NonSerialized] private ResourceRequester m_SelectedRequester;
        [NonSerialized] private ResourcePurchaser m_SelectedPurchaser;
        [NonSerialized] private ResourceMask m_ActiveResource;
        [NonSerialized] private bool m_ConnectionsDirty;
        [NonSerialized] public bool HoldOpen;

        private readonly RingBuffer<MarketQueryResultInfo> m_QueryResults = new RingBuffer<MarketQueryResultInfo>(16, RingBufferMode.Expand);

        static public readonly Comparison<MarketQueryResultInfo> SortResultsDescending = (x, y) => {
            return y.Profit - x.Profit;
        };

        static public readonly Comparison<MarketQueryResultInfo> SortResultsAscending = (x, y) => {
            return x.Profit - y.Profit;
        };

        #endregion // State

        #region Preload

        public IEnumerator<WorkSlicer.Result?> Preload() {
            foreach (InfoPopupTab tab in m_Tabs) {
                tab.TabButton.onClick.AddListener(() => HandleTabClicked(tab.Resource));
            }
            return null;
        }

        #endregion

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            Game.Events.Register(GameEvents.MarketPrioritiesRebuilt, OnMarketPrioritiesUpdated);
            Game.SharedState.Get<RoadNetwork>().OnConnectionsReevaluated.Register(OnRoadNetworkRebuilt);
            Game.SharedState.Get<PolicyState>().OnPolicyUpdated.Register(OnPolicyUpdated);
        }



        #endregion // Unity Events

        #region Load

        public void LoadTarget(HasInfoPopup thing) {
            if (m_SelectedThing == thing) {
                return;
            }

            m_SelectedThing = thing;
            if (thing.Position)
            {
                m_Mode = thing.Position.Type;
            }
            else
            {
                m_Mode = thing.OverrideType;
            }
            m_SelectedLocation = thing.GetComponent<LocationDescription>();

            m_SelectedPurchaser = null;
            m_SelectedRequester = null;
            m_SelectedSupplier = null;
            m_TabGroup.SetActive(false);
            m_PurchaseContents.gameObject.SetActive(false);
            m_MarketContentsRows.gameObject.SetActive(false);
            m_MarketContentsColsGroup.gameObject.SetActive(false);
            m_DescriptionGroup.gameObject.SetActive(false);
            m_DescriptionText.gameObject.SetActive(false);
            m_StorageGroup.gameObject.SetActive(false);
            m_ResourceIcon.gameObject.SetActive(false);
            m_HeaderLayout.padding.right = DefaultHeaderRightPadding;

            switch (m_Mode) {
                case BuildingType.GrainFarm: {
                    //m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel)/* + " (" + Loc.Find(m_SelectedLocation.RegionId) + ")"*/);
                    m_SelectedRequester = thing.GetComponent<ResourceRequester>();
                    m_SelectedSupplier = thing.GetComponent<ResourceSupplier>();
                    m_MarketContentsColsGroup.gameObject.SetActive(true);
                    m_HeaderBG.color = GrainFarmColor;
                    m_ResourceIcon.gameObject.SetActive(true);
                    m_HeaderLayout.padding.right = ResourceHeaderRightPadding;
                    m_TabGroup.SetActive(true);
                    SetTabsVisible((ResourceMask)0b01110); // weird to hardcode this?
                    PickTab(ResourceMask.Phosphorus);
                    break;
                }

                case BuildingType.DairyFarm: {
                    //m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel)/* + " (" + Loc.Find(m_SelectedLocation.RegionId) + ")"*/);
                    m_SelectedRequester = thing.GetComponent<ResourceRequester>();
                    m_SelectedSupplier = thing.GetComponent<ResourceSupplier>();
                    m_MarketContentsColsGroup.gameObject.SetActive(true);
                    m_HeaderBG.color = DairyFarmColor;
                    m_ResourceIcon.gameObject.SetActive(true);
                    m_HeaderLayout.padding.right = ResourceHeaderRightPadding;
                    m_TabGroup.SetActive(true);
                    SetTabsVisible((ResourceMask)0b11001); // weird to hardcode this?
                    PickTab(ResourceMask.Manure);
                    break;
                    }

                case BuildingType.City: {
                    m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.InfoLabel));
                    m_SelectedPurchaser = thing.GetComponent<ResourcePurchaser>();
                    m_PurchaseContents.gameObject.SetActive(true);
                    m_HeaderBG.color = CityColor;
                    break;
                }

                case BuildingType.Digester: {
                    m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.InfoLabel));
                    m_HeaderBG.color = DigesterColor;
                    m_DescriptionGroup.gameObject.SetActive(true);
                    m_DescriptionText.gameObject.SetActive(true);
                    m_DescriptionImage.sprite = m_DigesterCapture;
                    break;
                }

                case BuildingType.Storage:
                {
                    m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.InfoLabel));
                    m_HeaderBG.color = StorageColor;
                    m_DescriptionGroup.gameObject.SetActive(true);
                    m_DescriptionText.gameObject.SetActive(true);
                    m_StorageGroup.gameObject.SetActive(true);
                    m_DescriptionImage.sprite = m_StorageCapture;
                    break;
                }

                case BuildingType.TollBooth:
                {
                    m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText("");
                    m_HeaderBG.color = TollColor;
                    m_DescriptionGroup.gameObject.SetActive(true);
                    m_DescriptionText.gameObject.SetActive(true);
                    m_DescriptionImage.sprite = m_TollCapture;
                    break;
                }

                case BuildingType.ExportDepot:
                {
                    m_HeaderLabel.SetText(Loc.Find(m_SelectedLocation.TitleLabel));
                    m_SubheaderLabel.SetText(Loc.Find(m_SelectedLocation.InfoLabel));
                    m_HeaderBG.color = DepotColor;
                    m_DescriptionGroup.gameObject.SetActive(true);
                    m_DescriptionText.gameObject.SetActive(true);
                    m_DescriptionImage.sprite = m_DepotCapture;
                    break;
                }
            }

            m_Pin.Pin(thing.transform);
            PopulateHeader();
            m_ConnectionsDirty = true;
            Game.Events.Dispatch(GameEvents.ForceMarketPrioritiesRebuild);

            Show();
        }

        private void PopulateHeader() {
            ScriptCharacterDB charDB = Game.SharedState.Get<ScriptCharacterDB>();
            ScriptCharacterDef charDef = ScriptCharacterDBUtility.Get(charDB, m_SelectedLocation.CharacterId);

            m_PortraitGroup.SetActive(charDef != null);
            if (charDef != null) {
                m_CharacterPortrait.sprite = charDef.PortraitArt;
            }
        }

        private void PopulateMarketDisplay(bool isShipping) {
            if (isShipping) {
                PopulateShippingWide();
            } else {
                PopulatePurchasingWide();
            }
        }

        /// <summary>
        /// Populate seller (CAFO) popup
        /// </summary>
        private void PopulateShippingWide()
        {
            bool showRunoff = (m_ActiveResource & ResourceMask.Manure) != 0;
            int shrinkShipping = showRunoff ? 70 : 120;
            Root.sizeDelta = new Vector2(WideWidth - shrinkShipping, WideHeight);

            int queryCount = Math.Min(m_QueryResults.Count, WideNumRows);
            ConfigureCols(WideNumRows);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            for (int i = 0; i < WideNumRows; i++)
            {
                if (i < queryCount)
                {
                    var results = m_QueryResults[i];
                    bool forSale = true;
                    bool runoffAffected = results.TaxRevenue.Penalties > 0;
                    InfoPopupMarketUtility.LoadLocationIntoCol(m_MarketContentsCols.LocationCols[i], results.Requester.Position, results.Supplier.Position, forSale, runoffAffected, m_BestOption.gameObject, i);
                    InfoPopupMarketUtility.LoadProfitIntoCol(policyState, grid, m_MarketContentsCols.LocationCols[i], m_MarketContentsColHeaders, results, config, forSale, i > 0, showRunoff);
                    m_MarketContentsCols.LocationCols[i].Arrow.color = SellArrowColor;
                    m_MarketContentsCols.LocationCols[i].ArrowText.color = SellArrowTextColor;
                }
                else
                {
                    // load empty col group
                    InfoPopupMarketUtility.LoadEmptyProfitCol(policyState, grid, m_MarketContentsCols.LocationCols[i], m_MarketContentsColHeaders, m_BestOption.gameObject, i);
                }
                m_MarketContentsColHeaders.PenaltyColHeader.gameObject.SetActive(showRunoff);
            }
            InfoPopupMarketUtility.AssignColColors(m_MarketContentsColHeaders);
            if (m_BestOption.gameObject.activeSelf)
            {
                m_BestOption.Banner.SetText(Loc.Find("ui.popup.info.selling"));
                m_BestOption.ArrowText.SetText(Loc.Find("ui.popup.info.sellingArrow"));
                m_BestOption.Arrow.color = SellArrowColor;
                m_BestOption.ArrowText.color = SellArrowTextColor;
            }

            if (gameObject.activeSelf)
            {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        /// <summary>
        /// Populate buyer popup
        /// </summary>
        private void PopulatePurchasingWide()
        {
            Root.sizeDelta = new Vector2(WideWidth, WideHeight);

            int queryCount = Math.Min(m_QueryResults.Count, WideNumRows);
            ConfigureCols(queryCount);

            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            PolicyState policyState = Game.SharedState.Get<PolicyState>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            for (int i = 0; i < WideNumRows; i++)
            {
                if (i < queryCount)
                {
                    var results = m_QueryResults[i];
                    bool forSale = true;
                    bool runoffAffected = false;
                    InfoPopupMarketUtility.LoadLocationIntoCol(m_MarketContentsCols.LocationCols[i], results.Supplier.Position, results.Requester.Position, forSale, runoffAffected, m_BestOption.gameObject, i);
                    InfoPopupMarketUtility.LoadCostsIntoCol(policyState, grid, m_MarketContentsCols.LocationCols[i], m_MarketContentsColHeaders, results, config, forSale, i > 0);
                    m_MarketContentsCols.LocationCols[i].Arrow.color = BuyArrowColor;
                    m_MarketContentsCols.LocationCols[i].ArrowText.color = BuyArrowTextColor;
                }
                else
                {
                    // load empty col group
                    InfoPopupMarketUtility.LoadEmptyCostsCol(policyState, grid, m_MarketContentsCols.LocationCols[i], m_MarketContentsColHeaders, m_BestOption.gameObject, i);
                }
            }
            InfoPopupMarketUtility.AssignColColors(m_MarketContentsColHeaders);
            if (m_BestOption.gameObject.activeSelf)
            {
                m_BestOption.Banner.SetText(Loc.Find("ui.popup.info.buying"));
                m_BestOption.ArrowText.SetText(Loc.Find("ui.popup.info.buyingArrow"));
                m_BestOption.Arrow.color = BuyArrowColor;
                m_BestOption.ArrowText.color = BuyArrowTextColor;
            }

            if (gameObject.activeSelf)
            {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void PopulateDescriptionSmall()
        {
            Root.sizeDelta = new Vector2(SmallWidth, SmallHeight);

            m_DescriptionText.SetText(Loc.Find(m_SelectedLocation.DescriptionLabel));

            if (gameObject.activeSelf)
            {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void PopulateStorageCapacity()
        {
            Root.sizeDelta = new Vector2(SmallWidth, SmallHeight + 15);

            var storage = m_SelectedLocation.GetComponent<ResourceStorage>();
            if (!storage) { return; }

            InfoPopupMarketUtility.LoadStorageCapacity(m_StorageGroup, storage.Current.Manure, storage.Capacity.Manure);

            m_DescriptionText.SetText(Loc.Find(m_SelectedLocation.DescriptionLabel));

            if (gameObject.activeSelf)
            {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void PopulateCity() {
            Root.sizeDelta = new Vector2(NarrowWidth, NarrowHeight);

            /*
            int purchaseAmount = m_SelectedPurchaser.RequestAmount.Milk;
            m_PurchaseContents.Number.SetText(purchaseAmount.ToStringLookup());

            float average = 0;
            foreach(var amt in m_SelectedPurchaser.RequestAmountHistory) {
                average += amt.Milk;
            }
            average /= m_SelectedPurchaser.RequestAmountHistory.Count;
            */

            // TODO: determine sources of stress (blooms, not enough milk, etc)
            m_PurchaseContents.WaterStatus.gameObject.SetActive(true);
            m_PurchaseContents.MilkStatus.gameObject.SetActive(true);
            StressableActor actor = m_SelectedPurchaser.GetComponent<StressableActor>();

            if (actor.OperationState == OperationState.Great)
            {
                m_PurchaseContents.StatusText.SetText(Loc.Find("ui.popup.info.city.rising"));
                m_PurchaseContents.WaterStatus.Description.SetText(Loc.Find("ui.popup.info.city.water.rising"));
                m_PurchaseContents.WaterStatus.Icon.sprite = m_GreatWaterSprite;
                m_PurchaseContents.MilkStatus.Description.SetText(Loc.Find("ui.popup.info.city.milk.rising"));

                m_PurchaseContents.Arrow.gameObject.SetActive(true);
                m_PurchaseContents.Arrow.color = InfoPopupMarketUtility.PositiveColor;
                m_PurchaseContents.Arrow.rectTransform.SetRotation(90, Axis.Z, Space.Self);

                // if water is less than great (>= to 4), don't show as contributing factor
                if (actor.CurrentStress[StressCategory.Bloom] >= actor.OperationThresholds[OperationState.Okay])
                {
                    m_PurchaseContents.WaterStatus.gameObject.SetActive(false);
                }
                if (actor.CurrentStress[StressCategory.Resource] >= actor.OperationThresholds[OperationState.Okay])
                {
                    m_PurchaseContents.MilkStatus.gameObject.SetActive(false);
                }
            }
            else if (actor.OperationState == OperationState.Bad)
            {
                m_PurchaseContents.StatusText.SetText(Loc.Find("ui.popup.info.city.falling"));
                m_PurchaseContents.WaterStatus.Description.SetText(Loc.Find("ui.popup.info.city.water.falling"));
                m_PurchaseContents.WaterStatus.Icon.sprite = m_PoorWaterSprite;
                m_PurchaseContents.MilkStatus.Description.SetText(Loc.Find("ui.popup.info.city.milk.falling"));

                m_PurchaseContents.Arrow.gameObject.SetActive(true);
                m_PurchaseContents.Arrow.color = InfoPopupMarketUtility.NegativeColor;
                m_PurchaseContents.Arrow.rectTransform.SetRotation(-90, Axis.Z, Space.Self);

                // if water is better than bad (7 or less), don't show as contributing factor
                if (actor.CurrentStress[StressCategory.Bloom] < actor.OperationThresholds[OperationState.Bad])
                {
                    m_PurchaseContents.WaterStatus.gameObject.SetActive(false);
                }
                if (actor.CurrentStress[StressCategory.Resource] < actor.OperationThresholds[OperationState.Bad])
                {
                    m_PurchaseContents.MilkStatus.gameObject.SetActive(false);
                }
            }
            else
            {
                m_PurchaseContents.StatusText.SetText(Loc.Find("ui.popup.info.city.stable"));
                m_PurchaseContents.WaterStatus.Description.SetText(Loc.Find("ui.popup.info.city.water.stable"));
                m_PurchaseContents.WaterStatus.Icon.sprite = m_FairWaterSprite;
                m_PurchaseContents.MilkStatus.Description.SetText(Loc.Find("ui.popup.info.city.milk.stable"));

                m_PurchaseContents.Arrow.gameObject.SetActive(false);

                m_PurchaseContents.WaterStatus.gameObject.SetActive(false);
                m_PurchaseContents.MilkStatus.gameObject.SetActive(false);

                // if water is great or terrible (< 4 or >= 8), don't show as contributing factor
                if (actor.CurrentStress[StressCategory.Bloom] < actor.OperationThresholds[OperationState.Okay]
                    && actor.CurrentStress[StressCategory.Bloom] >= actor.OperationThresholds[OperationState.Bad])
                {
                    m_PurchaseContents.WaterStatus.gameObject.SetActive(true);
                }
                if (actor.CurrentStress[StressCategory.Resource] < actor.OperationThresholds[OperationState.Okay]
                     && actor.CurrentStress[StressCategory.Resource] >= actor.OperationThresholds[OperationState.Bad])
                {
                    m_PurchaseContents.MilkStatus.gameObject.SetActive(true);
                }

                /*
                // if neither status are within okay range, show the worst
                if (!m_PurchaseContents.MilkStatus.gameObject.activeSelf && !m_PurchaseContents.WaterStatus.gameObject.activeSelf)
                {
                    if (actor.CurrentStress[StressCategory.Resource] >= actor.CurrentStress[StressCategory.Bloom])
                    {
                        m_PurchaseContents.MilkStatus.gameObject.SetActive(true);
                    }
                    else
                    {
                        m_PurchaseContents.WaterStatus.gameObject.SetActive(true);
                    }
                }
                */
            }

            if (gameObject.activeSelf) {
                m_Layout.ForceRebuild(true);
                m_PinTransform.sizeDelta = m_LayoutTransform.sizeDelta;
            }
        }

        private void ConfigureRowsAndDividers(int count) {
            for (int i = 0; i < m_MarketContentsRows.Dividers.Length; i++) {
                m_MarketContentsRows.Dividers[i].SetActive(count > i + 1);
            }

            for (int i = 0; i < m_MarketContentsRows.LocationRows.Length; i++) {
                m_MarketContentsRows.LocationRows[i].gameObject.SetActive(count > i);
            }
        }

        private void ConfigureCols(int count)
        {
            /*
            for (int i = 0; i < m_MarketContentsCols.Dividers.Length; i++)
            {
                m_MarketContentsCols.Dividers[i].SetActive(count > i + 1);
            }
            */

            /*
            for (int i = 0; i < m_MarketContentsCols.LocationCols.Length; i++)
            {
                m_MarketContentsCols.LocationCols[i].gameObject.SetActive(count > i);
            }
            */
        }

        private void UpdateData(bool forceRefresh) {
            Log.Msg("[InfoPopup] Updating data... resource: {0}", m_ActiveResource);
            bool refresh = m_ConnectionsDirty || forceRefresh;

            switch (m_Mode) {
                case BuildingType.DairyFarm: {
                    bool shipping = m_ActiveResource != ResourceMask.Grain;
                    if (refresh) {
                        m_QueryResults.Clear();
                        m_HeaderLabel.SetText(BuySellString(shipping));
                    }

                    MarketUtility.GeneralMarketQuery(m_SelectedRequester, m_SelectedSupplier, m_QueryResults, m_ActiveResource, shipping, refresh);

                    SortQueryResults(shipping);
                    PopulateMarketDisplay(shipping);
                    break;
                }

                case BuildingType.GrainFarm: {
                    bool shipping = m_ActiveResource == ResourceMask.Grain;
                    if (refresh) {
                        m_QueryResults.Clear();
                        m_HeaderLabel.SetText(BuySellString(shipping));
                    }
                    MarketUtility.GeneralMarketQuery(m_SelectedRequester, m_SelectedSupplier, m_QueryResults, m_ActiveResource, shipping, refresh);
                    SortQueryResults(shipping);
                    PopulateMarketDisplay(shipping);
                    break;
                }

                case BuildingType.City: {
                    PopulateCity();
                    break;
                }

                case BuildingType.Digester: {
                    PopulateDescriptionSmall();
                    break;
                }

                case BuildingType.Storage: {
                    PopulateStorageCapacity();
                    break;
                }

                case BuildingType.TollBooth: {
                    PopulateDescriptionSmall();
                    break;
                }

                case BuildingType.ExportDepot: {
                    PopulateDescriptionSmall();
                    break;
                }
            }

            m_ConnectionsDirty = false;
        }

        private void SortQueryResults(bool isShippingResource) {
            if (isShippingResource) {
                m_QueryResults.Sort(SortResultsDescending);
            } else {
                m_QueryResults.Sort((a, b) => {
                    int dif = a.CostToBuyer - b.CostToBuyer;
                    if (dif == 0) {
                        // tie break
                        if (b.Supplier.Position.IsExternal && a.Supplier.Position.IsExternal) {
                            // no additional tiebreaker (random)
                            return 0;
                        } else if (b.Supplier.Position.IsExternal) {
                            // favor a
                            return -1;
                        } else if (a.Supplier.Position.IsExternal) {
                            // favor b
                            return 1;
                        } else {
                            // favor closest option
                            return b.Distance - a.Distance;
                        }
                    } else {
                        return dif;
                    }
                });
            }
        }

        private void SetTabsVisible(ResourceMask relevantResources) {
            foreach (InfoPopupTab tab in m_Tabs) {
                ResourceMask r = tab.Resource;
                if ((relevantResources & r) != 0) { // full overlaps only
                    tab.gameObject.SetActive(true);
                    // special case to disable fert. on cafos
                    if ((r & ResourceMask.MFertilizer) != 0 && (relevantResources & ResourceMask.Milk) != 0) {
                        tab.gameObject.SetActive(false);
                    }
                } else {
                    tab.gameObject.SetActive(false);
                }
            }
        }

        private void PickTab(ResourceMask resource) {
            foreach (InfoPopupTab tab in m_Tabs) {
                if ((tab.Resource & resource) != 0) {
                    m_ActiveResource = resource;
                    tab.TintTab(FrameColor);
                    m_ResourceIcon.sprite = tab.GetSprite();
                } else {
                    tab.TintTab(FrameShadedColor);
                }
            }
        }

        private void HandleTabClicked(ResourceMask resource) {
            PickTab(resource);
            // Game.Events.Dispatch(GameEvents.ForceMarketPrioritiesRebuild);
            UpdateData(true);
        }

        private string BuySellString(bool isShipping) {
            string val = "";
            val += m_ActiveResource.ToString() + " ";
            if (isShipping) {
                val += Loc.Find("ui.popup.info.sellingResource");
            } else {
                val += Loc.Find("ui.popup.info.buyingResource");
            }

            return val;
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

        private void OnPolicyUpdated()
        {
            m_ConnectionsDirty = true;
            Game.Events.Dispatch(GameEvents.ForceMarketPrioritiesRebuild);
            // Game.Events.Dispatch(GameEvents.ForceMarketPrioritiesRebuild);
        }

        #endregion // Handlers
    }
}