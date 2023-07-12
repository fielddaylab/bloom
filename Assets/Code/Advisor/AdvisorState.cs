using System;
using FieldDay.SharedState;

namespace Zavala.Advisor {
    public class AdvisorState : SharedStateComponent {
        [NonSerialized] public AdvisorType ActiveAdvisor = AdvisorType.Environment;
    }

    public enum AdvisorType : byte {
        None,
        Environment,
        Economy
    }
}