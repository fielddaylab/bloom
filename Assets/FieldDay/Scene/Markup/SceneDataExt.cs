using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Scenes {
    /// <summary>
    /// Extended scene data.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-10000000), AddComponentMenu("")]
    internal sealed class SceneDataExt : MonoBehaviour {
        /// <summary>
        /// Preload data.
        /// </summary>
        public PreloadManifest Preload;

        /// <summary>
        /// Subscenes to import (but not merge).
        /// </summary>
        public ImportScene[] SubScenes;

        /// <summary>
        /// Dynamic subscenes to import.
        /// </summary>
        public Component[] DynamicSubscenes;

        #region Tracking

        static private readonly List<SceneDataExt> s_Loaded = new List<SceneDataExt>(4);

        private void OnEnable() {
            s_Loaded.Add(this);
        }

        private void OnDisable() {
            s_Loaded.FastRemove(this);
        }

        /// <summary>
        /// Gets all currently loaded instances.
        /// </summary>
        static internal int GetAllInstances(IList<SceneDataExt> output) {
            foreach (var current in s_Loaded) {
                output.Add(current);
            }
            return s_Loaded.Count;
        }

        /// <summary>
        /// Gets all currently loaded instances for the given scene.
        /// </summary>
        static internal int GetInstances(IList<SceneDataExt> output, Scene scene) {
            int count = 0;
            foreach (var current in s_Loaded) {
                if (current.gameObject.scene == scene) {
                    output.Add(current);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets the first loaded instance for the given scene.
        /// </summary>
        static internal SceneDataExt GetInstance(Scene scene) {
            foreach (var current in s_Loaded) {
                if (current.gameObject.scene == scene) {
                    return current;
                }
            }
            return null;
        }

        #endregion // Tracking
    }
}