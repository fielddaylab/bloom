using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;
using Zavala.Roads;
using Zavala.Sim;

namespace Zavala.Building {
    public class BuildingPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class RoadPool : SerializablePool<RoadInstanceController> { }
        [Serializable] public class DigesterPool : SerializablePool<OccupiesTile> { }
        [Serializable] public class StoragePool : SerializablePool<OccupiesTile> { }
        [Serializable] public class SkimmerPool : SerializablePool<PhosphorusSkimmer> { }
        [Serializable] public class VizAnchorPool : SerializablePool<ParticleSystem> { }


        #endregion // Types

        [Header("Player-created Buildings")]
        public RoadPool Roads;
        public DigesterPool Digesters;
        public StoragePool Storages;
        public SkimmerPool Skimmers;

        [Header("Building Meshes")]
        public Mesh DigesterMesh;
        public Mesh StorageMesh;

        [Header("Highlights")]
        public VizAnchorPool VizAnchors;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Roads.TryInitialize(PoolRoot);
            Digesters.TryInitialize(PoolRoot);
            Storages.TryInitialize(PoolRoot);
            Skimmers.TryInitialize(PoolRoot);
            VizAnchors.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}