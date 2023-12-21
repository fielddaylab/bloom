using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Audio {
    public sealed class SfxState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public DynamicPool<AudioSource> PlaybackPool;
        [NonSerialized] public RingBuffer<SfxCommand> CommandQueue = new RingBuffer<SfxCommand>(32, RingBufferMode.Expand);

        [NonSerialized] public RingBuffer<ActiveSfxData> ActiveSfx = new RingBuffer<ActiveSfxData>(32, RingBufferMode.Expand);
        [NonSerialized] public LLTable<SfxPositionUpdateData> PositionalUpdateTable;
        [NonSerialized] public LLIndexList PositionalUpdateList;

        [NonSerialized] public Dictionary<StringHash32, AudioClip> LoadedClips = MapUtils.Create<StringHash32, AudioClip>(64);
        [NonSerialized] public Dictionary<StringHash32, SfxAsset> LoadedSfxAssets = MapUtils.Create<StringHash32, SfxAsset>(64);

        [NonSerialized] public UniqueIdAllocator16 LoopHandleAllocator = new UniqueIdAllocator16(64);

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
            PlaybackPool.Config.RegisterOnFree((p, a) => {
                a.Stop();
                a.clip = null;
#if UNITY_EDITOR
                a.gameObject.name = "Sfx";
#endif // UNITY_EDITOR
            });

            PlaybackPool.Prewarm(8);

            PositionalUpdateTable = new LLTable<SfxPositionUpdateData>(32);
            PositionalUpdateList = LLIndexList.Empty;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SfxCommand {
        [FieldOffset(0)] public SfxCommandType Type;
        [FieldOffset(4)] public SfxPlayData PlayData;
        [FieldOffset(4)] public SfxIdData StopData;
    }

    [Flags]
    public enum SfxPlayFlags : ushort {
        Loop = 0x01,
        Randomize = 0x02,
        Positional = 0x04
    }

    public enum SfxCommandType : ushort {
        PlayClip,
        PlayFromAssetRef,
        StopWithClip,
        StopWithTag,
        StopAll,
        StopHandle
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SfxAssetRef {
        [FieldOffset(0)] public StringHash32 AssetId;
        [FieldOffset(0)] public int InstanceId;
    }

    public struct SfxPlayData {
        public SfxAssetRef Asset;
        public float Volume;
        public float Pitch;
        public float Delay;
        public StringHash32 Tag;
        public SfxPlayFlags Flags;
        public UniqueId16 Handle;

        public int TransformId;
        public Vector3 TransformOffset;
        public float MinDistance;
        public float MaxDistance;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SfxIdData {
        [FieldOffset(0)] public StringHash32 Id;
        [FieldOffset(0)] public UniqueId16 Handle;
    }

    public struct ActiveSfxData {
        public AudioSource Src;
        public StringHash32 ClipId;
        public StringHash32 Tag;
        public UniqueId16 Handle;
        public ushort FrameStarted;
        public int PositionUpdateIndex;
    }

    public struct SfxPositionUpdateData {
        public AudioSource Src;
        public Transform Position;
        public Transform Reference;
        public Vector3 RefOffset;
        public float ZoomScale;
    }

    static public class SfxUtility {
        static public void PlaySfx(StringHash32 assetId, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayClip,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { AssetId = assetId },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag
                }
            });
        }

        static public void PlaySfx(SfxAsset asset, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayFromAssetRef,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { InstanceId = asset.GetInstanceID() },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag
                }
            });
        }

        static public void PlaySfx(AudioClip clip, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayFromAssetRef,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { InstanceId = clip.GetInstanceID() },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag
                }
            });
        }

        static public void PlaySfx3d(StringHash32 assetId, Transform position, Vector3 offset, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayClip,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { AssetId = assetId },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag,
                    Flags = SfxPlayFlags.Positional,
                    TransformId = UnityHelper.Id(position),
                    TransformOffset = offset,
                }
            });
        }

        static public UniqueId16 LoopSfx(AudioClip clip, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            SfxState state = Game.SharedState.Get<SfxState>();
            UniqueId16 handle = state.LoopHandleAllocator.Alloc();
            state.CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayFromAssetRef,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { InstanceId = clip.GetInstanceID() },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag,
                    Flags = SfxPlayFlags.Loop | SfxPlayFlags.Randomize,
                    Handle = handle
                }
            });
            return handle;
        }

        static public UniqueId16 LoopSfx3d(AudioClip clip, Transform position, Vector3 offset, float minDistance = 0.1f, float maxDistance = 1f, float volume = 1, float pitch = 1, float delay = 0, StringHash32 tag = default) {
            SfxState state = Game.SharedState.Get<SfxState>();
            UniqueId16 handle = state.LoopHandleAllocator.Alloc();
            state.CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.PlayFromAssetRef,
                PlayData = new SfxPlayData() {
                    Asset = new SfxAssetRef() { InstanceId = clip.GetInstanceID() },
                    Volume = volume,
                    Pitch = pitch,
                    Delay = 0,
                    Tag = tag,
                    Flags = SfxPlayFlags.Loop | SfxPlayFlags.Randomize | SfxPlayFlags.Positional,
                    TransformId = UnityHelper.Id(position),
                    TransformOffset = offset,
                    MinDistance = minDistance,
                    MaxDistance = maxDistance,
                    Handle = handle
                }
            });
            return handle;
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

        static public void StopFromHandle(UniqueId16 handle) {
            Game.SharedState.Get<SfxState>().CommandQueue.PushBack(new SfxCommand() {
                Type = SfxCommandType.StopHandle,
                StopData = new SfxIdData() {
                    Handle = handle
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