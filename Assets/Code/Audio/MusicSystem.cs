using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Data;

namespace Zavala.Audio {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class MusicSystem : SharedStateSystemBehaviour<MusicState, UserSettings> {
        public override void ProcessWork(float deltaTime) {
            switch (m_StateA.Step) {
                case MusicPlaybackStep.None: {
                    string nextPath = GetNextPath(out m_StateA.Mode);
                    if (!string.IsNullOrEmpty(nextPath)) {
                        m_StateA.MusicStream.SetURLFromStreamingAssets(nextPath);
                        m_StateA.MusicStream.Preload();
                        m_StateA.Step = MusicPlaybackStep.Load;
                    }
                    break;
                }
                case MusicPlaybackStep.Load: {
                    if (m_StateA.MusicStream.IsReady()) {
                        m_StateA.MusicStream.Volume = 0;
                        m_StateA.CurrentVolume = 0;
                        m_StateA.MusicStream.Play();
                        m_StateA.MusicStream.Loop = m_StateA.Mode == MusicPlaybackMode.Override;
                        m_StateA.Step = MusicPlaybackStep.FadeIn;
                    } else if (m_StateA.Mode == MusicPlaybackMode.Playlist && !string.IsNullOrEmpty(m_StateA.Override)) {
                        m_StateA.MusicStream.Unload();
                        m_StateA.Mode = MusicPlaybackMode.None;
                    }
                    break;
                }
                case MusicPlaybackStep.FadeIn: {
                    if (m_StateA.CurrentVolume < 1) {
                        m_StateA.CurrentVolume = Mathf.Clamp01(m_StateA.CurrentVolume + deltaTime / 2);
                        if (m_StateA.CurrentVolume == 1) {
                            m_StateA.Step = MusicPlaybackStep.Playing;
                        }
                    }
                    break;
                }
                case MusicPlaybackStep.Playing: {
                    if (!m_StateA.MusicStream.IsPlaying) {
                        if (string.IsNullOrEmpty(m_StateA.Override)) {
                            m_StateA.Step = MusicPlaybackStep.Wait;
                            m_StateA.CurrentWait = 2;
                        } else {
                            m_StateA.Step = MusicPlaybackStep.None;
                        }
                        m_StateA.CurrentVolume = 0;
                    } else if (m_StateA.Mode == MusicPlaybackMode.Playlist && !string.IsNullOrEmpty(m_StateA.Override)) {
                        m_StateA.Step = MusicPlaybackStep.FadeOut;
                    } else if (m_StateA.Mode == MusicPlaybackMode.Override && string.IsNullOrEmpty(m_StateA.Override)) {
                        m_StateA.Step = MusicPlaybackStep.FadeOut;
                    }
                    break;
                }
                case MusicPlaybackStep.FadeOut: {
                    if (m_StateA.CurrentVolume > 0) {
                        m_StateA.CurrentVolume = Mathf.Clamp01(m_StateA.CurrentVolume - deltaTime * 2);
                        if (m_StateA.CurrentVolume == 0 || !m_StateA.MusicStream.IsPlaying) {
                            m_StateA.MusicStream.Stop();
                            if (string.IsNullOrEmpty(m_StateA.Override)) {
                                m_StateA.Step = MusicPlaybackStep.Wait;
                                m_StateA.CurrentWait = 2;
                            } else {
                                m_StateA.Step = MusicPlaybackStep.None;
                            }
                        }
                    }
                    break;
                }
                case MusicPlaybackStep.Wait: {
                    m_StateA.CurrentWait -= deltaTime;
                    if (m_StateA.CurrentWait <= 0) {
                        m_StateA.Step = MusicPlaybackStep.None;
                    }
                    break;
                }
            }

            m_StateA.MusicStream.Volume = m_StateA.Volume * m_StateA.CurrentVolume * m_StateB.MusicVolume;
        }

        private string GetNextPath(out MusicPlaybackMode mode) {
            if (!string.IsNullOrEmpty(m_StateA.Override)) {
                mode = MusicPlaybackMode.Override;
                return m_StateA.Override;
            } else if (m_StateA.Playlist.Count > 0) {
                mode = MusicPlaybackMode.Playlist;
                return m_StateA.Playlist.Next();
            } else {
                mode = MusicPlaybackMode.None;
                return null;
            }
        }
    }
}