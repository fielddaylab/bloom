using BeauUtil;
using BeauUtil.Variants;
using Zavala.Scripting;

namespace Zavala
{
    static public class ImportSettings {
        static public int HEIGHT_SCALE = 50;
    }

    static public class GameEvents
    {
        static public readonly StringHash32 PolicySlotClicked = "advisors:policy-slot-clicked";
        static public readonly StringHash32 PolicyCardSelected = "advisors:policy-card-selected";
        static public readonly StringHash32 AdvisorButtonClicked = "advisors:advisor-clicked";
        static public readonly StringHash32 MarketCycleTickCompleted = "sim:market-cycle-completed";
        static public readonly StringHash32 RegionSwitched = "sim:region-switched";

        static public readonly StringHash32 SimPaused = "sim:paused";
        static public readonly StringHash32 SimResumed = "sim:resumed";

        static public readonly StringHash32 BlueprintModeStarted = "blueprint:started";
        static public readonly StringHash32 BlueprintModeEnded = "blueprint:ended";
        static public readonly StringHash32 BuildToolSelected = "blueprint:build-tool-selected";
        static public readonly StringHash32 BuildToolDeselected = "blueprint:build-tool-deselected";

        static public readonly StringHash32 DestroyModeStarted = "destroy:started";
        static public readonly StringHash32 DestroyModeEnded = "destroy:ended";

        static public readonly StringHash32 DialogueClosing = "dialog:closing";
    }

    static public class GameTriggers
    {
        static public readonly StringHash32 GameBooted = "GameBooted";
        static public readonly StringHash32 AlertExamined = "AlertExamined";
        static public readonly StringHash32 AdvisorOpened = "AdvisorOpened";
        static public readonly StringHash32 AdvisorClosed = "AdvisorClosed";
        static public readonly StringHash32 RegionUnlocked = "RegionUnlocked";
        static public readonly StringHash32 LetSat = "LetSat";
        static public readonly StringHash32 BuildButtonPressed = "BuildButtonPressed";
        static public readonly StringHash32 FarmConnection = "FarmConnection";
        static public readonly StringHash32 CityConnection = "CityConnection";
        static public readonly StringHash32 ExternalImport = "ExternalImport";
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

        // Localization Ids
        private const string BaseLocPath = "alerts.";
        private const string ExcessRunoffLocId = BaseLocPath + ExcessRunoff;
        private const string BloomLocId = BaseLocPath + Bloom;
        private const string DieOffLocId = BaseLocPath + DieOff;
        private const string CritImbalanceLocId = BaseLocPath + CritImbalance;
        private const string UnusedDigesterLocId = BaseLocPath + UnusedDigester;
        private const string DecliningPopLocId = BaseLocPath + DecliningPop;
        private const string SellingLossLocId = BaseLocPath + SellingLoss;

        static private readonly string[] AlertTypeToString = new string[] {
            null, Bloom, ExcessRunoff, DieOff, CritImbalance, UnusedDigester, DecliningPop, SellingLoss
        };

        static private readonly StringHash32[] AlertTypeToHash = new StringHash32[] {
            null, Bloom, ExcessRunoff, DieOff, CritImbalance, UnusedDigester, DecliningPop, SellingLoss
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
            }
            return Loc.Find(lookup);
        }
    }
}