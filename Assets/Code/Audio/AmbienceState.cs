using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Audio {
    public sealed class AmbienceState : SharedStateComponent, IRegistrationCallbacks {
        public AudioSource AmbiencePlayerA;
        public AudioSource AmbiencePlayerB;

        [Range(0, 1)] public float LoopVolume = 1;
        [Range(0, 1)] public float BirdVolume = 1;

        [NonSerialized] public AmbientSfxConfig[] RegionConfigs;

        [NonSerialized] public AmbientSfxConfig CurrentConfig;
        [NonSerialized] public float BirdSoundDelay;
        [NonSerialized] public bool QueuedUpdate;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            RegionConfigs = new AmbientSfxConfig[RegionInfo.MaxRegions];
        }
    }
}