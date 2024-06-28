#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections.Generic;
using System.Text;
using BeauData;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Rendering;
using FieldDay.Scripting;
using OGD;
using UnityEngine;
using Zavala.Advisor;
using Zavala.Building;
using Zavala.Cards;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.UI.Info;
using Zavala.World;

namespace Zavala.Data {

    /*
    "SUB-TYPES"
    BuildTile : List[enum(// all the types of object)]
    CountyBuildMap { List[BuildTiles] }
    BuildMap : { List[CountyBuildMap] }
     */
    public enum MenuInteractionType : byte {
        CreditsButtonClicked,
        CreditsExited,
        NewGameClicked,
        ResumeGameClicked,
        PlayGameClicked,
        ReturnedToMainMenu,
        GameStarted,
    }

    public enum ModeInteraction : byte {
        Clicked, // clicked button to start
        Started, // started
        Confirmed, // clicked button to finish
        Exited, // clicked button to cancel
        Ended // ended mode
    }

    public enum CityPopulationLog : byte
    {
        GOOD,
        BAD,
        OKAY
    }

    public enum CityWaterStatusLog : byte
    {
        GOOD,
        OKAY,
        BAD
    }

    public enum CityMilkStatusLog : byte
    {
        GOOD,
        OKAY,
        BAD
    }

    public enum TerrainType : byte {
        Land,
        Water,
        DeepWater,
        Void
    }

    public struct ZoomVolData {
        public float Start;
        public float End;
        public bool UsedWheel;
        public ZoomVolData(float start, float end, bool usedWheel) {
            Start = start;
            End = end;
            UsedWheel = usedWheel;
        }
    }

    public struct MapTile {
        public int TileIndex;
        public ushort Height;
        public TerrainType TileType;
        public TileAdjacencyMask Connections;
        public BuildingType Building;
    }

    public struct BuildTile {
        //     Building : { building_type, tile_index, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }
        public BuildingLocation Building;
        public TileAdjacencyMask Connections;
        public int Cost;
        public bool IsDestroying;
        public BuildTile(BuildingType type, string id, int idx, int cost, TileAdjacencyMask mask, ActionType action) {
            Building = new BuildingLocation(type, id, idx);
            Cost = cost;
            Connections = mask;
            IsDestroying = (action == ActionType.Destroy);
        }
        public BuildTile(ActionCommit commit) {
            Building = new BuildingLocation(commit.BuildType, "", commit.TileIndex);
            Cost = commit.Cost;
            Connections = commit.FlowMaskSnapshot;
            IsDestroying = (commit.ActionType == ActionType.Destroy);
        }
    }

    [Serializable]
    public struct BuildingLocation {
        public BuildingType Type;
        public string Id;
        public int TileIndex;
        public BuildingLocation(BuildingType type, string id, int tileIndex) {
            Type = type;
            Id = id;
            TileIndex = tileIndex;
        }
        public BuildingLocation(int tileIndex) {
            TileIndex = tileIndex;
            Id = "";
            Type = BuildingType.None;
        }
    }

    [Serializable]
    public struct CountyBuildMap {
        public List<BuildTile> Tiles;
    }

    public struct CityData {
        public string Name;
        public CityPopulationLog Population;
        public CityWaterStatusLog Water;
        public CityMilkStatusLog Milk;

