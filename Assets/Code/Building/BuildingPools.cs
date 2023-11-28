using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.World;

namespace Zavala.Building
{
    public class BuildingPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class RoadPool : SerializablePool<RoadInstanceController> { }
        [Serializable] public class DigesterPool : SerializablePool<OccupiesTile> { }
        [Serializable] public class StoragePool : SerializablePool<OccupiesTile> { }
        [Serializable] public class SkimmerPool : SerializablePool<OccupiesTile> { }
        [Serializable] public class VizAnchorPool : SerializablePool<SpriteRenderer> { }
        [Serializable] public class DestroyIconsPool : SerializablePool<OccupiesTile> { }



        #endregion // Types

        [Header("Player-created Buildings")]
        public RoadPool Roads;
        public DigesterPool Digesters;
        public StoragePool Storages;
        public SkimmerPool Skimmers;

        [Header("Highlights")]
        public VizAnchorPool VizAnchors;
        public DestroyIconsPool DestroyIcons;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Roads.TryInitialize(PoolRoot);
            Digesters.TryInitialize(PoolRoot);
            Storages.TryInitialize(PoolRoot);
            Skimmers.TryInitialize(PoolRoot);
            VizAnchors.TryInitialize(PoolRoot);
            DestroyIcons.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}