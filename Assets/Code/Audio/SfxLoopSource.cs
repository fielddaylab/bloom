using FieldDay.SharedState;
using FieldDay;
using UnityEngine;
using System;
using BeauPools;
using BeauUtil;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using FieldDay.Components;

namespace Zavala.Audio {
    public sealed class SfxLoopSource : BatchedComponent {
        public AudioClip Sound;
        public float MinDistance = 2;
        public float MaxDistance = 2;
        public float Volume = 1;

        [NonSerialized] private UniqueId16 m_AudioId;

        protected override void OnEnable() {
            base.OnEnable();

            m_AudioId = SfxUtility.LoopSfx3d(Sound, transform, default, MinDistance, MaxDistance, Volume);
        }

        protected override void OnDisable() {
            if (!Game.IsShuttingDown) {
                SfxUtility.StopFromHandle(m_AudioId);
            }

            base.OnDisable();
        }
    }
}