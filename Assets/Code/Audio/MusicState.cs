using System;
using BeauUtil;
using BeauUWT;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Data;

namespace Zavala.Audio {
    public sealed class MusicState : SharedStateComponent, IRegistrationCallbacks {
        public UWTStreamPlayer MusicStream;
        [Range(0, 1)] public float Volume;

        [StreamingAudioPath] public string[] AllSongs;
        [NonSerialized] public MusicPlaybackMode Mode = MusicPlaybackMode.None;
        [NonSerialized] public MusicPlaybackStep Step = MusicPlaybackStep.None;
        [NonSerialized] public float CurrentVolume;
        [NonSerialized] public float CurrentWait;

        public RandomDeck<string> Playlist = new RandomDeck<string>(8);
        [NonSerialized] public string Override;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            MusicUtility.ResetPlaylist(this);
        }
    }



    public enum MusicPlaybackMode {
        None,
        Playlist,
        Override
    }

    public enum MusicPlaybackStep {
        None,
        Load,
        Playing,
        FadeOut,
        FadeIn,
        Wait
    }

    static public class MusicUtility {
        public static void SetAllSongs(MusicState state, string[] allSongs)
        {
            state.AllSongs = allSongs;
            state.Override = null;
            state.Step = MusicPlaybackStep.None;
            state.Playlist.Clear();
            ResetPlaylist(state);
        }

        public static void ResetPlaylist(MusicState state)
        {
            foreach (var element in state.AllSongs)
            {
                state.Playlist.Add(element);
            }
        }
    }
}