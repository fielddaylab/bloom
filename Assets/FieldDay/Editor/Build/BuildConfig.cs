using System;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    /// <summary>
    /// Build configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Build System/Build Configuration")]
    public class BuildConfig : ScriptableObject {
        public string[] BranchNamePatterns;
        public bool DevelopmentBuild;

        [Multiline]
        public string CustomDefines;

        public int Order;
    }
}