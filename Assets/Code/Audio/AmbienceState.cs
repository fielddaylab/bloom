using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Audio {
    public sealed class AmbienceState : SharedStateComponent, IRegistrationCallbacks {
        public AudioSource AmbiencePlayerA;
        public AudioSource AmbiencePlayerB;
        [Range(0, 1)] public float Volume;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            
        }
    }
}