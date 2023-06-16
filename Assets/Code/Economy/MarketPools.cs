using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Economy {
    public class MarketPools : SharedStateComponent, IRegistrationCallbacks {
        #region Types

        [Serializable] public class TruckPool : SerializablePool<RequestFulfiller> { }

        #endregion // Types

        public TruckPool Trucks;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Trucks.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }
}