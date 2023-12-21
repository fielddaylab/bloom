using FieldDay;
using UnityEngine;
using BeauUtil;
using FieldDay.Systems;
using BeauUtil.Debugger;
using Zavala.Input;
using Zavala.World;

namespace Zavala.Audio {
    [SysUpdate(GameLoopPhaseMask.LateFixedUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 10000)]
    public sealed class SfxSystem : SharedStateSystemBehaviour<SfxState> {
        public const int DistanceScale = 10;

        public override void ProcessWork(float deltaTime) {
            // handle checking for current playback
            for(int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ref ActiveSfxData sfx = ref m_State.ActiveSfx[i];
                if (!sfx.Src.isPlaying) {
                    if (Frame.Age(sfx.FrameStarted) > 4) {
                        // if more than 4 frames old, then we can safely stop
                        sfx.Src.Stop();
                        FreeHandle(sfx.Handle);
                        FreePositionalUpdate(sfx.PositionUpdateIndex);
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

                    case SfxCommandType.StopHandle: {
                        StopWithHandle(cmd.StopData.Handle);
                        break;
                    }

                    case SfxCommandType.PlayClip: {
                        PlayClipFromId(cmd.PlayData);
                        break;
                    }

                    case SfxCommandType.PlayFromAssetRef: {
                        PlayClipFromRef(cmd.PlayData);
                        break;
                    }
                }
            }
        
            // handle positional updates
            if (GameLoop.IsPhase(GameLoopPhase.UnscaledLateUpdate)) {
                UpdatePositionalAudio();
            }
        }

        private void UpdatePositionalAudio() {
            if (m_State.PositionalUpdateList.Length <= 0) {
                return;
            }

            // TODO: better method of storing these?
            Camera c = Game.SharedState.Get<SimWorldCamera>().Camera;
            AudioListener listener = c.GetComponent<AudioListener>();

            listener.transform.GetPositionAndRotation(out Vector3 listenerPos, out Quaternion listenerRot);
            Matrix4x4 worldToCamera = c.transform.worldToLocalMatrix;
            float cameraFov = c.fieldOfView;

            var enumerator = m_State.PositionalUpdateTable.GetEnumerator(m_State.PositionalUpdateList);
            while (enumerator.MoveNext()) {
                var entry = enumerator.Current.Tag;
                Vector3 pos = entry.RefOffset;
                if (entry.Reference) {
                    pos += entry.Reference.position;
                }
                Vector3 camPos = worldToCamera.MultiplyPoint3x4(pos);
                float fHeight = CameraHelper.HeightForDistanceAndFOV(Mathf.Abs(camPos.z), cameraFov);
                Vector3 viewportOffset = new Vector3(DistanceScale * camPos.x / fHeight, DistanceScale * camPos.y / fHeight, camPos.z / entry.ZoomScale);
                entry.Position.position = listenerPos + (listenerRot * viewportOffset);
            }
        }

        private void FreeHandle(UniqueId16 handle) {
            if (handle != UniqueId16.Invalid) {
                m_State.LoopHandleAllocator.Free(handle);
            }
        }

        private void FreePositionalUpdate(int index) {
            if (index >= 0) {
                m_State.PositionalUpdateTable.Remove(ref m_State.PositionalUpdateList, index);
            }
        }

        private void StopAll() {
            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                sfx.Src.Stop();
                m_State.PlaybackPool.Free(sfx.Src);
            }

            m_State.ActiveSfx.Clear();
            m_State.PositionalUpdateTable.Clear(ref m_State.PositionalUpdateList);
        }

        private void StopAllWithClip(StringHash32 clipId) {
            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                if (sfx.ClipId == clipId) {
                    sfx.Src.Stop();
                    FreeHandle(sfx.Handle);
                    FreePositionalUpdate(sfx.PositionUpdateIndex);
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
                    FreePositionalUpdate(sfx.PositionUpdateIndex);
                    m_State.PlaybackPool.Free(sfx.Src);
                    m_State.ActiveSfx.FastRemoveAt(i);
                }
            }
        }

        private void StopWithHandle(UniqueId16 handle) {
            if (!m_State.LoopHandleAllocator.IsValid(handle)) {
                return;
            }

            for (int i = m_State.ActiveSfx.Count - 1; i >= 0; i--) {
                ActiveSfxData sfx = m_State.ActiveSfx[i];
                if (sfx.Handle == handle) {
                    sfx.Src.Stop();
                    FreeHandle(sfx.Handle);
                    FreePositionalUpdate(sfx.PositionUpdateIndex);
                    m_State.PlaybackPool.Free(sfx.Src);
                    m_State.ActiveSfx.FastRemoveAt(i);
                    return;
                }
            }
        }

        private void PlayClipFromId(SfxPlayData play) {
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
            float spread = 80;
            float zoomScale = 80;
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

                spread = asset.Spread;
                play.MinDistance *= asset.Range;
                play.MaxDistance *= asset.Range;
                zoomScale = asset.Range;
            }

            AudioSource src = m_State.PlaybackPool.Alloc();
            src.clip = clip;
            src.volume = play.Volume;
            src.pitch = play.Pitch;
            src.loop = (play.Flags & SfxPlayFlags.Loop) != 0;
            src.time = (play.Flags & SfxPlayFlags.Randomize) != 0 ? RNG.Instance.NextFloat(clip.length) : 0;

            src.PlayDelayed(play.Delay);

#if UNITY_EDITOR
            src.gameObject.name = clipId.ToDebugString();
#endif // UNITY_EDITOR

            ActiveSfxData active;
            active.ClipId = clipId;
            active.FrameStarted = Frame.Index;
            active.Tag = play.Tag;
            active.Src = src;
            active.Handle = play.Handle;

            if ((play.Flags & SfxPlayFlags.Positional) != 0) {
                SfxPositionUpdateData posUpdate;
                posUpdate.Src = src;
                posUpdate.Position = src.transform;
                posUpdate.Reference = UnityHelper.Find<Transform>(play.TransformId);
                posUpdate.RefOffset = play.TransformOffset;
                posUpdate.ZoomScale = zoomScale;
                active.PositionUpdateIndex = m_State.PositionalUpdateTable.PushBack(ref m_State.PositionalUpdateList, posUpdate);

                src.spatialBlend = 1;
                src.spread = spread;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = play.MinDistance;
                src.maxDistance = play.MaxDistance;
            } else {
                active.PositionUpdateIndex = -1;
                src.spatialBlend = 0;
            }

            m_State.ActiveSfx.PushBack(active);
        }
    }
}