#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauUtil;
using FieldDay;
using UnityEngine;
using System;
using BeauUtil.Debugger;
using FieldDay.Rendering;
using Zavala.Advisor;
using Zavala.Economy;
using Zavala.Building;
using EasyAssetStreaming;

namespace Zavala.Data {

    /*
    "SUB-TYPES"
    BuildTile : List[enum(// all the types of object)]
    CountyBuildMap { List[BuildTiles] }
    BuildMap : { List[CountyBuildMap] }
    Building : { building_type, tile_id, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }

     */
    public enum MenuInteractionType : byte {
        CreditsButtonClicked,
        CreditsExited,
        NewGameClicked,
        ResumeGameClicked,
        PlayGameClicked,
        ReturnedToMainMenu,
    }

    public enum ModeInteraction : byte {
        Clicked, // clicked button to start
        Started, // started
        Confirmed, // clicked button to finish
        Exited, // clicked button to cancel
        Ended // ended mode
    }

    public struct ZoomData {
        public float Start;
        public float End;
        public bool UsedWheel;
        public ZoomData(float start, float end, bool usedWheel) {
            Start = start;
            End = end;
            UsedWheel = usedWheel;
        }
    }

    public struct BuildingData {
        public BuildingType Type;
        public string Id;
        public int TileIndex;
        public BuildingData(BuildingType type, string id, int tileIndex) {
            Type = type;
            Id = id;
            TileIndex = tileIndex;
        }
    }

    public class AnalyticsService : MonoBehaviour {
        private const ushort CLIENT_LOG_VERSION = 1;

        private static class Mode {
            public static readonly string View = "VIEW";
            public static readonly string Build = "BUILD";
            public static readonly string Destroy = "DESTROY";
        }

        #region Inspector
        [SerializeField, Required] private string m_AppId = "ZAVALA";
        [SerializeField, Required] private string m_AppVersion = "1.0";
        // TODO: set up firebase consts in inspector
        [SerializeField] private FirebaseConsts m_Firebase = default;

        #endregion // Inspector

        private enum ViewMode : byte {
            VIEW,
            BUILD,
            DESTROY
        }

        private enum CityStatus : byte {
            GOOD,
            OKAY,
            BAD
        }

        private struct PoliciesLog {
            
        }
        private enum AdvisorType : byte {
            ECONOMY,
            ECOLOGY
        }

        private struct PolicyLevelLog {
            public int Level;
            public bool IsLocked;
        }

        #region Logging Variables

        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        [NonSerialized] private float m_MusicVolume;
        [NonSerialized] private bool m_IsFullscreen;
        [NonSerialized] private bool m_IsQuality;

        [NonSerialized] private string m_CurrentRegionId;
        [NonSerialized] private int m_CurrentBudget;
        [NonSerialized] private string m_CurrentMode;
        [NonSerialized] private string m_CurrentTool;
        [NonSerialized] private BuildingData m_InspectingBuilding;

        [NonSerialized] private ushort m_CurrentCutscene;
        [NonSerialized] private ushort m_CurrentCutscenePage;
        // current policies
        // dialogue character opened (or none)
        // dialogue phrase displaying (or none)


        #endregion // Logging Variables

        #region Register and Deregister

        private void Start() {


            // TODO: register the events for data logging
            ZavalaGame.Events
                // Main Menu
                .Register<MenuInteractionType>(GameEvents.MainMenuInteraction, HandleMenuInteraction)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode)
                .Register<bool>(GameEvents.FullscreenToggled, LogFullscreenToggled)
                .Register<bool>(GameEvents.QualityToggled, LogQualityToggled)
                // Sim Controls
                .Register<AdvisorType>(GameEvents.AdvisorButtonClicked, LogAdvisorButtonClicked)
                .Register<bool>(GameEvents.ToggleEconomyView, LogMarketToggled)
                .Register<bool>(GameEvents.TogglePhosphorusView, LogPhosToggled)
                .Register(GameEvents.SimPaused, LogGamePaused)
                .Register(GameEvents.SimResumed, LogGameResumed)
                // TODO: Condense blueprint events with BlueprintInteractionType?
                .Register(GameEvents.BlueprintModeStarted, LogStartBuildMode)
                .Register(GameEvents.DestroyModeClicked, LogClickedDestroy)
                .Register(GameEvents.DestroyModeStarted, LogStartDestroy)
                .Register(GameEvents.DestroyModeConfirmed, LogConfirmedDestroy)
                .Register(GameEvents.DestroyModeExited, LogClickedExitDestroy)
                .Register(GameEvents.DestroyModeEnded, LogEndDestroy)
                .Register(GameEvents.BlueprintModeEnded, LogEndBuildMode)
                .Register<UserBuildTool>(GameEvents.BuildToolSelected, LogSelectedBuildTool)

