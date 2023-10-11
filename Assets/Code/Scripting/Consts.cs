using BeauUtil;
using BeauUtil.Variants;

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
    }

    static public class GameAlerts
    {
        // Localization Ids
        static private readonly string BasePath = "alerts.";
        static private readonly string ExcessRunoffLocId = "excess-runoff";
        static private readonly string BloomLocId = "bloom";
        static private readonly string DieOffLocId = "die-off";
        static private readonly string CritImbalanceLocId = "crit-imbalance";
        static private readonly string UnusedDigesterLocId = "unused-digester";
        static private readonly string DecliningPopLocId = "declining-pop";
        static private readonly string SellingLossLocId = "selling-loss";

        // Alerts
        static public readonly StringHash32 Bloom = BloomLocId;
        static public readonly StringHash32 ExcessRunoff = ExcessRunoffLocId;
        static public readonly StringHash32 DieOff = DieOffLocId;
        static public readonly StringHash32 CritImbalance = CritImbalanceLocId;
        static public readonly StringHash32 UnusedDigester = UnusedDigesterLocId;
        static public readonly StringHash32 DecliningPop = DecliningPopLocId;
        static public readonly StringHash32 SellingLoss = SellingLossLocId;

        static public string ConvertArgToLocText(Variant arg) {
            string locText = "";
            string eventName = "";

            // TODO: If there's another way around this that serves the same function as arg.ToDebugString(), I'm all for it
            // i.e. eventName = arg.ToDebugString() would be the simplest assignment approach

            if (arg == Bloom)                   { eventName = BloomLocId; }
            else if (arg == ExcessRunoff)       { eventName = ExcessRunoffLocId; }
            else if (arg == DieOff)             { eventName = DieOffLocId; }
            else if (arg == CritImbalance)      { eventName = CritImbalanceLocId; }
            else if (arg == UnusedDigester)     { eventName = UnusedDigesterLocId; }
            else if (arg == DecliningPop)       { eventName = DecliningPopLocId; }
            else if (arg == SellingLoss)        { eventName = SellingLossLocId; }

            locText = Loc.Find(GameAlerts.BasePath + "" + eventName);

            return locText;
        }
    }
}