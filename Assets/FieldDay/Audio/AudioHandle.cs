using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    public struct AudioHandle {
        private readonly UniqueId16 m_Id;

        internal AudioHandle(UniqueId16 id) {
            m_Id = id;
        }
    }
}