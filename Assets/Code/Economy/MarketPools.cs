using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Economy {
    public class MarketPools : SharedStateComponent, IRegistrationCallbacks {
        #region Types

        [Serializable] public class TruckPool : SerializablePool<RequestFulfiller> { }

        [Serializable] public class ExternalAirshipPool : SerializablePool<RequestFulfiller> { } // The vehicle transporting external commercial phosphorus

        [Serializable] public class InternalAirshipPool : SerializablePool<RequestFulfiller> { } // The vehicle transporting internal digested phosphorus

        [Serializable] public class ParcelPool : SerializablePool<RequestFulfiller> { } // The packages airships drop

        #endregion // Types

        public TruckPool Trucks;
        public ExternalAirshipPool ExternalAirships;
        public InternalAirshipPool InternalAirships;
        public ParcelPool Parcels;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Trucks.TryInitialize(PoolRoot);
            ExternalAirships.TryInitialize(PoolRoot);
            InternalAirships.TryInitialize(PoolRoot);
            Parcels.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {
            
        }
    }
}