        public CityData(string name, CityPopulationLog population, CityWaterStatusLog water, CityMilkStatusLog milk)
        {
            Name = name;
            Population = population;
            Water = water;
            Milk = milk;
        }

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("name", Name)
                .Field("population", EnumLookup.Get(Population))
                .Field("water", EnumLookup.Get(Water))
                .Field("milk", EnumLookup.Get(Milk));
            return json;
        }
    }

    public struct GrainFarmData {
        public List<GFarmGrainTabData> GrainTab;
        public List<GFarmFertilizerTabData> FertilizerTab;

        public GrainFarmData(List<GFarmGrainTabData> grainTab, List<GFarmFertilizerTabData> fertilizerTab)
        {
            GrainTab = grainTab;
            FertilizerTab = fertilizerTab;
        }
    }

    [Serializable]
    public struct GFarmGrainTabData
    {
        public bool IsActive;
        public string FarmName;
        public string FarmCounty;
        public int BasePrice;
        public int ShippingCost;
        public int TotalProfit;

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("is_active", IsActive)
                .Field("buyer_name", FarmName)
                .Field("buyer_county", FarmCounty)
                .Field("base_price", BasePrice)
                .Field("shipping_cost", ShippingCost)
                .Field("total_profit", TotalProfit);
            return json;
        }
    }

    [Serializable]
    public struct GFarmFertilizerTabData
    {
        public bool IsActive;
        public string FarmName;
        public string FarmCounty;
        public int BasePrice;
        public int ShippingCost;
        public int SalesPolicy;
        public int ImportPolicy;
        public int TotalProfit;

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("is_active", IsActive)
                .Field("seller_name", FarmName)
                .Field("seller_county", FarmCounty)
                .Field("base_price", BasePrice)
                .Field("shipping_cost", ShippingCost)
                .Field("sales_policy", SalesPolicy)
                .Field("import_policy", ImportPolicy)
                .Field("total_profit", TotalProfit);
            return json;
        }
    }

    [Serializable]
    public struct DairyFarmData
    {
        public List<DFarmGrainTabData> GrainTab;
        public List<DFarmDairyTabData> DairyTab;
        public List<DFarmFertilizerTabData> FertilizerTab;

        public DairyFarmData(List<DFarmGrainTabData> grainTab, List<DFarmDairyTabData> dairyTab, List<DFarmFertilizerTabData> fertilizerTab)
        {
            GrainTab = grainTab;
            DairyTab = dairyTab;
            FertilizerTab = fertilizerTab;
        }
    }

    [Serializable]
    public struct DFarmGrainTabData
    {
        public bool IsActive;
        public string FarmName;
        public string FarmCounty;
        public int BasePrice;
        public int ShippingCost;
        public int SalesPolicy;
        public int ImportPolicy;
        public int TotalProfit;

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("is_active", IsActive)
                .Field("seller_name", FarmName)
                .Field("seller_county", FarmCounty)
                .Field("base_price", BasePrice)
                .Field("shipping_cost", ShippingCost)
                .Field("sales_policy", SalesPolicy)
                .Field("import_policy", ImportPolicy)
                .Field("total_profit", TotalProfit);
            return json;
        }
    }

    [Serializable]
    public struct DFarmDairyTabData
    {
        public bool IsActive;
        public string FarmName;
        public string FarmCounty;
        public int BasePrice;
        public int TotalProfit;

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("is_active", IsActive)
                .Field("buyer_name", FarmName)
                .Field("buyer_county", FarmCounty)
                .Field("base_price", BasePrice)
                .Field("total_profit", TotalProfit);
            return json;
        }
    }

    [Serializable]
    public struct DFarmFertilizerTabData
    {
        public bool IsActive;
        public string FarmName;
        public string FarmCounty;
        public int BasePrice;
        public int ShippingCost;
        public int Penalties;
        public int TotalProfit;

        public JsonBuilder ToJson(JsonBuilder json) {
            json.Field("is_active", IsActive)
                .Field("buyer_name", FarmName)
                .Field("buyer_county", FarmCounty)
                .Field("base_price", BasePrice)
                .Field("shipping_cost", ShippingCost)
                .Field("runoff_fine", Penalties)
                .Field("total_profit", TotalProfit);
            return json;
        }
    }

    [Serializable]
    public struct InspectorDisplayData
    {
        public List<InspectorTabProfitGroup> AllAvailableTabs;
    }

    public struct StorageData {
        public int UnitsFilled;

        public StorageData(int filled) {
            UnitsFilled = filled;
        }
    }

    public struct LossData {
        public string EndType;
        public ushort Region;
        public LossData(string type, ushort region) {
            EndType = type;
            Region = region;
        }
    }


    public struct ScriptNodeData {
        public string NodeId;
        public bool Skippable;
        public ScriptNodeData(string nodeId, bool skippable) {
            NodeId = nodeId;
            Skippable = skippable;
        }
    }

    public struct DialogueLineData {
        public string CharName;
        public string CharTitle;
        public string Text;
        public DialogueLineData(string name, string title, string text) {
            CharName = name;
            CharTitle = title;
            Text = text;
        }
        public DialogueLineData(string text) {
            CharName = CharTitle = "cutscene";
            Text = text;
        }

        // TODO: add character CLASS (titleId doesn't cut it!)
    }

    public struct ExportDepotData {
        public string Id;
        public int TileIndex;
        public ExportDepotData(string id, int index) {
            Id = id;
            TileIndex = index;
        }
    }

    public struct PolicyData {
        public PolicyType Type;
        public int Level;
        // public int RegionIndex;
        public string HintText;
        public PolicyData(PolicyType type, int level, /*int region,*/ string hintText = "") {
            Type = type;
            Level = level;
            // RegionIndex = region;
            HintText = hintText;
        }
    }

    public struct SkimmerData {
        public int TileIndex;
        public bool IsAppearing;
        public bool IsDredger;
        public SkimmerData(int idx, bool appearing, bool dredger) {
            TileIndex = idx;
            IsAppearing = appearing;
            IsDredger = dredger;
        }
    }

    public struct AlertData {
        public EventActorAlertType Type;
        public int TileIndex;
        public string AttachedNode;
        public EventActor Actor;
        public AlertData(EventActor actor, EventActorAlertType type, int idx, string node) {
            Type = type;
            TileIndex = idx;
            AttachedNode = node;
            Actor = actor;
        }
        public AlertData(EventActor actor, EventActorQueuedEvent evt) {
            Type = evt.Alert;
            TileIndex = evt.TileIndex;
            Actor = actor;

            ScriptNode foundNode = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, evt.ScriptId);
            if (foundNode != null)
            {
                AttachedNode = foundNode.FullName;
            }
            else
            {
                Debug.LogWarning("[Analytics] No node found for script id " + evt.ScriptId.ToString());
                AttachedNode = null;
            }
        }
    }

    public struct AlgaeData {
        public bool IsGrowing;
        public int TileIndex;
        public int Phosphorus;
        public float Algae;
        public AlgaeData(bool growing, int index, int phos, float algae) {
            IsGrowing = growing;
            TileIndex = index;
            Phosphorus = phos;
            Algae = algae;
        }
    }

    public class AnalyticsService : MonoBehaviour {
        private const ushort CLIENT_LOG_VERSION = 2;

        private static class Mode {
            public static readonly string View = "VIEW";
            public static readonly string Build = "BUILD";
            public static readonly string Destroy = "DESTROY";
        }

        #region Inspector
        [SerializeField, Required] private string m_AppId = "BLOOM";
        [SerializeField, Required] private string m_AppVersion = "1.0";
        // TODO: set up firebase consts in inspector
        [SerializeField] private FirebaseConsts m_Firebase = default;
        [SerializeField] private bool m_Testing = false;

        #endregion // Inspector

        private readonly JsonBuilder m_JsonBuilder = new JsonBuilder(Unsafe.KiB * 64);

        [Serializable]
        private struct PolicyStateData {
            public PolicyLevelData sales;
            public PolicyLevelData import;
            public PolicyLevelData runoff;
            public PolicyLevelData cleanup;

            public readonly JsonBuilder Append(JsonBuilder json) {
                json.BeginObject("sales");
                sales.Append(json).EndObject();
                json.BeginObject("import");
                import.Append(json).EndObject();
                json.BeginObject("runoff");
                runoff.Append(json).EndObject();
                json.BeginObject("cleanup");
                cleanup.Append(json).EndObject();
                return json;
            }
        }

        [Serializable]
        private struct PolicyLevelData {
            public int policy_choice;
            public bool is_locked;

            public readonly JsonBuilder Append(JsonBuilder json) {
                json.Field("policy_choice", policy_choice.ToStringLookup());
                json.Field("is_locked", is_locked);
                return json;
            }
            
        }

        #region Logging Variables

        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        [NonSerialized] private float m_MusicVolume;
        [NonSerialized] private bool m_IsFullscreen;
        [NonSerialized] private bool m_IsQuality;

        [NonSerialized] private ushort m_CurrentRegionIndex;
        [NonSerialized] private int m_CurrentBudget = -1;
        [NonSerialized] private string m_CurrentMode = Mode.View;
        [NonSerialized] private PolicyStateData m_CountyPolicies;
        [NonSerialized] private bool m_PhosView = false;

        [NonSerialized] private string m_CurrentTool;
        [NonSerialized] private BuildingLocation m_InspectingBuilding;
        [NonSerialized] private short m_CurrentCutscene;
        [NonSerialized] private short m_CurrentCutscenePage;
        [NonSerialized] private DialogueLineData m_CurrentLine;
        // current policies
        // dialogue character opened (or none)
        // dialogue phrase displaying (or none)


        #endregion // Logging Variables

        #region Register and Deregister

        private void Start() {


            ZavalaGame.Events
                // Main Menu
                .Register<MenuInteractionType>(GameEvents.MainMenuInteraction, HandleMenuInteraction)
                .Register<ZoomVolData>(GameEvents.VolumeChanged, LogVolumeChanged)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode)
                .Register<bool>(GameEvents.FullscreenToggled, LogFullscreenToggled)
                .Register<bool>(GameEvents.QualityToggled, LogQualityToggled)
                // Sim Controls
                .Register<AdvisorType>(GameEvents.AdvisorButtonClicked, LogAdvisorButtonClicked)
                .Register<int>(GameEvents.BudgetRefreshed, UpdateMoneyState)
                .Register<bool>(GameEvents.ToggleEconomyView, LogMarketToggled)
                .Register<bool>(GameEvents.TogglePhosphorusView, LogPhosToggled)
                .Register<AlertData>(GameEvents.AlertAppeared, LogAlertDisplayed)
                .Register<AlertData>(GameEvents.AlertClicked, LogAlertClicked)
                .Register<AlertData>(GameEvents.GlobalAlertAppeared, LogGlobalAlertDisplayed)
                .Register<AlertData>(GameEvents.GlobalAlertClicked, LogGlobalAlertClicked)
                .Register<ExportDepotData>(GameEvents.ExportDepotUnlocked, LogExportDepot)
                .Register<AlgaeData>(GameEvents.SimAlgaeChanged, HandleAlgaeChanged)
                // TODO: do we want to log every pause or just player pause?
                .Register(GameEvents.SimPaused, LogGamePaused)
                .Register(GameEvents.SimResumed, LogGameResumed)
                // Build
                // TODO: Condense blueprint events with BlueprintInteractionType?
                .Register(GameEvents.BlueprintModeStarted, LogStartBuildMode)
                .Register(GameEvents.BuildMenuDisplayed, LogDisplayBuildMenu)
                .Register(GameEvents.DestroyModeClicked, LogClickedDestroy)
                .Register(GameEvents.DestroyModeStarted, LogStartDestroy)
                .Register(GameEvents.DestroyModeConfirmed, LogConfirmedDestroy)
                .Register(GameEvents.DestroyModeExited, LogClickedExitDestroy)
                .Register(GameEvents.DestroyModeEnded, LogEndDestroy)
                .Register(GameEvents.BlueprintModeEnded, LogEndBuildMode)
                .Register<RingBuffer<CommitChain>>(GameEvents.BuildConfirmed, LogConfirmedBuild)
                .Register<UserBuildTool>(GameEvents.BuildToolSelected, LogSelectedBuildTool)
                .Register<int>(GameEvents.HoverTile, HandleHover)
                .Register<int>(GameEvents.BuildInvalid, LogBuildInvalid)
                .Register<int>(GameEvents.DestroyInvalid, LogDestroyInvalid)
                .Register<ActionCommit>(GameEvents.BuildingQueued, LogQueuedBuilding)
                .Register<ActionCommit>(GameEvents.BuildingUnqueued, LogUnqueuedBuilding)
                .Register<UserBuildTool>(GameEvents.BuildToolUnlocked, LogBuildingUnlocked)
               
                // Advisor/Policy
                .Register<PolicyType>(GameEvents.PolicyTypeUnlocked, LogPolicyUnlocked)
                .Register<PolicyType>(GameEvents.PolicySlotClicked, (PolicyType type) => LogPolicyOpened(type, false))
                .Register<PolicyType>(GameEvents.PolicyButtonClicked, (PolicyType type) => LogPolicyOpened(type, true))
                .Register<PolicyData>(GameEvents.PolicyHover, LogHoverPolicy)
                .Register<PolicyData>(GameEvents.PolicySet, LogPolicySet)
                .Register<bool>(GameEvents.AdvisorLensUnlocked, LogUnlockView)
                .Register<SkimmerData>(GameEvents.SkimmerChanged, HandleSkimmer)
                // ADD?                
                .Register<ushort>(GameEvents.RegionSwitched, LogRegionChanged)
                .Register<ushort>(GameEvents.RegionUnlocked, LogRegionUnlocked)
                .Register<ZoomVolData>(GameEvents.SimZoomChanged, LogZoom)
                // Inspect
                .Register<BuildingLocation>(GameEvents.InspectorOpened, LogInspectBuilding)
                .Register(GameEvents.GenericInspectorDisplayed, LogCommonInspectorDisplayed)
                .Register<CityData>(GameEvents.CityInspectorDisplayed, LogCityInspectorDisplayed)
                .Register<GrainFarmData>(GameEvents.GrainFarmInspectorDisplayed, LogGrainFarmInspectorDisplayed)
                .Register<DairyFarmData>(GameEvents.DairyFarmInspectorDisplayed, LogDairyFarmInspectorDisplayed)
                .Register<StorageData>(GameEvents.StorageInspectorDisplayed, LogStorageInspectorDisplayed)
                .Register<string>(GameEvents.InspectorTabClicked, LogClickInspectorTab)
                .Register(GameEvents.InspectorClosed, LogDismissInspector)
                // Dialogue
                .Register<int>(GameEvents.CutsceneStarted, LogStartCutscene)
                .Register(GameEvents.CutsceneEnded, LogEndCutscene)
                .Register<ScriptNodeData>(GameEvents.DialogueStarted, LogStartDialogue)
                .Register<DialogueLineData>(GameEvents.DialogueDisplayed, HandleDialogueDisplayed)
                .Register(GameEvents.DialogueAdvanced, HandleDialogueAdvanced)
                .Register<ScriptNodeData>(GameEvents.DialogueClosing, LogEndDialogue)

                .Register(GameEvents.GameWon, LogWonGame)
                .Register<LossData>(GameEvents.GameFailed, LogFailedGame)
            ;

            // roadnetwork: use a StringBuilder to consider every tile of road as a char, send StringBuilder as a parameter, reset to length zero to reuse
            // check Penguins: example of StringBuilder as the entire event package (don't pull the whole project, just find PenguinAnalytics or etc)


            #if DEVELOPMENT
            m_Debug = true;
            #endif // DEVELOPMENT

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch(),
                ClientLogVersion = CLIENT_LOG_VERSION,
            }, new OGDLog.MemoryConfig(4096, Unsafe.KiB * 64, 256));
            if (!string.IsNullOrEmpty(m_Firebase.ApiKey)) {
                m_Log.UseFirebase(m_Firebase);
            }
            m_Log.SetDebug(m_Debug);
