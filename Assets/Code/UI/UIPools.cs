using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.UI
{
    public class UIPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class AlertPools : SerializablePool<UIAlert> { }

        #endregion // Types

        public AlertPools Alerts;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Alerts.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}