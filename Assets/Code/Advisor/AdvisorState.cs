using System;
using BeauUtil;
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
    }


    public enum AdvisorType : byte {
        None,
        Ecology,
        Economy
    }
}