                .Register<PolicyType>(GameEvents.PolicyTypeUnlocked, LogPolicyUnlocked)
                .Register<PolicyType>(GameEvents.PolicySlotClicked, (PolicyType type) => LogPolicyOpened(type, false))
                .Register<PolicyType>(GameEvents.PolicyButtonClicked, (PolicyType type) => LogPolicyOpened(type, true))
                .Register<bool>(GameEvents.AdvisorLensUnlocked, LogUnlockView)
                // ADD?                
                .Register<ushort>(GameEvents.RegionSwitched, LogRegionChanged)
                // TODO: county state .Register<string>(GameEvents.RegionUnlocked, LogRegionUnlocked)
                .Register<ZoomData>(GameEvents.SimZoomChanged, LogZoom)
                // Inspect
                .Register<BuildingData>(GameEvents.InspectorOpened, LogInspectBuilding)
                // Dialogue
                .Register<int>(GameEvents.CutsceneStarted, LogStartCutscene)
                .Register(GameEvents.CutsceneEnded, LogEndCutscene)
            ;

            // roadnetwork: use a StringBuilder to consider every tile of road as a char, send StringBuilder as a parameter, reset to length zero to reuse
            // check Penguins: example of StringBuilder as the entire event package (don't pull the whole project, just find PenguinAnalytics or etc)


