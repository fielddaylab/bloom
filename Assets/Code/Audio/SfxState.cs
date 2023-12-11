using FieldDay.SharedState;
using FieldDay;
using UnityEngine;
using System;
using BeauPools;
using BeauUtil;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Zavala.Audio {
    public sealed class SfxState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public DynamicPool<AudioSource> PlaybackPool;
        [NonSerialized] public RingBuffer<SfxCommand> CommandQueue = new RingBuffer<SfxCommand>(32, RingBufferMode.Expand);
        [NonSerialized] public RingBuffer<ActiveSfxData> ActiveSfx = new RingBuffer<ActiveSfxData>(32, RingBufferMode.Expand);

        [NonSerialized] public Dictionary<StringHash32, AudioClip> LoadedClips = MapUtils.Create<StringHash32, AudioClip>(64);
        [NonSerialized] public Dictionary<StringHash32, SfxAsset> LoadedSfxAssets = MapUtils.Create<StringHash32, SfxAsset>(64);

        void IRegistrationCallbacks.OnDeregister() {
            PlaybackPool.Clear();
        }

        void IRegistrationCallbacks.OnRegister() {
            PlaybackPool = new DynamicPool<AudioSource>(64, (p) => {
                GameObject go = new GameObject("Sfx");
                go.transform.SetParent(transform);
                AudioSource src = go.AddComponent<AudioSource>();
                src.loop = false;
                src.playOnAwake = false;
                src.spatialBlend = 0;
                return src;
            });
            PlaybackPool.Config.RegisterOnDestruct((p, a) => Destroy(a.gameObject));
            PlaybackPool.Config.RegisterOnFree((p, a) => { a.Stop(); a.clip = null; });

            PlaybackPool.Prewarm(8);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SfxCommand {
        [FieldOffset(0)] public SfxCommandType Type;
        [FieldOffset(4)] public SfxPlayData PlayData;
        [FieldOffset(4)] public SfxIdData StopData;
    }

    public enum SfxCommandType : ushort {
        PlayClip,
        StopWithClip,
        StopWithTag,
        StopAll
    }

    public struct SfxPlayData {
        public StringHash32 AssetId;
        public float Volume;
        public float Pitch;
        public float Delay;
        public StringHash32 Tag;
    }

    public struct SfxIdData {
        public StringHash32 Id;
    }

    public struct ActiveSfxData {
        public AudioSource Src;
        public StringHash32 ClipId;
        public StringHash32 Tag;
        public ushort FrameStarted;
    }

    static public class SfxUtility {
        static public void PlaySfx(StringHash32 assetId, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayClip,
                PlayData = new SfxPlayData() {
                    AssetId = assetId,
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag
                }
            });
        }

        static public void StopWithClip(StringHash32 assetId) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.StopWithClip,
                StopData = new SfxIdData() {
                    Id = assetId
                }
            });
        }

        static public void StopWithTag(StringHash32 tag) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.StopWithTag,
                StopData = new SfxIdData() {
                    Id = tag
                }
            });
        }

        static public void StopAll() {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.StopAll,
            });
        }
    }
}