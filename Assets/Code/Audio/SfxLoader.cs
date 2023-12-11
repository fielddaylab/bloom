using System;
using System.Collections.Generic;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scenes;
using UnityEngine;

namespace Zavala.Audio {
    public class SfxLoader : MonoBehaviour {
        public AudioClip[] Clips;
        public SfxAsset[] Assets;

        private void OnEnable() {
            SfxState sfx = Game.SharedState.Get<SfxState>();
            foreach(var clip in Clips) {
                sfx.LoadedClips.Add(clip.name, clip);
            }

            foreach (var asset in Assets) {
                sfx.LoadedSfxAssets.Add(asset.name, asset);
            }
        }

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            SfxState sfx = Game.SharedState.Get<SfxState>();
            foreach (var clip in Clips) {
                sfx.LoadedClips.Remove(clip.name);
            }

            foreach (var asset in Assets) {
                sfx.LoadedSfxAssets.Remove(asset.name);
            }
        }
    }
}