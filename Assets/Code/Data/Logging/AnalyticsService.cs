#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using UnityEngine;
using System;
using BeauUtil.Debugger;

namespace Zavala.Data {
    public class AnalyticsService : MonoBehaviour {
        private const ushort CLIENT_LOG_VERSION = 1;

        #region Inspector
        [SerializeField, Required] private string m_AppId = "ZAVALA";
        [SerializeField, Required] private string m_AppVersion = "1.0";
        // FirebaseConsts is present in other AnalyticsServices but isn't in FieldDay here?
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

        #region Logging Variables

        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        [NonSerialized] private string m_CurrentRegionId;
        [NonSerialized] private int m_CurrentBudget;
        // current policies
        // dialogue character opened (or none)
        // dialogue phrase displaying (or none)


        #endregion // Logging Variables

        #region Register and Deregister

        private void Start() {


            // TODO: register the events for data logging
            ZavalaGame.Events
                // Main Menu
                .Register(GameEvents.CreditsButtonClicked, LogClickedCredits)
                .Register(GameEvents.CreditsExited, LogExitedCredits)
                .Register(GameEvents.NewGameClicked, LogNewGameClicked)
                .Register(GameEvents.ResumeGameClicked, LogResumeGameClicked)
                .Register(GameEvents.ReturnedToMainMenu, LogReturnToMainMenu)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode)
                .Register<bool>(GameEvents.FullscreenToggled, LogFullscreenToggled)
                .Register<bool>(GameEvents.QualityToggled, LogQualityToggled)
                // Sim Controls
                .Register(GameEvents.SimPaused, LogGamePaused)
                .Register(GameEvents.SimResumed, LogGameResumed)
                .Register(GameEvents.BlueprintModeStarted, LogStartBuildMode)
                .Register(GameEvents.BlueprintModeEnded, LogEndBuildMode)
                .Register<ushort>(GameEvents.RegionSwitched, LogRegionChanged)
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

        #region Log Events


        private void LogClickedCredits() {
            using (var e = m_Log.NewEvent("click_credits")) {
            }
        }

        private void LogExitedCredits() {
            using (var e = m_Log.NewEvent("close_credits")) {
            }
        }
        private void LogNewGameClicked() {
            using (var e = m_Log.NewEvent("click_new_game")) {
            }
        }

        private void LogResumeGameClicked() {
            using (var e = m_Log.NewEvent("click_resume_game")) {
            }
        }


        private void LogFullscreenToggled(bool toggle) {
            using (var e = m_Log.NewEvent("toggle_fullscreen_setting")) {
                e.Param("enable", toggle);
            }
        }

        private void LogQualityToggled(bool toggle) {
            using (var e = m_Log.NewEvent("toggle_hq_graphics")) {
                e.Param("enable", toggle);
            }
        }

        private void LogReturnToMainMenu() {
            using (var e = m_Log.NewEvent("click_return_main_menu")) {
            }
        }



        private void LogStartCutscene() {

        }

        private void LogEndCutscene() {

        }


        private void LogGamePaused() {
            using (var e = m_Log.NewEvent("pause_game")) {
            }
        }

        private void LogGameResumed() {
            using (var e = m_Log.NewEvent("unpause_game")) {
            }

        }

        private void LogStartBuildMode() {
            using (var e = m_Log.NewEvent("toggle_map_mode")) {
                e.Param("new_mode", "BUILD");
            }
        }

        private void LogEndBuildMode() {
            using (var e = m_Log.NewEvent("toggle_map_mode")) {
                e.Param("new_mode", "VIEW");
            }
        }

        private void LogRegionChanged(ushort newRegion) {
            using (var e = m_Log.NewEvent("county_changed")) {
                e.Param("county_name", ((RegionId)newRegion).ToString());
            }
        }


        #endregion // Log Events

    }

}

/*
// click_credits { }
// close_credits { }
// click_new_game { }
// click_resume_game { }
// toggle_fullscreen_setting { enable : bool }
// toggle_hq_graphics { enable : bool }
click_play_game { }
click_return_main_menu { }
game_started { music_volume : float, fullscreen_enabled : bool, hq_graphics_enabled : bool, map_state : BuildMap }
// ?: county_changed { county_name }
county_unlock { county_name, county_state : CountyBuildMap }
cutscene_started { cutscene_id }
cutscene_end { cutscene_id }
cutscene_page_displayed { cutscene_id, page_id, frame_ids : List[str], page_text }
click_cutscene_next { cutscene_id, page_id }
dialog_start { skippable : bool, node_id}
dialog_end { skippable, node_id }
character_line_displayed { character_name, character_type, line_text }
click_next_character_line { character_name, character_type, line_text }
open_economy_view { }
close_economy_view { }
// toggle_map_mode { new_mode : enum(VIEW, BUILD)
enter_destroy_mode { }
exit_destroy_mode { }
global_alert_displayed { alert_id, node_id }
click_global_alert  { alert_id, node_id }
change_zoom { start_zoom : float, end_zoom : float, with_mousewheel : bool }
click_open_policy { policy : enum(...), from_taskbar : bool }
click_open_policy_category { category : enum(ECON, ECOLOGY) }
hover_policy_card { choice_number, choice_name, choice_text }
click_set_policy_choice { choice_number, choice_name }
// pause_game { }
// unpause_game { }
build_menu_displayed { available_buildings : array[dict] // each item has name and price }
click_inspect_building { building_type : enum(GATE, CITY, DAIRY_FARM, GRAIN_FARM, STORAGE, PROCESSOR, EXPORT_DEPOT), building_id, tile_index : int // index in the county map }
dismiss_building_inspector : { building_type, building_id, tile_index }
building_inspector_displayed : { building_type, building_id, tile_index}
storage_inspector_displayed : { building_id, tile_index, units_filled : int }
city_inspector_displayed : {
	building_id
	tile_index
	city_name : str
	population : enum(RISING, FALLING, STABLE)
	water : enum(GOOD, OK, BAD)
	milk : enum(PLENTY, ENOUGH, NOT_ENOUGH)
}
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
click_inspector_tab { tab_name }
queue_building { Building, total_cost, funds_remaining }
unqueue_building { Building, total_cost, funds_remaining } 
click_execute_build { built_items : array[ Building ], total_cost, funds_remaining }
click_destroy_mode { }
click_confirm_destroy { }
click_exit_destroy { }
select_building_type { building_type, cost }
hover_build_tile { tile_id, is_valid }
hover_destroy_tile { tile_id, building_type : enum(building types) | null }
click_build_invalid { tile_id, building_type }
click_destroy_invalid { tile_id, terrain_type }
unlock_building_type { building_type }
unlock_policy { policy_name }
algae_growth_begin { tile_id, phosphorus_value, algae_percent }
algae_growth_end { tile_id, phosphorus_value, algae_percent }
bloom_alert { tile_id, phosphorus_value }
skimmer_appear { tile_id, is_dredger : bool }
skimmer_disappear { tile_id, is_dredger : bool }
unlock_view { view_type : enum(PHOSPHORUS_VIEW, ECONOMY_VIEW) }
win_game { }
lose_game { lose_condition : enum(CITY_FAILED, TOO_MANY_BLOOMS, OUT_OF_MONEY), county_id, county_name }
export_depot_spawned { depot_id, tile_id }

 */ 