#if UNITY_EDITOR
            if (!m_Testing) {
                m_Log.AddSettings(OGDLog.SettingsFlags.SkipOGDUpload);
                m_Log.SetDebug(false);
            }
#endif // UNITY_EDITOR

            OGDLog.SchedulingConfig sched = OGDLog.SchedulingConfig.Default;
            sched.FlushDelay = 2;

            m_Log.ConfigureScheduling(sched);

            ResetGameState();
        }

        private void SetUserCode(string userCode) {
            Log.Msg("[Analytics, OGDLog] Setting user code: " + userCode);
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch(),
                ClientLogVersion = CLIENT_LOG_VERSION
            });
            m_Log.SetUserId(userCode);
            m_Log.NewEvent("session_start");
        }

        private void OnDestroy() {
            ZavalaGame.Events?.DeregisterAllForContext(this);
            m_Log.Dispose();
        }
        #endregion // Register and Deregister

        #region Game State
        /*
        Game State (across all events)
        county_policies : {
	        �sales� : { policy_choice : int | null, is_locked : bool },
	        �import_subsidy� : { policy_choice : int | null, is_locked : bool },
	        �runoff� : { policy_choice : int | null, is_locked : bool },
	        �cleanup� : { policy_choice : int | null, is_locked : bool },
        }
        // ADD?: avg_framerate : float
        */

        private void ResetGameState() {
            m_CurrentRegionIndex = 0;
            m_CurrentBudget = -1;
            m_CurrentMode = Mode.View;
            m_PhosView = false;
            InitializePolicyChoiceState();
            ResubmitGameState();
        }

        private void ResubmitGameState() {
            m_JsonBuilder.Begin()
                .Field("current_county", EnumLookup.RegionName[m_CurrentRegionIndex])
                .Field("current_money", m_CurrentBudget)
                .Field("map_mode", m_CurrentMode)
                .Field("phosphorus_view_enabled", m_PhosView)
                .BeginObject("county_policies");
            m_CountyPolicies.Append(m_JsonBuilder).EndObject();
            m_Log.GameState(m_JsonBuilder.End());
        }

        private void UpdateRegionState(ushort region) {
            //         current_county : str
            m_CurrentRegionIndex = region;
            ResubmitGameState();
        }

        private void UpdateMoneyState(int budget) {
            //         current_money : int
            m_CurrentBudget = budget;
            ResubmitGameState();
        }

        private void UpdateBuildState(string mode) {
            //         map_mode : enum(VIEW, BUILD, DESTROY)
            m_CurrentMode = mode;
            ResubmitGameState();
        }

        // -1 for "Not Set"
        private void InitializePolicyChoiceState() {
            m_CountyPolicies.sales.policy_choice = -1;
            m_CountyPolicies.sales.is_locked = true;

            m_CountyPolicies.import.policy_choice = -1;
            m_CountyPolicies.import.is_locked = true;

            m_CountyPolicies.runoff.policy_choice = -1;
            m_CountyPolicies.runoff.is_locked = true;

            m_CountyPolicies.cleanup.policy_choice = -1;
            m_CountyPolicies.cleanup.is_locked = true;
        }

        private void UpdatePolicyChoicesState() {
            PolicyBlock p = Game.SharedState.Get<PolicyState>().Policies[m_CurrentRegionIndex];
            int s = (int)PolicyType.SalesTaxPolicy;
            int i = (int)PolicyType.ImportTaxPolicy;
            int r = (int)PolicyType.RunoffPolicy;
            int c = (int)PolicyType.SkimmingPolicy;

            m_CountyPolicies.sales.policy_choice =
                p.EverSet[s] ? (int)p.Map[s] : -1;

            m_CountyPolicies.import.policy_choice = 
                p.EverSet[i] ? (int)p.Map[i] : -1;

            m_CountyPolicies.runoff.policy_choice = 
                p.EverSet[r] ? (int)p.Map[r] : -1;

            m_CountyPolicies.cleanup.policy_choice =
                p.EverSet[c] ? (int)p.Map[c] : -1;

            ResubmitGameState();
        }

        private void UpdatePoliciesLockedState() {
            CardsState cs = Game.SharedState.Get<CardsState>();
            m_CountyPolicies.sales.is_locked = CardsUtility.PolicyIsUnlocked(cs, PolicyType.SalesTaxPolicy);
            m_CountyPolicies.import.is_locked = CardsUtility.PolicyIsUnlocked(cs, PolicyType.ImportTaxPolicy);
            m_CountyPolicies.runoff.is_locked = CardsUtility.PolicyIsUnlocked(cs, PolicyType.RunoffPolicy);
            m_CountyPolicies.cleanup.is_locked = CardsUtility.PolicyIsUnlocked(cs, PolicyType.SkimmingPolicy);

            ResubmitGameState();
        }

        private void UpdatePolicyLockedState(PolicyType type, bool isLocked) {
            switch (type) {
                case PolicyType.SalesTaxPolicy:
                    m_CountyPolicies.sales.is_locked = isLocked;
                    break;
                case PolicyType.RunoffPolicy:
                    m_CountyPolicies.runoff.is_locked = isLocked;
                    break;
                case PolicyType.SkimmingPolicy:
                    m_CountyPolicies.cleanup.is_locked = isLocked;
                    break;
                case PolicyType.ImportTaxPolicy:
                    m_CountyPolicies.import.is_locked = isLocked;
                    break;
                default:
                    break;
            }
            ResubmitGameState();
        }

        private void UpdatePhosState(bool toggle) {
            m_PhosView = toggle;
            ResubmitGameState();
        }

        #endregion // Game State

        #region Log Events


        #region Menu

        private void HandleMenuInteraction(MenuInteractionType type) {
            switch (type) {
                case MenuInteractionType.CreditsButtonClicked:
                    LogClickedCredits();
                    break;
                case MenuInteractionType.CreditsExited:
                    LogExitedCredits();
                    break;
                case MenuInteractionType.NewGameClicked:
                    LogNewGameClicked();
                    break;
                case MenuInteractionType.ResumeGameClicked:
                    LogResumeGameClicked();
                    break;
                case MenuInteractionType.PlayGameClicked:
                    LogPlayClicked();
                    break;
                case MenuInteractionType.ReturnedToMainMenu:
                    LogReturnToMainMenu();
                    break;
                case MenuInteractionType.GameStarted:
                    LogGameStarted();
                    break;
                default: break;
            }
        }



        private void LogClickedCredits() {
            // click_credits { }
            m_Log.Log("click_credits");
        }

        private void LogExitedCredits() {
            // close_credits { }
            m_Log.Log("close_credits");
        }
        private void LogNewGameClicked() {
        // click_new_game { }
            m_Log.Log("click_new_game");  
        }

        private void LogResumeGameClicked() {
            // click_resume_game { }
            m_Log.Log("click_resume_game");
        }


        private void LogFullscreenToggled(bool toggle) {
            // toggle_fullscreen_setting { enable : bool }
            using (var e = m_Log.NewEvent("toggle_fullscreen_setting")) {
                e.Param("enable", toggle);
            }
        }

        private void LogQualityToggled(bool toggle) {
            // toggle_hq_graphics { enable : bool }
            using (var e = m_Log.NewEvent("toggle_hq_graphics")) {
                e.Param("enable", toggle);
            }
        }

        private void LogPlayClicked() {
            // click_play_game { }
            m_Log.Log("click_play_game");
        }

        private void LogReturnToMainMenu() {
            // click_return_main_menu { }
            m_Log.Log("click_return_main_menu");
        }

        private void LogGameStarted() {
            // game_started { music_volume : float, fullscreen_enabled : bool, hq_graphics_enabled : bool, map_state : BuildMap }
            using (var e = m_Log.NewEvent("game_start", m_JsonBuilder)) {
                UserSettings s = Game.SharedState.Get<UserSettings>();
                e.Field("music_volume", s.MusicVolume);
                e.Field("fullscreen_enabled", ScreenUtility.GetFullscreen());
                e.Field("hq_graphics_enabled", s.HighQualityMode);
                e.BeginObject("map_state");
                GenerateMapState(e);
                e.EndObject();
            }
        }

        private void LogVolumeChanged(ZoomVolData data) {
            using (var e = m_Log.NewEvent("set_music_volume")) {
                e.Param("old_volume", data.Start);
                e.Param("new_volume", data.End);
                // e.Param("type", data.UsedWheel ? "SLIDE" : "CLICK")
            }
        }
        #endregion // Menu

        #region Sim

        private void HandleTogglePause(bool paused) {
            if (paused) LogGamePaused();
            else LogGameResumed();
        }

        private void LogGamePaused() {
            // TODO: do we want to log every pause or just player pauses?
            m_Log.Log("pause_game");
        }

        private void LogGameResumed() {
            // TODO: do we want to log every pause or just player pauses?
            m_Log.Log("unpause_game");
        }

        #region Build
        private void LogStartBuildMode() {
            // toggle_map_mode { new_mode : enum(VIEW, BUILD)
            using (var e = m_Log.NewEvent("toggle_map_mode")) {
                e.Param("new_mode", Mode.Build);
            }
            UpdateBuildState(Mode.Build);
        }

        private void LogDisplayBuildMenu() {
            // build_menu_displayed { available_buildings : array[dict] // each item has name and price }
            using (var e = m_Log.NewEvent("build_menu_displayed", m_JsonBuilder)) {
                e.BeginArray("available_buildings");
                ShopUtility.GetShopUnlockData(e);
                e.EndArray();
            }
        }

        private void LogSelectedBuildTool(UserBuildTool tool) {
            // select_building_type { building_type, cost }
            string toolName = EnumLookup.BuildTool[(int)tool];
            using (var e = m_Log.NewEvent("select_building_type")) {
                e.Param("building_type", toolName);
                e.Param("cost", ShopUtility.PriceLookup(tool));
            }
            m_CurrentTool = toolName;

        }

        private void HandleHover(int idx) {
            if (m_CurrentMode == Mode.Destroy) {
                LogHoverDestroy(idx);
            } else {
                LogHoverBuild(idx);
            }
        }

        private void LogHoverBuild(int idx) {
            // hover_build_tile { tile_index, is_valid }
            bool valid = idx >= 0;
            if (!valid) idx *= -1;
            using (var e = m_Log.NewEvent("hover_build_tile")) {
                e.Param("tile_index", idx);
                e.Param("is_valid", valid);
            }
        }

        private void LogBuildInvalid(int idx) {
            // click_build_invalid { tile_index, building_type }
            using (var e = m_Log.NewEvent("click_build_invalid")) {
                e.Param("tile_index", idx);
                e.Param("building_type", m_CurrentTool);
            }
        }

        private void LogBuildClick(int idx) {
            // click_build_invalid { tile_index, building_type }
            using (var e = m_Log.NewEvent("click_build")) {
                e.Param("tile_index", idx);
                e.Param("building_type", m_CurrentTool);
            }
        }

        private void LogDestroyClick(int idx) {
            // click_build_invalid { tile_index, building_type }
            using (var e = m_Log.NewEvent("click_destroy")) {
                e.Param("tile_index", idx);
                e.Param("building_type", m_CurrentTool); // destroy type
            }
        }

        #region Destroy
        private void LogClickedDestroy() {
            //  click_destroy_mode { }
            m_Log.Log("click_destroy_mode");
        }

        private void LogStartDestroy() {
            // enter_destroy_mode { }
            m_Log.Log("enter_destroy_mode");
            m_CurrentMode = Mode.Destroy;
            ResubmitGameState();
        }

        private void LogHoverDestroy(int idx) {
            // hover_destroy_tile { tile_index, building_type : enum(building types) | null }
            using (var e = m_Log.NewEvent("hover_destroy_tile")) {
                e.Param("tile_index", idx);
                // TODO: e.Param("building_type", type | null);
                // How to get this data cheaply?
            }
        }

        private void LogDestroyInvalid(int idx) {
            // click_destroy_invalid { tile_index, terrain_type }
            using (var e = m_Log.NewEvent("click_destroy_invalid")) {
                e.Param("tile_index", idx);
                // TODO: e.Param("terrain_type", type);
                //          water, deep, grass, tree, rock
            }
        }

        private void LogConfirmedDestroy() {
            // click_confirm_destroy { }
            m_Log.Log("click_confirm_destroy");
        }

        private void LogClickedExitDestroy() {
            // click_exit_destroy { }
            m_Log.Log("click_exit_destroy");
        }

       
        private void LogEndDestroy() {
            // exit_destroy_mode { }
            m_Log.Log("exit_destroy_mode");
            m_CurrentMode = Mode.Build;
            ResubmitGameState();
        }

        // TODO: click_undo
        // TODO: use queue/dequeue exclusively for build stuff

        #endregion Destroy

        private void LogQueuedBuilding(ActionCommit commit) {
            //      queue_building { Building, total_cost, funds_remaining }
            using (var e = m_Log.NewEvent("building_queued")) {
                e.Param("building", PrintBuildingParam(commit));
                //          Building : { building_type, tile_index, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }
                int tot = Game.SharedState.Get<ShopState>().RunningCost + commit.Cost;
                e.Param("total_cost", tot);
                e.Param("funds_remaining", m_CurrentBudget - tot);
            }
        }

        private void LogUnqueuedBuilding(ActionCommit commit) {
            //  unqueue_building { Building, total_cost, funds_remaining }
            using (var e = m_Log.NewEvent("building_dequeued")) {
                e.Param("building", PrintBuildingParam(commit));
                //  Building : { building_type, tile_index, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }
                int tot = Game.SharedState.Get<ShopState>().RunningCost - commit.Cost;
                e.Param("total_cost", tot);
                e.Param("funds_remaining", m_CurrentBudget - tot);
            }
        }

        private StringBuilder PrintBuildingParam(ActionCommit commit) {
            using (var psb = PooledStringBuilder.Create()) {
                psb.Builder.Append('{');
                psb.Builder.Append("building_type:").Append(EnumLookup.BuildingType[(int)commit.BuildType]);
                psb.Builder.Append("tile_index:").Append(commit.TileIndex.ToStringLookup());
                psb.Builder.Append("cost:").Append(commit.Cost.ToStringLookup());
                psb.Builder.Append("connections:").Append(commit.FlowMaskSnapshot);
                psb.Builder.Append("build_type:").Append(commit.ActionType.ToString());
                psb.Builder.Append('}');
                return psb.Builder;
            }
        }


        private void LogConfirmedBuild(RingBuffer<CommitChain> chains) {
            // click_execute_build { built_items : array[Building], total_cost, funds_remaining }

            ShopState s = Game.SharedState.Get<ShopState>();
            using (var e = m_Log.NewEvent("execute_build_queue")) {
                e.Param("built_items", GetCommitChainData(chains));
                e.Param("total_cost", s.RunningCost);
                e.Param("funds_remaining", m_CurrentBudget - s.RunningCost);
            }
        }

        private StringBuilder GetCommitChainData(RingBuffer<CommitChain> chains) {
            if (chains == null) return null;
            using (var psb = PooledStringBuilder.Create()) {
                psb.Builder.Append('[');
                foreach (CommitChain chain in chains) {
                    foreach (ActionCommit commit in chain.Chain) {
                        psb.Builder.Append('{');
                        psb.Builder.Append("building_type:").Append(EnumLookup.BuildingType[(int)commit.BuildType]);
                        psb.Builder.Append("tile_index:").Append(commit.TileIndex.ToStringLookup());
                        psb.Builder.Append('}');
                    }
                }
                psb.Builder.Append(']');
                return psb.Builder;
            }

        }

        private void LogEndBuildMode() {
            // toggle_map_mode { new_mode : enum(VIEW, BUILD)
            using (var e = m_Log.NewEvent("toggle_map_mode")) {
                e.Param("new_mode", Mode.View);
            }
            m_CurrentMode = Mode.View;
        }


        #endregion // Build


        private void LogRegionChanged(ushort newRegion) {
            // NOT IN SCHEMA
            // ADD?: county_changed { county_name }
            string county = EnumLookup.RegionName[newRegion];
            using (var e = m_Log.NewEvent("county_changed")) {
                e.Param("county_name", county);
            }
            UpdateRegionState(newRegion);
        }

        private void LogZoom(ZoomVolData info) {
            // change_zoom { start_zoom: float, end_zoom : float, with_mousewheel : bool }
            using (var e = m_Log.NewEvent("change_zoom")) {
                e.Param("start_zoom", info.Start);
                e.Param("end_zoom", info.End);
                e.Param("zoom_type", info.UsedWheel ? "SCROLL" : "BUTTON");
            }
        }

        private void LogRegionUnlocked(ushort newRegion) {
            // county_unlocked { county_name, county_state : CountyBuildMap }
            m_JsonBuilder.Begin()
                .Field("county_name", EnumLookup.RegionName[newRegion])
                .BeginArray("county_state");
            GenerateCountyState(m_JsonBuilder, newRegion).EndArray();
            m_Log.Log("county_unlocked", m_JsonBuilder.End());
        }

        private JsonBuilder GenerateCountyState(JsonBuilder json, ushort regionIndex) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            SimWorldState world = Game.SharedState.Get<SimWorldState>();

            using (PooledList<OccupiesTile> tiles = PooledList<OccupiesTile>.Create()) {
                foreach (var tile in Game.Components.ComponentsOfType<OccupiesTile>()) {
                    if (tile.RegionIndex == regionIndex && IsBuildingLoggable(tile.Type)) {
                        tiles.Add(tile);
                    }
                }

                foreach (var tile in tiles) {
                    WriteOccupiesTile(tile, network, grid, json);
                }

                foreach(var buildingSpawn in world.Spawns.QueuedBuildings) {
                    if (buildingSpawn.RegionIndex == regionIndex && IsBuildingLoggable(buildingSpawn.Data.Type)) {
                        WriteQueuedBuildingSpawn(buildingSpawn, network, grid, json);
                    }
                }
            }

            return json;
        }
        private JsonBuilder GenerateMapState(JsonBuilder json) {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            using (PooledList<OccupiesTile> tiles = PooledList<OccupiesTile>.Create()) {
                // turn enumerator into a list so we can sort it
                tiles.AddRange(Game.Components.ComponentsOfType<OccupiesTile>());
                tiles.Sort((a, b) => a.RegionIndex - b.RegionIndex);

                int region = -1;
                for (int ot = 0; ot < tiles.Count; ot++) {
                    // if we've advanced to a new region, start a new "array"
                    OccupiesTile tile = tiles[ot];
                    if (IsBuildingLoggable(tile.Type)) {
                        if (tile.RegionIndex > region) {
                            if (region >= 0) {
                                json.EndArray();
                            }
                            region = tile.RegionIndex;
                            json.BeginArray(EnumLookup.RegionName[region]);
                        }

                        WriteOccupiesTile(tile, network, grid, json);
                    }
                }
                if (region >= 0) {
                    json.EndArray();
                }
            }

            return json;
        }

        static private void WriteOccupiesTile(OccupiesTile tile, RoadNetwork network, SimGridState grid, JsonBuilder json) {
            // print in current region
            int idx = tile.TileIndex;
            json.BeginObject()
                .Field("index", idx)
                .Field("height", grid.Terrain.Height[idx]);
            TerrainFlags flags = grid.Terrain.Info[idx].Flags;
            if ((flags & TerrainFlags.IsWater) != 0) {
                if ((flags & TerrainFlags.NonBuildable) != 0) {
                    json.Field("type", "DEEP_WATER");
                } else {
                    json.Field("type", "WATER");
                }
            } else {
                json.Field("type", "LAND");
            }

            json.Field("building", EnumLookup.BuildingType[(int) tile.Type]);

            json.BeginArray("connections");
            foreach (var dir in network.Roads.Info[idx].FlowMask) {
                json.Item(EnumLookup.TileDirection[(int) dir]);
            }
            json.EndArray();

            json.EndObject();
        }

        static private void WriteQueuedBuildingSpawn(SpawnRecord<BuildingSpawnData> building, RoadNetwork network, SimGridState grid, JsonBuilder json) {
            // print in current region
            int idx = building.TileIndex;
            json.BeginObject()
                .Field("index", idx)
                .Field("height", grid.Terrain.Height[idx]);
            TerrainFlags flags = grid.Terrain.Info[idx].Flags;
            if ((flags & TerrainFlags.IsWater) != 0) {
                if ((flags & TerrainFlags.NonBuildable) != 0) {
                    json.Field("type", "DEEP_WATER");
                } else {
                    json.Field("type", "WATER");
                }
            } else {
                json.Field("type", "LAND");
            }

            json.Field("building", EnumLookup.BuildingType[(int) building.Data.Type]);

            json.BeginArray("connections");
            foreach (var dir in network.Roads.Info[idx].FlowMask) {
                json.Item(EnumLookup.TileDirection[(int) dir]);
            }
            json.EndArray();

            json.EndObject();
        }

        private void LogBuildingUnlocked(UserBuildTool type) {
            // unlock_building_type { building_type }
            using (var e = m_Log.NewEvent("building_type_unlocked")) {
                e.Param("building_type", EnumLookup.BuildingType[(int)type]);
            }
        }

        private void LogExportDepot(ExportDepotData data) {
            // export_depot_spawned { depot_id, tile_index }
            using (var e = m_Log.NewEvent("export_depot_spawned")) {
                e.Param("depot_id", data.Id);
                e.Param("tile_index", data.TileIndex);
            }
        }
        
        static private bool IsBuildingLoggable(BuildingType type) {
            switch (type) {
                case BuildingType.None:
                case BuildingType.SkimmerLocation:
                    return false;

                default:
                    return true;
            }
        }

        #region Advisor/Policy
        private void LogAdvisorButtonClicked(AdvisorType type) {
            // click_open_policy_category { category : enum(ECON, ECOLOGY) }
            using (var e = m_Log.NewEvent("click_open_policy_category")) {
                e.Param("category", EnumLookup.AdvisorType[(int)type]);
            }
        }

        private void LogPolicyOpened(PolicyType type, bool fromTaskbar) {
            // click_open_policy { policy: enum(...), from_taskbar : bool }
            using (var e = m_Log.NewEvent("click_open_policy")) {
                e.Param("policy", EnumLookup.PolicyType[(int)type]);
                e.Param("from_taskbar", fromTaskbar);
            }
        }

        private void LogHoverPolicy(PolicyData data) { // policy, level, hint text
            // hover_policy_card { choice_number, choice_name, choice_text }
            using (var e = m_Log.NewEvent("hover_policy_card")) {
                e.Param("policy", EnumLookup.PolicyType[(int)data.Type]);
                e.Param("choice_name", Loc.Find("cards." + EnumLookup.PolicyType[(int)data.Type] + "." + EnumLookup.PolicyLevel[data.Level]));
                e.Param("choice_number", data.Level);
                e.Param("choice_text", data.HintText);
            }
        }

        private void LogPolicySet(PolicyData data) { // policy and level
            using (var e = m_Log.NewEvent("select_policy_card")) {
                e.Param("policy", EnumLookup.PolicyType[(int)data.Type]);
                e.Param("choice_name", Loc.Find("cards." + EnumLookup.PolicyType[(int)data.Type] + "." + EnumLookup.PolicyLevel[data.Level]));
                e.Param("choice_number", data.Level);
                e.Param("choice_text", data.HintText);
            }
            UpdatePolicyChoicesState();
        }
        private void LogPolicyUnlocked(PolicyType type) {
            // unlock_policy { policy_name }
            using (var e = m_Log.NewEvent("policy_unlocked")) {
                e.Param("policy_name", EnumLookup.PolicyType[(int)type]);
            }
            UpdatePolicyLockedState(type, false);
        }

        private void LogUnlockView(bool isEcon) {
            //unlock_view { view_type : enum(PHOSPHORUS_VIEW, ECONOMY_VIEW) }
            using (var e = m_Log.NewEvent("view_unlocked")) {
                if (isEcon) {
                    e.Param("view_type", "ECONOMY_VIEW");
                } else {
                    e.Param("view_type", "PHOSPHORUS_VIEW");
                }
            }
        }

        private void LogMarketToggled(bool toggle) {
            // open_economy_view { }
            // close_economy_view { }
            if (toggle) {
                m_Log.Log("open_economy_view");
            } else {
                m_Log.Log("close_economy_view");
            }
        }

        private void LogPhosToggled(bool toggle) {
            // ADD?: open_phosphorus_view { }
            // ADD?: close_phosphorus_view { }
            if (toggle) {
                m_Log.Log("open_phosphorus_view");
            } else {
                m_Log.Log("close_phosphorus_view");
            }
            UpdatePhosState(toggle);
        }

        private void HandleSkimmer(SkimmerData data) {
            if (data.IsAppearing) {
                LogSkimmerAppear(data);
            } else {
                LogSkimmerDisappear(data);
            }
        }

        private void LogSkimmerAppear(SkimmerData data) {
            // skimmer_appear { tile_index, is_dredger : bool }
            using (var e = m_Log.NewEvent("skimmer_appeared")) {
                e.Param("tile_index", data.TileIndex);
                e.Param("is_dredger", data.IsDredger);
            }
        }

        private void LogSkimmerDisappear(SkimmerData data) {
            // skimmer_disappear { tile_index, is_dredger : bool }
            using (var e = m_Log.NewEvent("skimmer_disappeared")) {
                e.Param("tile_index", data.TileIndex);
                e.Param("is_dredger", data.IsDredger);
            }
        }

        #endregion // Advisor/Policy

        #region Alert

        private void LogAlertDisplayed(AlertData data) {
            // ADD? alert_displayed { alert_type, tile_index }
            using (var e = m_Log.NewEvent("local_alert_displayed")) {
                e.Param("alert_type", EnumLookup.AlertType[(int) data.Type]);
                e.Param("tile_index", data.TileIndex);
            }
            if (data.Type == EventActorAlertType.Bloom) {
                LogBloomAlert(data.TileIndex);
            }
        }

        private void LogAlertClicked(AlertData data) {
            // ADD? click_alert { alert_type, tile_index, node_id }
            using (var e = m_Log.NewEvent("click_local_alert")) {
                e.Param("alert_type", EnumLookup.AlertType[(int) data.Type]);
                e.Param("tile_index", data.TileIndex);
            }
        }

        private void LogBloomAlert(int idx) {
            // bloom_alert { tile_index, phosphorus_value }
            int phos = Game.SharedState.Get<SimPhosphorusState>().Phosphorus.CurrentState()[idx].Count;
            using (var e = m_Log.NewEvent("bloom_alert")) {
                e.Param("tile_index", idx);
                e.Param("phosphorus_value", phos);
            }
        }

        private void LogGlobalAlertDisplayed(AlertData data) {
            // global_alert_displayed { alert_type, node_id }
            using (var e = m_Log.NewEvent("global_alert_displayed")) {
                e.Param("alert_type", EnumLookup.AlertType[(int) data.Type]);
            }
        }

        private void LogGlobalAlertClicked(AlertData data) {
            // click_global_alert  { alert_type, node_id }
            using (var e = m_Log.NewEvent("click_global_alert")) {
                e.Param("alert_type", EnumLookup.AlertType[(int) data.Type]);
            }
        }

        #endregion // Alert

        #region Inspector
        private void LogInspectBuilding(BuildingLocation data) {
            // click_inspect_building { building_type : enum(GATE, CITY, DAIRY_FARM, GRAIN_FARM, STORAGE, PROCESSOR, EXPORT_DEPOT), building_id, tile_index : int // index in the county map }
            using (var e = m_Log.NewEvent("click_inspect_building")) {
                e.Param("building_type", EnumLookup.BuildingType[(int)data.Type]);
                e.Param("building_id", data.Id);
                e.Param("tile_index", data.TileIndex);
            }

            // save this building for inspector dismiss and inspector displayed events
            m_InspectingBuilding = data;
        }

        private void LogDismissInspector() {
            // dismiss_building_inspector: { building_type, building_id, tile_index }
            using (var e = m_Log.NewEvent("dismiss_building_inspector")) {
                e.Param("building_type", EnumLookup.BuildingType[(int)m_InspectingBuilding.Type]);
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
            }
            m_InspectingBuilding = new BuildingLocation() {
                Type = BuildingType.None,
                Id = "",
                TileIndex = -1
            };
        }

        private void LogCityInspectorDisplayed(CityData data) {
            LogCommonInspectorDisplayed();

            /*
            city_inspector_displayed : {
                building_id
                tile_index
                city_name : str
                population : enum(RISING, FALLING, STABLE)
                water : enum(GOOD, OK, BAD)
                milk : enum(PLENTY, ENOUGH, NOT_ENOUGH)
            }
            */

            using (var e = m_Log.NewEvent("city_inspector_displayed"))
            {
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
                e.Param("city_name", data.Name);
                // TODO: convert enum tostring to string lookup array?
                e.Param("population", EnumLookup.Get(data.Population));
                e.Param("water", EnumLookup.Get(data.Water));
                e.Param("milk", EnumLookup.Get(data.Milk));
            }
        }

        private void LogGrainFarmInspectorDisplayed(GrainFarmData data) {
            LogCommonInspectorDisplayed();

            /*
            grain_inspector_displayed {
                building_id
                tile_index
                grain_tab : [{
                    is_active : bool 
                    farm_name : str
                    farm_county : str
                    base_price
                    shipping_cost
                    total_profit
                }],
                fertilizer_tab : [{
                    is_active : bool
                    farm_name : str
                    farm_county : str
                    base_price
                    shipping_cost
                    sales_policy : int$
                    import_policy : int$
                    total_profit
                }]
            }
             */

            m_JsonBuilder.Begin()
                .Field("building_id", m_InspectingBuilding.Id)
                .Field("tile_index", m_InspectingBuilding.TileIndex)
                .BeginArray("grain_tab");
            foreach(var d in data.GrainTab) {
                m_JsonBuilder.BeginObject();
                d.ToJson(m_JsonBuilder).EndObject();
            }
            m_JsonBuilder.EndArray()
                .BeginArray("fertilizer_tab");
            foreach (var d in data.FertilizerTab) {
                m_JsonBuilder.BeginObject();
                d.ToJson(m_JsonBuilder).EndObject();
            }
            m_JsonBuilder.EndArray();
            m_Log.Log("grain_inspector_displayed", m_JsonBuilder.End());
        }

        private void LogDairyFarmInspectorDisplayed(DairyFarmData data) {
            LogCommonInspectorDisplayed();

            /*
            dairy_inspector_displayed {
                building_id
                tile_index
                �grain_tab� : [{
                    is_active : bool
                    farm_name : str
                    farm_county : str
                    base_price
                    shipping_cost
                    sales_policy : int$
                    import_policy : int$
                    total_profit
                }],
                �dairy_tab� : [{
                    is_active : bool
                    farm_name : str
                    farm_county : str | null
                    base_price
                    total_profit
                }],
                �fertilizer_tab� : [{
                    is_active : bool
                    farm_name : str
                    farm_county : str | null
                    base_price
                    shipping_cost
                    runoff_fine : int$
                    total_profit
                }]
            }
             */

            m_JsonBuilder.Begin()
                .Field("building_id", m_InspectingBuilding.Id)
                .Field("tile_index", m_InspectingBuilding.TileIndex)
                .BeginArray("grain_tab");
            foreach (var d in data.GrainTab) {
                m_JsonBuilder.BeginObject();
                d.ToJson(m_JsonBuilder).EndObject();
            }
            m_JsonBuilder.EndArray()
                .BeginArray("dairy_tab");
            foreach (var d in data.DairyTab) {
                m_JsonBuilder.BeginObject();
                d.ToJson(m_JsonBuilder).EndObject();
            }
            m_JsonBuilder.EndArray()
                .BeginArray("fertilizer_tab");
            foreach (var d in data.FertilizerTab) {
                m_JsonBuilder.BeginObject();
                d.ToJson(m_JsonBuilder).EndObject();
            }
            m_JsonBuilder.EndArray();
            m_Log.Log("dairy_inspector_displayed", m_JsonBuilder.End());
        }

        private void LogStorageInspectorDisplayed(StorageData data) {
            LogCommonInspectorDisplayed();

            // storage_inspector_displayed : { building_id, tile_index, units_filled : int }
            using (var e = m_Log.NewEvent("storage_inspector_displayed")) {
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
                e.Param("units_filled", data.UnitsFilled);
            }
        }

        /// <summary>
        /// Logging event that either precedes the more specific InspectorDisplayed events (city, grain farm, dairy farm, storage),
        /// or stands alone for the generic InspectorDisplayed event (digester, toll booth, export depot)
        /// </summary>
        private void LogCommonInspectorDisplayed() {
            // building_inspector_displayed : { building_type, building_id, tile_index}
            using (var e = m_Log.NewEvent("building_inspector_displayed")) {
                e.Param("building_type", EnumLookup.BuildingType[(int)m_InspectingBuilding.Type]);
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
            }
        }

        private void LogClickInspectorTab(string name) {
            // click_inspector_tab { tab_name }
            using (var e = m_Log.NewEvent("click_inspector_tab")) {
                e.Param("tab_name", name);
            }
        }

        #endregion //Inspector

        private void HandleAlgaeChanged(AlgaeData data) {
            if (data.IsGrowing) {
                LogStartGrowAlgae(data);
            } else {
                LogEndGrowAlgae(data);
            }
        }
        private void LogStartGrowAlgae(AlgaeData data) {
            // algae_growth_begin { tile_index, phosphorus_value, algae_percent }
            using (var e = m_Log.NewEvent("algae_growth_begin")) {
                e.Param("tile_index", data.TileIndex);
                e.Param("phosphorus_value", data.Phosphorus);
                e.Param("algae_percent", data.Algae);
            }
        }

        private void LogEndGrowAlgae(AlgaeData data) {
            // algae_growth_end { tile_index, phosphorus_value, algae_percent }
            using (var e = m_Log.NewEvent("algae_growth_end")) {
                e.Param("tile_index", data.TileIndex);
                e.Param("phosphorus_value", data.Phosphorus);
                e.Param("algae_percent", data.Algae);
            }
        }

        #endregion // Sim

        #region Dialogue
        private void HandleDialogueAdvanced() {
            if (m_CurrentCutscene < 0) {
                LogDialogueAdvanced();
            } else {
                LogCutscenePageAdvanced();
            }
        }

        private void HandleDialogueDisplayed(DialogueLineData data) {
            m_CurrentLine = data;
            if (m_CurrentCutscene < 0) {
                LogDialogueDisplayed();
            } else {
                LogCutscenePageDisplayed();
            }
        }


        private void LogStartDialogue(ScriptNodeData data) {
            //dialog_start { skippable: bool, node_id}
            using (var e = m_Log.NewEvent("dialogue_start")) {
                e.Param("skippable", data.Skippable);
                e.Param("node_id", data.NodeId);
            }
        }

        private void LogDialogueAdvanced() {
            // click_next_character_line { character_name, character_type, line_text }
            using (var e = m_Log.NewEvent("click_next_character_line")) {
                // is it necessary to re-log the line we just logged?
                e.Param("character_name", m_CurrentLine.CharName);
                e.Param("character_type", m_CurrentLine.CharTitle);
                e.Param("line_text", m_CurrentLine.Text);
            }

        }

        private void LogDialogueDisplayed() {
            //character_line_displayed { character_name, character_type, line_text }
            using (var e = m_Log.NewEvent("character_line_displayed")) {
                e.Param("character_name", m_CurrentLine.CharName);
                e.Param("character_type", m_CurrentLine.CharTitle);
                e.Param("line_text", m_CurrentLine.Text);
            }
        }

        private void LogEndDialogue(ScriptNodeData data) {
            //dialog_end { skippable, node_id }
            using (var e = m_Log.NewEvent("dialogue_end")) {
                e.Param("skippable", data.Skippable);
                e.Param("node_id", data.NodeId);
            }
        }

        private void LogStartCutscene(int id) {
            // cutscene_started { cutscene_id }
            m_CurrentCutscene = (short)id;
            m_CurrentCutscenePage = 0;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
            using (var e = m_Log.NewEvent("cutscene_start")) {
                e.Param("cutscene_id", id);
            }

        }

        private void LogCutscenePageAdvanced() {
            //click_cutscene_next { cutscene_id, page_id }
            using (var e = m_Log.NewEvent("click_cutscene_next")) {
                e.Param("cutscene_id", m_CurrentCutscene);
                e.Param("page_id", m_CurrentCutscenePage);
            }
            m_CurrentCutscenePage++;
        }

        private void LogCutscenePageDisplayed() {
            // cutscene_page_displayed { cutscene_id, page_id, frame_ids : List[str], page_text }
            using (var e = m_Log.NewEvent("cutscene_page_displayed")) {
                e.Param("cutscene_id", m_CurrentCutscene);
                e.Param("page_id", m_CurrentCutscenePage);
                // TODO: e.Param("frame_ids", [str]);
                e.Param("page_text", m_CurrentLine.Text);
            }
        }

        private void LogEndCutscene() {
            // cutscene_end { cutscene_id }
            using (var e = m_Log.NewEvent("cutscene_end")) {
                e.Param("cutscene_id", m_CurrentCutscene);
            }
            m_CurrentCutscene = -1;
            m_CurrentCutscenePage = -1;
        }
        #endregion // Dialogue

        #region End
        private void LogWonGame() {
            // win_game { }
            m_JsonBuilder.Begin();
            m_JsonBuilder.BeginObject("map_state");
            GenerateMapState(m_JsonBuilder);
            m_JsonBuilder.EndObject();
            m_Log.Log("win_game", m_JsonBuilder.End());
        }
        
        private void LogFailedGame(LossData data) {
            // lose_game { lose_condition: enum(CITY_FAILED, TOO_MANY_BLOOMS, OUT_OF_MONEY), county_id, county_name
            m_JsonBuilder.Begin();
            m_JsonBuilder.Field("lose_condition", data.EndType);
            m_JsonBuilder.Field("county_id", data.Region);
            m_JsonBuilder.Field("county_name", EnumLookup.RegionName[data.Region]);
            m_JsonBuilder.BeginObject("map_state");
            GenerateMapState(m_JsonBuilder);
            m_JsonBuilder.EndObject();
            m_Log.Log("lose_game", m_JsonBuilder.End());
        }

        #endregion // End

        #endregion // Log Events

    }

}

