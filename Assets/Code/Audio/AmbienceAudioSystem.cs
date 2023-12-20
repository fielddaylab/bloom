using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Audio {
    public sealed class AmbienceAudioSystem : SharedStateSystemBehaviour<AmbienceState, SimGridState, SimTimeState> {
        static private readonly StringHash32 AmbientBirdTag = "birds";

        public override void ProcessWork(float deltaTime) {
            AmbientSfxConfig config = m_StateA.CurrentConfig;

            if (m_StateA.QueuedUpdate) {
                AmbientSfxConfig nextConfig = m_StateA.RegionConfigs[m_StateB.CurrRegionIndex];
                if (nextConfig != config) {
                    config = m_StateA.CurrentConfig = nextConfig;
                    if (config) {
                        m_StateA.BirdSoundDelay = Math.Min(m_StateA.BirdSoundDelay, config.Delay.Generate() / 2);
                        Log.Msg("[AmbienceAudioSystem] Ambience switched to '{0}'", nextConfig.name);
                    } else {
                        Log.Msg("[AmbienceAudioSystem] Ambience switched to null");
                    }
                }
            }

            // don't play during fullscreen cutscene
            if (config && (m_StateC.Paused & SimPauseFlags.FullscreenCutscene) == 0) {
                m_StateA.BirdSoundDelay -= Frame.DeltaTime;
                if (m_StateA.BirdSoundDelay <= 0) {
                    SfxAsset birdSound = RNG.Instance.Choose(config.BirdSounds);
                    SfxUtility.PlaySfx(birdSound, m_StateA.BirdVolume, 1, 0, AmbientBirdTag);
                    float nextDelay;
                    if (RNG.Instance.Chance(config.DelayShortChance)) {
                        nextDelay = config.DelayShort.Generate();
                    } else {
                        nextDelay = config.Delay.Generate();
                    }
                    m_StateA.BirdSoundDelay += nextDelay;
                }
            }
        }

        public override void Initialize() {
            base.Initialize();

            Game.Events.Register(GameEvents.RegionSwitched, OnRegionSwitched)
                .Register(SimGridState.Event_RegionUpdated, OnRegionSwitched);
        }

        public override void Shutdown() {
            Game.Events?.Deregister(GameEvents.RegionSwitched, OnRegionSwitched)
                .Deregister(SimGridState.Event_RegionUpdated, OnRegionSwitched);

            base.Shutdown();
        }

        private void OnRegionSwitched() {
            if (!m_StateA) {
                return;
            }

            m_StateA.QueuedUpdate = true;
        }
    }
}