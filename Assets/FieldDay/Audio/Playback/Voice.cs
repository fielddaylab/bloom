using System;
using BeauUtil;
using BeauUWT;
using UnityEngine;
using UnityEngine.UIElements;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio voice type.
    /// </summary>
    public enum AudioVoiceType : byte {
        Sample,
        Stream
    }

    /// <summary>
    /// Playback flags.
    /// </summary>
    [Flags]
    public enum AudioPlaybackFlags : byte {
        PreloadOnly = 0x01
    }

    /// <summary>
    /// Voice state.
    /// </summary>
    internal enum VoiceState : byte {
        Idle = 0,
        PlayRequested,
        Playing,
        Paused,
        Stopped
    }

    /// <summary>
    /// Shared voice data,
    /// </summary>
    internal struct VoiceData {
        public VoiceState State;
        public AudioPlaybackFlags Flags;
        public byte StopCounter;
        public UniqueId16 CurrentHandle;
        public bool RandomizePlaybackHeadOnPlay;

        public ushort BusIndex;
        public float Delay;
        public ulong LastKnownTime;
        public double LastStartTimeDSP;

        public VoicePositionData Position;
        public VoiceCallbackData Callbacks;
        public VoiceParamBlockData Params;

        public AudioEvent Event;
    }

    /// <summary>
    /// Voice parameter blocks,
    /// </summary>
    internal struct VoiceParamBlockData {
        public AudioPropertyBlock EventProperties;
        public AudioPropertyBlock LocalProperties;
        public AudioPropertyBlock LastKnownProperties;
    }

    /// <summary>
    /// Voice callbacks.
    /// </summary>
    internal struct VoiceCallbackData {
        public VoiceCallback OnLoop;
        public VoiceCallback OnStop;
    }

    /// <summary>
    /// Voice positioning data.
    /// </summary>
    internal struct VoicePositionData {
        public AudioEmitterMode Mode;
        public Transform TransformRead;
        public Vector3 ReadOffset;
        public Transform TransformWrite;
    }

    /// <summary>
    /// Data for a sample voice.
    /// </summary>
    internal struct SampleVoiceData {
        public VoiceData Voice;
        public AudioSource Source;
    }

    /// <summary>
    /// Data for a streaming voice.
    /// </summary>
    internal struct StreamVoiceData {
        public VoiceData Voice;
        public UWTStreamPlayer Source;
    }

    public delegate void VoiceCallback(AudioHandle handle);

    static internal class VoiceUtility {
        #region Setup

        static internal void LoadVoice(ref VoiceData voiceData, AudioEvent audioEvent, UniqueId16 handle, Transform sourcePos, System.Random random) {
            voiceData.State = VoiceState.Idle;
            voiceData.Flags = 0;
            voiceData.StopCounter = 0;
            voiceData.CurrentHandle = handle;
            voiceData.RandomizePlaybackHeadOnPlay = audioEvent.RandomizeStartLocation;

            voiceData.BusIndex = audioEvent.BusIndex;
            voiceData.LastKnownTime = 0;
            voiceData.LastStartTimeDSP = 0;

            voiceData.Params.LocalProperties = AudioPropertyBlock.Default;
            voiceData.Params.LastKnownProperties = default;

            voiceData.Callbacks = default;

            LoadInitialPropertyBlock(out voiceData.Params.EventProperties, out voiceData.Delay, audioEvent, random);

            voiceData.Position.Mode = audioEvent.Spatial.Mode;
            voiceData.Position.TransformRead = null;
            voiceData.Position.TransformWrite = sourcePos;
            voiceData.Position.ReadOffset = default;
        }

        static internal void LoadSample(ref SampleVoiceData sampleData, AudioEvent audioEvent, AudioSource source, UniqueId16 handle, System.Random random) {
            LoadVoice(ref sampleData.Voice, audioEvent, handle, source.transform, random);

            if (audioEvent.SampleSelector == null) {
                audioEvent.SampleSelector = new RandomDeck<AudioClip>(audioEvent.Samples);
            }

            source.loop = audioEvent.Loop;
            source.clip = audioEvent.SampleSelector.Next();
            source.priority = audioEvent.Priority;
        }

        static internal void LoadStream(ref StreamVoiceData sampleData, AudioEvent audioEvent, UWTStreamPlayer source, UniqueId16 handle, System.Random random) {
            LoadVoice(ref sampleData.Voice, audioEvent, handle, source.transform, random);

            source.Loop = audioEvent.Loop;
            source.SetURLFromPath(audioEvent.StreamingPath);
        }

        static internal void LoadInitialPropertyBlock(out AudioPropertyBlock properties, out float delay, AudioEvent audioEvent, System.Random random) {
            properties.Volume = audioEvent.Volume.Generate(random);
            properties.Pitch = audioEvent.Pitch.Generate(random);
            properties.Pan = audioEvent.Pan.Generate(random);
            properties.Mute = properties.Pause = false;
            delay = audioEvent.Delay.Generate(random);
        }

        static internal void LoadSpatialProperties(AudioSource source, in AudioEmitterConfig emitterConfig, bool hasSpatialPlugin) {
            switch (emitterConfig.Mode) {
                case AudioEmitterMode.Fixed: {
                    source.spatialBlend = 0;
                    source.spatialize = false;
                    break;
                }

                default: {
                    source.spatialBlend = 1 - emitterConfig.DespatializeFactor;
                    source.spatialize = hasSpatialPlugin;

                    source.rolloffMode = emitterConfig.Rolloff;
                    source.minDistance = emitterConfig.MinDistance;
                    source.maxDistance = emitterConfig.MaxDistance;

                    source.dopplerLevel = emitterConfig.DopplerLevel;
                    source.spread = emitterConfig.Spread;
                    break;
                }
            }

            source.reverbZoneMix = emitterConfig.ReverbZoneMix;

            source.bypassEffects = (emitterConfig.EffectBypasses & AudioEmitterBypassFlags.LocalEffects) != 0;
            source.bypassListenerEffects = (emitterConfig.EffectBypasses & AudioEmitterBypassFlags.ListenerEffects) != 0;
            source.bypassReverbZones = (emitterConfig.EffectBypasses & AudioEmitterBypassFlags.ReverbZones) != 0;
        }

        #endregion // Setup

        #region Sync

        static internal void SyncSettingsToSource(ref SampleVoiceData sampleData) {
            AudioPropertyBlock lastProps = sampleData.Voice.Params.LastKnownProperties;
            sampleData.Source.volume = lastProps.Volume;
            sampleData.Source.pitch = lastProps.Pitch;
            sampleData.Source.panStereo = lastProps.Pan;
            sampleData.Source.mute = !lastProps.IsAudible();
        }

        static internal void SyncSettingsToSource(ref StreamVoiceData sampleData) {
            AudioPropertyBlock lastProps = sampleData.Voice.Params.LastKnownProperties;
            sampleData.Source.Volume = lastProps.Volume;
            sampleData.Source.Mute = !lastProps.IsAudible();
        }

        #endregion // Sync
    }
}