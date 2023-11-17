using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio playback handle.
    /// </summary>
    public struct AudioHandle {
        private readonly UniqueId16 m_Id;
        private readonly AudioVoiceType m_Type;

        internal AudioHandle(UniqueId16 id, AudioVoiceType type) {
            m_Id = id;
            m_Type = type;
        }
    }
}