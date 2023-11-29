using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Economy {
    public class MarketPools : SharedStateComponent, IRegistrationCallbacks {
        #region Types

        [Serializable] public class TruckPool : SerializablePool<RequestFulfiller> {
            [Header("Truck Configs")]
            [SerializeField] private SimpleMeshConfig GrainTruck;
            [SerializeField] private SimpleMeshConfig ManureTruck;
            [SerializeField] private SimpleMeshConfig MilkTruck;
            [SerializeField] private SimpleMeshConfig FertilizerTruck;

            public void SetTruckMesh(RequestFulfiller truck, ResourceMask resources) {
                if ((resources & ResourceMask.Manure) != 0) {
                    ManureTruck.Apply(truck.TruckRenderer, truck.TruckMesh);
                } else if ((resources & ResourceMask.MFertilizer) != 0 ||
                    (resources & ResourceMask.DFertilizer) != 0) {
                    FertilizerTruck.Apply(truck.TruckRenderer, truck.TruckMesh);
                } else if ((resources & ResourceMask.Grain) != 0) {
                    GrainTruck.Apply(truck.TruckRenderer, truck.TruckMesh);
                }
                if ((resources & ResourceMask.Milk) != 0) {
                    MilkTruck.Apply(truck.TruckRenderer, truck.TruckMesh);
                    // Temporary rotation because the prefab is rotated
                    truck.TruckRenderer.transform.rotation = Quaternion.Euler(-90, 180, -90);
                } else {
                    truck.TruckRenderer.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }
        }

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