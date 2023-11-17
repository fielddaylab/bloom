using System;
using System.Collections.Generic;
using ScriptableBake;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio event package.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Package")]
    public sealed class AudioPackage : ScriptableObject, IBaked {
        #region Inspector

        [SerializeField] private AudioEvent[] m_Events = null;

        #endregion // Inspector

        [NonSerialized] private int m_RefCount;

        /// <summary>
        /// List of all events in this package.
        /// </summary>
        public IReadOnlyList<AudioEvent> Events() { return m_Events; }

        #region Unity Events

        private void OnEnable() {
            m_RefCount = 0;
        }

        private void OnDisable() {
            m_RefCount = 0;
        }

        #endregion // Unity Events

#if UNITY_EDITOR

        private void FindAllEventsInDirectory() {
            Baking.PrepareUndo(this, "locating all audio events in directory");
            string myDir = Baking.GetAssetDirectory(this);
            m_Events = Baking.FindAssets<AudioEvent>(myDir);
        }

        int IBaked.Order { get { return -10;  } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            FindAllEventsInDirectory();
            return true;
        }

        [UnityEditor.CustomEditor(typeof(AudioPackage)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Find All In Directory")) {
                    foreach(AudioPackage pkg in targets) {
                        pkg.FindAllEventsInDirectory();
                    }
                }
            }
        }

#endif // UNITY_EDITOR
    }
}