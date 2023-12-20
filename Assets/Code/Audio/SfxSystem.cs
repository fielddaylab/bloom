using FieldDay;
using UnityEngine;
using BeauUtil;
using FieldDay.Systems;
using BeauUtil.Debugger;

namespace Zavala.Audio {
    [SysUpdate(GameLoopPhaseMask.LateFixedUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 10000)]
    public sealed class SfxSystem : SharedStateSystemBehaviour<SfxState> {
        public override void ProcessWork(float deltaTime) {
            // handle checking for current playback
            for(int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ref ActiveSfxData sfx = ref m_State.ActiveSfx[i];
                if (!sfx.Src.isPlaying) {
                    if (Frame.Age(sfx.FrameStarted) > 4) {
                        // if more than 8 frames old, then we can safely stop
                        sfx.Src.Stop();
                        m_State.PlaybackPool.Free(sfx.Src);
                        m_State.ActiveSfx.FastRemoveAt(i);
                    }
                } else {
                    sfx.FrameStarted = Frame.InvalidIndex;
                }
            }

            // handle command queue
            while(m_State.CommandQueue.TryPopBack(out SfxCommand cmd)) {
                switch (cmd.Type) {
                    case SfxCommandType.StopAll: {
                        StopAll();
                        break;
                    }

                    case SfxCommandType.StopWithClip: {
                        StopAllWithClip(cmd.StopData.Id);
                        break;
                    }

                    case SfxCommandType.StopWithTag: {
                        StopAllWithTag(cmd.StopData.Id);
                        break;
                    }

                    case SfxCommandType.PlayClip: {
                        PlayClip(cmd.PlayData);
                        break;
                    }

                    case SfxCommandType.PlayFromAssetRef: {
                        PlayClipFromRef(cmd.PlayData);
                        break;
                    }
                }
            }
        }

        private void FreeHandle(UniqueId16 handle) {
            if (handle != UniqueId16.Invalid) {
                m_State.LoopHandleAllocator.Free(handle);
            }
        }

        private void StopAll() {
            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                sfx.Src.Stop();
                m_State.PlaybackPool.Free(sfx.Src);
            }
            m_State.ActiveSfx.Clear();
        }

        private void StopAllWithClip(StringHash32 clipId) {
            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                if (sfx.ClipId == clipId) {
                    sfx.Src.Stop();
                    FreeHandle(sfx.Handle);
                    m_State.PlaybackPool.Free(sfx.Src);
                    m_State.ActiveSfx.FastRemoveAt(i);
                }
            }
        }

        private void StopAllWithTag(StringHash32 tagId) {
            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                if (sfx.Tag == tagId) {
                    sfx.Src.Stop();
                    FreeHandle(sfx.Handle);
                    m_State.PlaybackPool.Free(sfx.Src);
                    m_State.ActiveSfx.FastRemoveAt(i);
                }
            }
        }

        private void PlayClip(SfxPlayData play) {
            SfxAsset asset = null;
            if (!m_State.LoadedClips.TryGetValue(play.Asset.AssetId, out AudioClip clip)) {
                if (!m_State.LoadedSfxAssets.TryGetValue(play.Asset.AssetId, out asset)) {
                    Log.Warn("[SfxSystem] No clips or assets loaded with id '{0}'", play.Asset.AssetId);
                    FreeHandle(play.Handle);
                    return;
                }
            }

            PlayClip(play, asset, clip, play.Asset.AssetId);
        }

        private void PlayClipFromRef(SfxPlayData play) {
            var instance = UnityHelper.Find(play.Asset.InstanceId);
            if (instance == null) {
                Log.Warn("[SfxSystem] No clips or assets loaded with instance id '{0}'", play.Asset.InstanceId);
                FreeHandle(play.Handle);
                return;
            }

            AudioClip clip = instance as AudioClip;
            SfxAsset asset = instance as SfxAsset;

            if (clip == null && asset == null) {
                Log.Warn("[SfxSystem] No clips or assets loaded with instance id '{0}'", play.Asset.InstanceId);
                FreeHandle(play.Handle);
                return;
            }

            PlayClip(play, asset, clip, instance.name);
        }

        private void PlayClip(SfxPlayData play, SfxAsset asset, AudioClip clip, StringHash32 clipId) {
            if (asset != null) {
                if (asset.Randomizer == null) {
                    asset.Randomizer = new RandomDeck<AudioClip>(asset.Clips);
                }
                clip = asset.Randomizer.Next();

                play.Volume *= asset.Volume.Generate();
                play.Pitch *= asset.Pitch.Generate();
                play.Delay += asset.Delay.Generate();

                if (play.Tag.IsEmpty) {
                    play.Tag = asset.Tag;
                }
            }

            AudioSource src = m_State.PlaybackPool.Alloc();
            src.clip = clip;
            src.volume = play.Volume;
            src.pitch = play.Pitch;
            src.loop = play.Handle != UniqueId16.Invalid;

            src.PlayDelayed(play.Delay);

            ActiveSfxData active;
            active.ClipId = clipId;
            active.FrameStarted = Frame.Index;
            active.Tag = play.Tag;
            active.Src = src;
            active.Handle = play.Handle;

            m_State.ActiveSfx.PushBack(active);
        }
    }
}