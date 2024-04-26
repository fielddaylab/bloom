using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Scripting;

namespace Zavala.UI
{
    public class UIPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class AlertPools : SerializablePool<UIAlert> { }
        [Serializable] public class PolicyBoxPopupPools : SerializablePool<UIPolicyBoxPopup> { }

        #endregion // Types

        public AlertPools Alerts;
        public PolicyBoxPopupPools PolicyBoxPopups;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Alerts.TryInitialize(PoolRoot);
            PolicyBoxPopups.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}