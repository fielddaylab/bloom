using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/Sprite Library")]
    public sealed class SpriteLibrary : ScriptableObject, IEditorOnlyData {
        [Serializable]
        private struct Entry {
            public SerializedHash32 Id;
            public Sprite Sprite;
        }

        [SerializeField] private Sprite[] m_Sprites = Array.Empty<Sprite>();
        [SerializeField] private Entry[] m_Entries = Array.Empty<Entry>();
        private Dictionary<StringHash32, Sprite> m_Map;

        private void BuildLookup() {
            if (m_Map == null) {
                m_Map = new Dictionary<StringHash32, Sprite>(m_Entries.Length, CompareUtils.DefaultEquals<StringHash32>());
                foreach (var sprite in m_Sprites) {
                    if (sprite == null) {
                        Log.Error("[SpriteLibrary] Error: Missing sprite in {0}", name);
                        continue;
                    }
                    Log.Msg("[SpriteLibrary] Adding sprite {0}",sprite.name);
                    m_Map.Add(sprite.name, sprite);
                }
               
                foreach (var entry in m_Entries) {
                    StringHash32 id = entry.Id;
                    if (id.IsEmpty) {
                        id = entry.Sprite.name;
                    }
                    Log.Msg("[SpriteLibrary] Adding entry {0}", id);
                    m_Map.Add(id, entry.Sprite);
                }
            }
        }

        public bool TryLookup(StringHash32 id, out Sprite sprite) {
            if (m_Map == null) {
                BuildLookup();
            }
            return m_Map.TryGetValue(id, out sprite);
        }

        public void Prewarm() {
            BuildLookup();
        }

        private void OnEnable() {
            if (Application.isPlaying) {
                BuildLookup();
            }
        }

#if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            for(int i = 0; i < m_Entries.Length; i++) {
                EditorOnlyData.Strip(ref m_Entries[i].Id);
            }
        }

#endif // UNITY_EDITOR
    }
}