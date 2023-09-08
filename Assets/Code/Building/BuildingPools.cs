using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;
using Zavala.Roads;

namespace Zavala.Building
{
    public class BuildingPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class RoadPool : SerializablePool<RoadInstanceController> { }
        [Serializable] public class DigesterPool : SerializablePool<OccupiesTile> { }
        [Serializable] public class StoragePool : SerializablePool<OccupiesTile> { }
        [Serializable] public class SkimmerPool : SerializablePool<OccupiesTile> { }

        #endregion // Types

        [Header("Static Buildings")]
        public GameObject GrainFarm;
        public GameObject DairyFarm;
        public GameObject City;

        [Header("Player-created Buildings")]
        public RoadPool Roads;
        public DigesterPool Digesters;
        public DigesterPool Storages;
        public SkimmerPool Skimmers;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Roads.TryInitialize(PoolRoot);
            Digesters.TryInitialize(PoolRoot);
            Storages.TryInitialize(PoolRoot);
            Skimmers.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}