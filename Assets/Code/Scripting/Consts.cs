using UnityEngine;
using BeauUtil;
using BeauUtil.Variants;
using Zavala.Scripting;

namespace Zavala
{
    static public class ImportSettings {
        static public readonly int HEIGHT_SCALE = 50;
    }

    static public class RenderSettings {
        static public readonly Quaternion CameraPerspective = Quaternion.Euler(50, 45, 0);
    }

    static public class GameEvents
    {

       static public readonly StringHash32 MainMenuInteraction = "menu:interact";

        static public readonly StringHash32 FullscreenToggled = "menu:fullscreen-clicked";
        static public readonly StringHash32 QualityToggled = "menu:hq-toggled";
        static public readonly StringHash32 ProfileStarting = "menu:starting";

        static public readonly StringHash32 PolicySlotClicked = "advisors:policy-slot-clicked";
        static public readonly StringHash32 PolicyTypeUnlocked = "advisors:policy-type-unlocked";
        static public readonly StringHash32 AdvisorButtonClicked = "advisors:advisor-clicked";
        static public readonly StringHash32 PolicyButtonClicked = "advisors:policy-clicked";
        static public readonly StringHash32 PolicyHover = "advisors:policy-hover";
        static public readonly StringHash32 PolicySet = "advisors:policy-set";
        static public readonly StringHash32 AdvisorLensUnlocked = "advisors:lens-unlocked";
        static public readonly StringHash32 SkimmerChanged = "sim:skimmer-changed";

        static public readonly StringHash32 MarketCycleTickCompleted = "sim:market-cycle-completed";
        static public readonly StringHash32 ForceMarketPrioritiesRebuild = "sim:force-market-priorities-rebuild";
        static public readonly StringHash32 MarketPrioritiesRebuilt = "sim:market-priorities-rebuilt";
        static public readonly StringHash32 BudgetRefreshed = "sim:budget-refreshed";
        static public readonly StringHash32 ToggleEconomyView = "sim:toggle-economy-view";
        static public readonly StringHash32 TogglePhosphorusView = "sim:toggle-phosphorus-view";

        static public readonly StringHash32 RegionSwitched = "sim:region-switched";
        static public readonly StringHash32 RegionUnlocked = "sim:region-unlocked";
        static public readonly StringHash32 ExportDepotUnlocked = "sim:export-unlocked";
        static public readonly StringHash32 BuildToolUnlocked = "sim:building-unlocked";

        static public readonly StringHash32 SimPaused = "sim:paused";
        static public readonly StringHash32 SimResumed = "sim:resumed";
        static public readonly StringHash32 SimUserPaused = "sim:user-paused";
        static public readonly StringHash32 SimUserResumed = "sim:user-resumed";
        static public readonly StringHash32 SimZoomChanged = "sim:zoom";

        static public readonly StringHash32 SimAlgaeChanged = "sim:algae-change";

        static public readonly StringHash32 GlobalAlertAppeared = "sim:global-appeared";
        static public readonly StringHash32 GlobalAlertClicked = "sim:global-clicked";
        static public readonly StringHash32 AlertAppeared = "sim:alert-appeared";
        static public readonly StringHash32 AlertClicked = "sim:alert-clicked";

        static public readonly StringHash32 BlueprintModeStarted = "blueprint:started";
        static public readonly StringHash32 BlueprintModeEnded = "blueprint:ended";
        static public readonly StringHash32 BuildConfirmed = "blueprint:confirmed";
        static public readonly StringHash32 BuildToolSelected = "blueprint:build-tool-selected";
        static public readonly StringHash32 BuildToolDeselected = "blueprint:build-tool-deselected";
        static public readonly StringHash32 HoverTile = "blueprint:hover-tile";

        static public readonly StringHash32 DestroyModeClicked = "destroy:clicked";
        static public readonly StringHash32 DestroyModeStarted = "destroy:started";
        static public readonly StringHash32 DestroyModeConfirmed = "destroy:clicked";
        static public readonly StringHash32 DestroyModeExited = "destroy:cancel";
        static public readonly StringHash32 DestroyModeEnded = "destroy:ended";

        static public readonly StringHash32 InspectorOpened = "inspector:opened";
        static public readonly StringHash32 InspectorClosed = "inspector:closed";
        static public readonly StringHash32 InspectorTabClicked = "inspector:tab";

        static public readonly StringHash32 GenericInspectorDisplayed = "inspector:generic-displayed";
        static public readonly StringHash32 CityInspectorDisplayed = "inspector:city-displayed";
        static public readonly StringHash32 GrainFarmInspectorDisplayed = "inspector:grain-displayed";
        static public readonly StringHash32 DairyFarmInspectorDisplayed = "inspector:dairy-displayed";
        static public readonly StringHash32 StorageInspectorDisplayed = "inspector:storage-displayed";

        static public readonly StringHash32 CutsceneStarted = "dialogue:cutscene-started";
        static public readonly StringHash32 CutsceneEnded = "dialogue:cutscene-ended";
        static public readonly StringHash32 DialogueStarted = "dialogue:started";
        static public readonly StringHash32 DialogueDisplayed = "dialogue:displayed";
        static public readonly StringHash32 DialogueAdvanced = "dialogue:advanced";
        static public readonly StringHash32 DialogueClosing = "dialogue:closing";

