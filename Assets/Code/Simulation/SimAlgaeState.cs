using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Zavala.Sim {
    public sealed class SimAlgaeState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public AlgaeBuffers Algae;
        // TODO: create better solution, 
        public GameObject AlgaePrefab;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            SimGridState gridState = ZavalaGame.SimGrid;
            Algae.Create(gridState.HexSize);
        }


    }
}