using System;
using FieldDay.SharedState;

namespace Zavala.Advisor {
    public class AdvisorState : SharedStateComponent {
        [NonSerialized] public AdvisorType ActiveAdvisor = AdvisorType.None;
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