using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using Zavala.Cards;

namespace Zavala.Advisor {
    public class AdvisorState : SharedStateComponent {
        [NonSerialized] public AdvisorType ActiveAdvisor = AdvisorType.None;
        public CastableEvent<AdvisorType> AdvisorButtonClicked = new CastableEvent<AdvisorType>();

        public AdvisorType UpdateAdvisor(AdvisorType type) {
            ActiveAdvisor = type;
            return ActiveAdvisor;
        }
    }

    public static class AdvisorUtility {
        [LeafMember("UnlockAdvisorModule")]
        public static void UnlockModule(AdvisorType type) {
            ZavalaGame.SharedState.Get<ScriptRuntimeState>().DefaultDialogue.GetModule(type).Unlock();
        }

        [DebugMenuFactory]
        static private DMInfo AdvisorModuleUnlockUtility() {
            DMInfo info = new DMInfo("Advisor Modules");
            info.AddButton("Unlock Pips", () => {
                UnlockModule(AdvisorType.Ecology);
            }, () => Game.SharedState.TryGet(out AdvisorState a));

            info.AddButton("Unlock Graphs", () => {
                UnlockModule(AdvisorType.Economy);
            }, () => Game.SharedState.TryGet(out AdvisorState a));
            return info;
        }
    }


    public enum AdvisorType : byte {
        None,
        Ecology,
        Economy
    }
}