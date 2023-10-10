using System;
using BeauUtil;
using FieldDay.SharedState;
using UnityEngine.Events;
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

    
    public enum AdvisorType : byte {
        None,
        Ecology,
        Economy
    }
}