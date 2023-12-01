using BeauUtil;
using BeauUWT;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Zavala.Audio {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class MusicSystem : SharedStateSystemBehaviour<MusicState> {
        public override void ProcessWork(float deltaTime) {
            switch (m_State.Step) {
                case MusicPlaybackStep.None: {
                    string nextPath = GetNextPath(out m_State.Mode);
                    if (!string.IsNullOrEmpty(nextPath)) {
                        m_State.MusicStream.SetURLFromStreamingAssets(nextPath);
                        m_State.MusicStream.Preload();
                        m_State.Step = MusicPlaybackStep.Load;
                    }
                    break;
                }
                case MusicPlaybackStep.Load: {
                    if (m_State.MusicStream.IsReady()) {
                        m_State.MusicStream.Volume = 0;
                        m_State.CurrentVolume = 0;
                        m_State.MusicStream.Play();
                        m_State.Step = MusicPlaybackStep.FadeIn;
                    } else if (m_State.Mode == MusicPlaybackMode.Playlist && m_State.Queue.Count > 0) {
                        m_State.MusicStream.Unload();
                        m_State.Mode = MusicPlaybackMode.None;
                    }
                    break;
                }
                case MusicPlaybackStep.FadeIn: {
                    if (m_State.CurrentVolume < 1) {
                        m_State.CurrentVolume = Mathf.Clamp01(m_State.CurrentVolume + deltaTime / 2);
                        if (m_State.CurrentVolume == 1) {
                            m_State.Step = MusicPlaybackStep.Playing;
                        }
                    }
                    break;
                }
                case MusicPlaybackStep.Playing: {
                    if (!m_State.MusicStream.IsPlaying) {
                        if (m_State.Queue.Count == 0) {
                            m_State.Step = MusicPlaybackStep.Wait;
                            m_State.CurrentWait = 2;
                        } else {
                            m_State.Step = MusicPlaybackStep.None;
                        }
                        m_State.CurrentVolume = 0;
                    } else if (m_State.Mode == MusicPlaybackMode.Playlist && m_State.Queue.Count > 0) {
                        m_State.Step = MusicPlaybackStep.FadeOut;
                    }
                    break;
                }
                case MusicPlaybackStep.FadeOut: {
                    if (m_State.CurrentVolume > 0) {
                        m_State.CurrentVolume = Mathf.Clamp01(m_State.CurrentVolume - deltaTime * 2);
                        if (m_State.CurrentVolume == 0 || !m_State.MusicStream.IsPlaying) {
                            m_State.MusicStream.Stop();
                            if (m_State.Queue.Count == 0) {
                                m_State.Step = MusicPlaybackStep.Wait;
                                m_State.CurrentWait = 2;
                            } else {
                                m_State.Step = MusicPlaybackStep.None;
                            }
                        }
                    }
                    break;
                }
                case MusicPlaybackStep.Wait: {
                    m_State.CurrentWait -= deltaTime;
                    if (m_State.CurrentWait <= 0) {
                        m_State.Step = MusicPlaybackStep.None;
                    }
                    break;
                }
            }

            m_State.MusicStream.Volume = m_State.Volume * m_State.CurrentVolume;
        }

        private string GetNextPath(out MusicPlaybackMode mode) {
            if (m_State.Queue.TryPopFront(out string queuedPath)) {
                mode = MusicPlaybackMode.Queue;
                return queuedPath;
            } else if (m_State.Playlist.Count > 0) {
                mode = MusicPlaybackMode.Playlist;
                return m_State.Playlist.Next();
            } else {
                mode = MusicPlaybackMode.None;
                return null;
            }
        }
    }
}