        static public readonly StringHash32 GameLoaded = "game:loaded";
        static public readonly StringHash32 GameFailed = "game:failed";
        static public readonly StringHash32 GameWon = "game:won";

    }

    static public class GameTriggers
    {
        static public readonly StringHash32 GameBooted = "GameBooted";
        static public readonly StringHash32 TutorialSkipped = "TutorialSkipped";
        static public readonly StringHash32 AlertExamined = "AlertExamined";
        static public readonly StringHash32 AdvisorOpened = "AdvisorOpened";
        static public readonly StringHash32 AdvisorClosed = "AdvisorClosed";
        static public readonly StringHash32 PolicySet = "PolicySet";
        static public readonly StringHash32 RegionReady = "RegionReady";
        static public readonly StringHash32 RegionUnlocked = "RegionUnlocked";
        static public readonly StringHash32 RegionReachedAge = "RegionReachedAge";
        static public readonly StringHash32 LetSat = "LetSat";
        static public readonly StringHash32 BlueprintModeEntered = "BlueprintModeEntered";
        static public readonly StringHash32 BlueprintModeExited = "BlueprintModeExited";
        static public readonly StringHash32 BuildButtonPressed = "BuildButtonPressed";
        static public readonly StringHash32 PlayerBuiltBuilding = "PlayerBuiltBuilding";
        static public readonly StringHash32 FarmConnection = "FarmConnection";
        static public readonly StringHash32 CityConnection = "CityConnection";
        static public readonly StringHash32 ExternalImport = "ExternalImport";
        static public readonly StringHash32 ResourceSent = "ResourceSent";
        static public readonly StringHash32 ManureSold = "ManureSold";
        static public readonly StringHash32 InternalBlimpSent = "InternalBlimpSent";
        static public readonly StringHash32 InternalBlimpReceived = "InternalBlimpReceived";

        static public readonly StringHash32 GameFailed = "GameFailed";
        static public readonly StringHash32 GameCompleted = "GameCompleted";

    }

    static public class GameAlerts
    {
        // alert ids
        private const string Bloom = "bloom";
        private const string ExcessRunoff = "excess-runoff";
        private const string DieOff = "die-off";
        private const string CritImbalance = "crit-imbalance";
        private const string UnusedDigester = "unused-digester";
        private const string DecliningPop = "declining-pop";
        private const string SellingLoss = "selling-loss";
        private const string Disconnected = "disconnected";
        private const string DialogueBubble = "dialogue-bubble";

        // Localization Ids
        private const string BaseLocPath = "alerts.";
        private const string ExcessRunoffLocId = BaseLocPath + ExcessRunoff;
        private const string BloomLocId = BaseLocPath + Bloom;
        private const string DieOffLocId = BaseLocPath + DieOff;
        private const string CritImbalanceLocId = BaseLocPath + CritImbalance;
        private const string UnusedDigesterLocId = BaseLocPath + UnusedDigester;
        private const string DecliningPopLocId = BaseLocPath + DecliningPop;
        private const string SellingLossLocId = BaseLocPath + SellingLoss;
        private const string DisconnectedLocId = BaseLocPath + Disconnected;
        private const string DialogueBubbleLocId = BaseLocPath + DialogueBubble;


        static private readonly string[] AlertTypeToString = new string[] {
            null, Bloom, ExcessRunoff, DieOff, CritImbalance, UnusedDigester, DecliningPop, SellingLoss, Disconnected, DialogueBubble
        };

        static private readonly StringHash32[] AlertTypeToHash = new StringHash32[] {
            null, Bloom, ExcessRunoff, DieOff, CritImbalance, UnusedDigester, DecliningPop, SellingLoss, Disconnected, DialogueBubble
        };

        static public string GetAlertName(EventActorAlertType alert) {
            return AlertTypeToString[(int) alert];
        }

        static public StringHash32 GetAlertId(EventActorAlertType alert) {
            return AlertTypeToHash[(int) alert];
        }

        static public NamedVariant GetAlertTypeArgument(EventActorAlertType alert) {
            return new NamedVariant("alertType", AlertTypeToHash[(int) alert]);
        }

        static public string GetLocalizedName(EventActorAlertType type) {
            TextId lookup = default;
            switch (type) {
                case EventActorAlertType.Bloom: {
                    lookup = BloomLocId; break;
                }
                case EventActorAlertType.ExcessRunoff: {
                    lookup = ExcessRunoffLocId; break;
                }
                case EventActorAlertType.DieOff: {
                    lookup = DieOffLocId; break;
                }
                case EventActorAlertType.CritImbalance: {
                    lookup = CritImbalanceLocId; break;
                }
                case EventActorAlertType.UnusedDigester: {
                    lookup = UnusedDigesterLocId; break;
                }
                case EventActorAlertType.DecliningPop: {
                    lookup = DecliningPopLocId; break;
                }
                case EventActorAlertType.SellingLoss: {
                    lookup = SellingLossLocId; break;
                }
                case EventActorAlertType.Disconnected: {
                    lookup = DisconnectedLocId; break;
                }
                case EventActorAlertType.Dialogue: {
                    lookup = DialogueBubbleLocId; break;
                }
            }
            return Loc.Find(lookup);
        }
    }
}