using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio bus mixing layer.
    /// </summary>
    [AddComponentMenu("Field Day/Audio/Mix Layer")]
    public sealed class AudioMixLayer : MonoBehaviour {
        [EditModeOnly] public int Priority = 0;
        
        private void OnEnable() {
            
        }

        private void OnDisable() {
            
        }
    }
}