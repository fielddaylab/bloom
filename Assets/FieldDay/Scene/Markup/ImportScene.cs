using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Scenes {
    /// <summary>
    /// Loads another scene upon scene load.
    /// </summary>
    public sealed class ImportScene : MonoBehaviour {
        public SceneReference Scene;
        [AutoEnum] public SceneImportFlags Flags = SceneImportFlags.MergeWithActive;
    }

    /// <summary>
    /// Scene import settings.
    /// </summary>
    public struct SceneImportSettings {
        public string Path;
        public SceneImportFlags Flags;
        public Matrix4x4? RootTransform;
    }

    /// <summary>
    /// Dynamic scene import.
    /// </summary>
    public interface IDynamicSceneImport {
        IEnumerable<SceneImportSettings> GetSubscenes();
    }

    /// <summary>
    /// Scene import behavior flags.
    /// </summary>
    [Flags]
    public enum SceneImportFlags : uint {
        /// <summary>
        /// Additive scene load.
        /// By default, scenes will load additively at runtime.
        /// </summary>
        Additive = 0,

        /// <summary>
        /// Merges the scene with the active scene.
        /// This can be merged in at bake/build time.
        /// </summary>
        MergeWithActive = 0x01,
        
        /// <summary>
        /// Imports lighting data into the main scene.
        /// Only valid when MergeWithActive is also set.
        /// </summary>
        ImportLightingSettings = 0x02
    }
}