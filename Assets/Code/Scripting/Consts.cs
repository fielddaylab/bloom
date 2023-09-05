using BeauUtil;
using BeauUtil.Variants;

namespace Zavala
{
    static public class GameTriggers
    {
        static public readonly StringHash32 GameBooted = "GameBooted";
        static public readonly StringHash32 AlertExamined = "AlertExamined";
    }

    static public class GameAlerts
    {
        // Localization Ids
        static public readonly string BasePath = "alerts.";
        static public readonly string ExcessRunoffLocId = "excess-runoff";
        static public readonly string BloomLocId = "bloom";
        static public readonly string DieOffLocId = "die-off";
        static public readonly string CritImbalanceLocId = "crit-imbalance";
        static public readonly string UnusedDigesterLocId = "unused-digester";
        static public readonly string DecliningPopLocId = "declining-pop";
        static public readonly string SellingLossLocId = "selling-loss";

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