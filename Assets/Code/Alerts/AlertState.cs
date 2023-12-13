using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using Zavala.Data;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.Alerts {

    public struct AutoAlertCondition {
        public EventActorAlertType Alert;
        public int RegionIndex;
    }
    public class AlertState : SharedStateComponent, IRegistrationCallbacks {
        // would this fit inside another shared state component?
        [NonSerialized] public HashSet<EventActorAlertType> PausedAlertTypes;
        [NonSerialized] public HashSet<AutoAlertCondition> AutoTriggerAlerts;

        void IRegistrationCallbacks.OnRegister() {
            PausedAlertTypes = new HashSet<EventActorAlertType>((int) EventActorAlertType.COUNT);
            AutoTriggerAlerts = new HashSet<AutoAlertCondition>(5); 
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

    }
}