            #if DEVELOPMENT
            m_Debug = true;
            #endif // DEVELOPMENT

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = CLIENT_LOG_VERSION
            });
            m_Log.UseFirebase(m_Firebase);
            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode) {
            Log.Msg("[Analytics] Setting user code: " + userCode);
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch(),
                ClientLogVersion = CLIENT_LOG_VERSION
            });
            m_Log.SetUserId(userCode);
        }

        private void OnDestroy() {
            Game.Events?.DeregisterAllForContext(this);
            m_Log.Dispose();
        }
        #endregion // Register and Deregister

        #region Game State
        /*
        Game State (across all events)
        county_policies : {
	        “sales“ : { policy_choice : int | null, is_locked : bool },
	        “import_subsidy“ : { policy_choice : int | null, is_locked : bool },
	        “runoff” : { policy_choice : int | null, is_locked : bool },
	        “cleanup” : { policy_choice : int | null, is_locked : bool },
        }
        // ADD?: avg_framerate : float
        */

        private void ResetGameState() {

        }
        private void UpdateRegionState(string county) {
            //         current_county : str
            m_CurrentRegionId = county;
            using (var s = m_Log.OpenGameState()) {
                s.Param("current_county", county);
            }
        }

        public void UpdateMoneyState(int budget) {
            //         current_money : int
            m_CurrentBudget = budget;
            using (var s = m_Log.OpenGameState()) {
                s.Param("current_money", budget);
            }
        }

        private void UpdateBuildState(string mode) {
            //         map_mode : enum(VIEW, BUILD, DESTROY)
            m_CurrentMode = mode;
            using (var s = m_Log.OpenGameState()) {
                s.Param("map_mode", mode.ToString());
            }
        }

        private void UpdatePhosState(bool toggle) {
            using (var s = m_Log.OpenGameState()) {
                s.Param("phosphorus_view_enabled", toggle);
            }
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
            using (var e = m_Log.NewEvent("click_return_main_menu")) {
                // TODO: e.Param("music_volume", );
                e.Param("fullscreen_enabled", ScreenUtility.GetFullscreen());
                e.Param("hq_graphics_enabled", Game.SharedState.Get<UserSettings>().HighQualityMode);
                // TODO: e.Param("map_state",);
            }
        }
        #endregion // Menu

        #region Sim

        private void HandleTogglePause(bool paused) {
            if (paused) LogGamePaused();
            else LogGameResumed();
        }

        private void LogGamePaused() {
            m_Log.Log("pause_game");
        }

        private void LogGameResumed() {
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
            using (var e = m_Log.NewEvent("build_menu_displayed")) {
                // TODO: e.Param("available_buildings", [building: Type, price: int]);
            }
        }

        private void LogSelectedBuildTool(UserBuildTool tool) {
            // select_building_type { building_type, cost }
            string toolName = tool.ToString();
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
            // hover_build_tile { tile_id, is_valid }
            using (var e = m_Log.NewEvent("hover_build_tile")) {
                e.Param("tile_id", idx);
                // TODO: e.Param("is_valid", bool);
            }
        }

        private void LogBuildInvalid(int idx) {
            // click_build_invalid { tile_id, building_type }
            using (var e = m_Log.NewEvent("click_build_invalid")) {
                e.Param("tile_id", idx);
                e.Param("building_type", m_CurrentTool);
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
            using (var s = m_Log.OpenGameState()) {
                s.Param("map_mode", Mode.Destroy);
            }
        }

        private void LogHoverDestroy(int idx) {
            // hover_destroy_tile { tile_id, building_type : enum(building types) | null }
            using (var e = m_Log.NewEvent("hover_destroy_tile")) {
                e.Param("tile_id", idx);
                // TODO: e.Param("building_type", type | null);
            }
        }

        private void LogDestroyInvalid() {
            // click_destroy_invalid { tile_id, terrain_type }
            using (var e = m_Log.NewEvent("click_destroy_invalid")) {
                // TODO: e.Param("tile_id", int);
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
            using (var s = m_Log.OpenGameState()) {
                s.Param("map_mode", Mode.Build);
            }
        }

        #endregion Destroy

        /*
        unlock_building_type { building_type }
         */
        private void LogQueuedBuilding() {
            //      queue_building { Building, total_cost, funds_remaining }
            using (var e = m_Log.NewEvent("queue_building")) {
                // TODO: e.Param("Building", ???);
                //          Building : { building_type, tile_id, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }
                // TODO: e.Param("total_cost", int);
                // TODO: e.Param("funds_remaining", int);
            }
        }

        private void LogUnqueuedBuilding() {
            //      unqueue_building { Building, total_cost, funds_remaining }
            using (var e = m_Log.NewEvent("unqueue_building")) {
                // TODO: e.Param("Building", ???);
                //          Building : { building_type, tile_id, cost, connections : array[bool], build_type : enum(BUILD, DESTROY) }
                // TODO: e.Param("total_cost", int);
                // TODO: e.Param("funds_remaining", int);
            }
        }


        private void LogConfirmBuild() {
            //      click_execute_build { built_items : array[Building], total_cost, funds_remaining }
            using (var e = m_Log.NewEvent("click_execute_build")) {
                // TODO: e.Param("built_items", [BuildingType type, int tileIndex])
                //      should include building type and tile index?
                // TODO: e.Param("total_cost", int);
                // TODO: e.Param("funds_remaining", int);
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
            string county = ((RegionId)newRegion).ToString();
            using (var e = m_Log.NewEvent("county_changed")) {
                e.Param("county_name", county);
            }
            UpdateRegionState(county);
        }

        private void LogZoom(ZoomData info) {
            // change_zoom { start_zoom: float, end_zoom : float, with_mousewheel : bool }
            using (var e = m_Log.NewEvent("change_zoom")) {
                e.Param("start_zoom", info.Start);
                e.Param("end_zoom", info.End);
                e.Param("with_mousewheel", info.UsedWheel);
            }
        }

        private void LogRegionUnlocked(string newRegion) {
            // county_unlock { county_name, county_state : CountyBuildMap }
            using (var e = m_Log.NewEvent("county_unlock")) {
                e.Param("county_name", newRegion);
                // TODO: e.Param("county_state", );
            }
        }

        private void LogExportDepot() {
            // export_depot_spawned { depot_id, tile_id }
            using (var e = m_Log.NewEvent("export_depot_spawned")) {
                // TODO: e.Param("depot_id", string)
                // TODO: e.Param("tile_id", int)
            }
        }

        #region Advisor/Policy
        private void LogAdvisorButtonClicked(AdvisorType type) {
            // click_open_policy_category { category : enum(ECON, ECOLOGY) }
            using (var e = m_Log.NewEvent("click_open_policy_category")) {
                e.Param("category", type.ToString());
            }
        }

        private void LogPolicyOpened(PolicyType type, bool fromTaskbar) {
            // click_open_policy { policy: enum(...), from_taskbar : bool }
            using (var e = m_Log.NewEvent("click_open_policy")) {
                e.Param("policy", type.ToString());
                e.Param("from_taskbar", fromTaskbar);
            }

        }

        private void LogPolicyUnlocked(PolicyType type) {
            // unlock_policy { policy_name }
            using (var e = m_Log.NewEvent("unlock_policy")) {
                e.Param("policy_name", type.ToString());
            }
        }

        private void LogUnlockView(bool isEcon) {
            //unlock_view { view_type : enum(PHOSPHORUS_VIEW, ECONOMY_VIEW) }
            using (var e = m_Log.NewEvent("unlock_view")) {
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

        private void LogSkimmerAppear() {
            // skimmer_appear { tile_id, is_dredger : bool }
            using (var e = m_Log.NewEvent("skimmer_appear")) {
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("is_dredger", bool)
            }
        }

        private void LogSkimmerDisappear() {
            // skimmer_disappear { tile_id, is_dredger : bool }
            using (var e = m_Log.NewEvent("skimmer_disappear")) {
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("is_dredger", bool)
            }
        }

        #endregion // Advisor/Policy

        #region Alert

        private void LogAlertDisplayed() {
            // ADD? alert_displayed { alert_type, tile_id }
            using (var e = m_Log.NewEvent("click_alert")) {
                // TODO: e.Param("alert_type", EventActorAlertType)
                // TODO: e.Param("tile_id", int)
            }

        }

        private void LogAlertClicked() {
            // ADD? click_alert { alert_type, tile_id, node_id }
            using (var e = m_Log.NewEvent("click_alert")) {
                // TODO: e.Param("alert_type", EventActorAlertType)
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("node_id", int)
            }
        }

        private void LogBloomAlert() {
            // bloom_alert { tile_id, phosphorus_value }
            using (var e = m_Log.NewEvent("bloom_alert")) {
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("phsophorus_value", int)
            }
        }

        private void LogGlobalAlertDisplayed() {
            // global_alert_displayed { alert_id, node_id }
            using (var e = m_Log.NewEvent("global_alert_displayed")) {
                // TODO: e.Param("actor_id", string)
                // TODO: e.Param("node_id", int)
            }
        }

        private void LogGlobalAlertClicked() {
            // click_global_alert  { alert_id, node_id }
            using (var e = m_Log.NewEvent("click_global_alert")) {
                // TODO: e.Param("actor_id", string)
                // TODO: e.Param("node_id", int)
            }
        }

        #endregion // Alert

        #region Inspector
        private void LogInspectBuilding(BuildingData data) {
            // click_inspect_building { building_type : enum(GATE, CITY, DAIRY_FARM, GRAIN_FARM, STORAGE, PROCESSOR, EXPORT_DEPOT), building_id, tile_index : int // index in the county map }
            using (var e = m_Log.NewEvent("click_inspect_building")) {
                e.Param("building_type", data.Type.ToString());
                e.Param("building_id", data.Id);
                e.Param("tile_index", data.TileIndex);
            }

        }

        private void LogDismissInspector() {
            // dismiss_building_inspector: { building_type, building_id, tile_index }
            using (var e = m_Log.NewEvent("dismiss_building_inspector")) {
                e.Param("building_type", m_InspectingBuilding.Type.ToString());
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
            }
            m_InspectingBuilding = new BuildingData() {
                Type = BuildingType.None,
                Id = "",
                TileIndex = -1
            };
        }

        private void LogInspectorDisplayed() {
            // building_inspector_displayed : { building_type, building_id, tile_index}
            using (var e = m_Log.NewEvent("building_inspector_displayed")) {
                e.Param("building_type", m_InspectingBuilding.Type.ToString());
                e.Param("building_id", m_InspectingBuilding.Id);
                e.Param("tile_index", m_InspectingBuilding.TileIndex);
            }

            switch (m_InspectingBuilding.Type) {
                case BuildingType.City:
                    /* TODO:
                    city_inspector_displayed : {
                        building_id
                        tile_index
                        city_name : str
                        population : enum(RISING, FALLING, STABLE)
                        water : enum(GOOD, OK, BAD)
                        milk : enum(PLENTY, ENOUGH, NOT_ENOUGH)
                    }
                    */
                    break;
                case BuildingType.GrainFarm:
                    /* TODO:
                    grain_inspector_displayed {
                        building_id
                        tile_index
                        “grain_tab” : [{
                            is_active : bool
                            farm_name : str
                            farm_county : str
                            base_price
                            shipping_cost
                            total_profit
                        }],
                        “fertilizer_tab” : [{
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
                    break;
                case BuildingType.DairyFarm:
                    /* TODO:
                    dairy_inspector_displayed {
                        building_id
                        tile_index
                        “grain_tab” : [{
                            is_active : bool
                            farm_name : str
                            farm_county : str
                            base_price
                            shipping_cost
                            sales_policy : int$
                            import_policy : int$
                            total_profit
                        }],
                        “dairy_tab” : [{
                            is_active : bool
                            farm_name : str
                            farm_county : str | null
                            base_price
                            total_profit
                        }],
                        “fertilizer_tab” : [{
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
                    break;
                case BuildingType.Storage:
                    /* TODO:
                    storage_inspector_displayed : { building_id, tile_index, units_filled : int }
                    */
                    break;
                default:
                    break;
            }

        }

        private void LogClickInspectorTab(string name) {
            // click_inspector_tab { tab_name }
            using (var e = m_Log.NewEvent("click_inspector_tab")) {
                e.Param("tab_name", name);
            }
        }

        #endregion //Inspector

        private void LogStartGrowAlgae() {
            // algae_growth_begin { tile_id, phosphorus_value, algae_percent }
            using (var e = m_Log.NewEvent("algae_growth_begin")) {
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("phosphorus_value", int)
                // TODO: e.Param("algae_percent", float)
            }
        }

        private void LogEndGrowAlgae() {
            // algae_growth_end { tile_id, phosphorus_value, algae_percent }
            using (var e = m_Log.NewEvent("algae_growth_end")) {
                // TODO: e.Param("tile_id", int)
                // TODO: e.Param("phosphorus_value", int)
                // TODO: e.Param("algae_percent", float)
            }
        }

        #endregion // Sim

        #region Dialogue
        private void HandleDialogueAdvanced(bool isCutscene) {
            // TODO:
            if (isCutscene) {
                LogCutscenePageAdvanced();
            } else {
                LogDialogueAdvanced();
            }
        }

        private void HandleDialogueDisplayed(bool isCutscene) {
            if (isCutscene) {
                LogCutscenePageDisplayed();
            } else {
                LogDialogueDisplayed();
            }
        }


        private void LogStartDialogue() {
            //dialog_start { skippable: bool, node_id}
            using (var e = m_Log.NewEvent("dialogue_start")) {
                // TODO: e.Param("skippable", bool);
                // TODO: e.Param("node_id", string);
            }
        }

        private void LogDialogueAdvanced() {
            // click_next_character_line { character_name, character_type, line_text }
            using (var e = m_Log.NewEvent("click_next_character_line")) {
                // TODO: e.Param("character_name", string);
                // TODO: e.Param("character_type", string);
                // TODO: e.Param("line_text", string);
            }

        }

        private void LogDialogueDisplayed() {
            //character_line_displayed { character_name, character_type, line_text }
            using (var e = m_Log.NewEvent("character_line_displayed")) {
                // TODO: e.Param("character_name", string);
                // TODO: e.Param("character_type", string);
                // TODO: e.Param("line_text", string);
            }
        }

        private void LogEndDialogue() {
            //dialog_end { skippable, node_id }
            using (var e = m_Log.NewEvent("dialogue_end")) {
                // TODO: e.Param("skippable", bool);
                // TODO: e.Param("node_id", string);
            }
        }

        private void LogStartCutscene(int id) {
            // cutscene_started { cutscene_id }
            m_CurrentCutscene = (ushort)id;
            m_CurrentCutscenePage = 1;
            using (var e = m_Log.NewEvent("cutscene_started")) {
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
                // TODO: e.Param("page_text");
            }
        }

        private void LogEndCutscene() {
            // cutscene_end { cutscene_id }
            using (var e = m_Log.NewEvent("cutscene_end")) {
                e.Param("cutscene_id", m_CurrentCutscene);
            }
            m_CurrentCutscene = 0;
            m_CurrentCutscenePage = 0;
        }
        #endregion // Dialogue

        #region End
        private void LogWonGame() {
            // win_game { }
            m_Log.Log("win_game");
        }
        
        private void LogLostGame() {
            // lose_game { lose_condition: enum(CITY_FAILED, TOO_MANY_BLOOMS, OUT_OF_MONEY), county_id, county_name
            using (var e = m_Log.NewEvent("lose_game")) {
                // TODO: e.Param("lose_condition", cond);
                // TODO: e.Param("county_id", name);
                // TODO: e.Param("county_name", name);
            }
        }
        #endregion // End

        #endregion // Log Events

    }

}

