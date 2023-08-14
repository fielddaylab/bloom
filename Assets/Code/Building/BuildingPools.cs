using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;

namespace Zavala.Building
{
    public class BuildingPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class RoadPool : SerializablePool<OccupiesTile> { }
        [Serializable] public class DigesterPool : SerializablePool<OccupiesTile> { }

        #endregion // Types

        public RoadPool Roads;
        public DigesterPool Digesters;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Roads.TryInitialize(PoolRoot);
            Digesters.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}