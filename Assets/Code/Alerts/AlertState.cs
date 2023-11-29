using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using Zavala.Data;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.Alerts {
    public class AlertState : SharedStateComponent, IRegistrationCallbacks {
        // would this fit inside another shared state component?
        [NonSerialized] public HashSet<EventActorAlertType> PausedAlertTypes;

        void IRegistrationCallbacks.OnRegister() {
            PausedAlertTypes = new HashSet<EventActorAlertType>((int) EventActorAlertType.COUNT);
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

    }
}