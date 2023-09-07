using System;
using FieldDay.SharedState;
using UnityEngine.Events;
using Zavala.Cards;

namespace Zavala.Advisor {
    public class AdvisorState : SharedStateComponent {
        [NonSerialized] public AdvisorType ActiveAdvisor = AdvisorType.None;
        public UnityEvent<AdvisorType> AdvisorButtonClicked = new UnityEvent<AdvisorType>();

        public AdvisorType UpdateAdvisor(AdvisorType type) {
            ActiveAdvisor = type;
            return ActiveAdvisor;
        }
    }

    
    public enum AdvisorType : byte {
        None,
        Environment,
        Economy
    }
}