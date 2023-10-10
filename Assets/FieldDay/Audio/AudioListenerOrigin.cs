using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Origin for the audio listener.
    /// </summary>
    public sealed class AudioListenerOrigin : MonoBehaviour {
        [EditModeOnly] public int Priority = 0;
        
        private void OnEnable() {
            
        }

        private void OnDisable() {
            
        }
    }
}