using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using FieldDay.Debugging;
using ScriptableBake;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Editor {
    /// <summary>
    /// Strips editor-only data from scene objects.
    /// </summary>
    public class StripEditorDataSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10000; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            List<IEditorOnlyData> toStrip = new List<IEditorOnlyData>(256);
            scene.GetAllComponents(true, toStrip);

            if (toStrip.Count > 0) {
                Debug.LogFormat("[StripEditorDataSceneProcessor] Found {0} objects with editor-only data...", toStrip.Count);
                using(Profiling.Time("stripping editor-only data")) {
                    foreach(var obj in toStrip) {
                        obj.ClearEditorData(EditorUserBuildSettings.development);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Strips GameLoop GameObjects, from all scenes except for the initial scene.
    /// </summary>
    public class StripGameLoopSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            if (scene.buildIndex == 0) {
                return;
            }

            GameLoop[] toRemove = GameObject.FindObjectsOfType<GameLoop>();
            if (toRemove.Length > 0) {
                Debug.LogFormat("[StripGameLoopSceneProcessor] Removing GameLoop GameObjects from scene '{0}'", scene.name);
                foreach(var obj in toRemove) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Strips DebugDraw components, and DebugConsole GameObjects, from non-development builds.
    /// </summary>
    public class StripDebugSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 10; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUserBuildSettings.development) {
                return;
            }

            DebugDraw[] toRemoveDraw = GameObject.FindObjectsOfType<DebugDraw>();
            if (toRemoveDraw.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing DebugDraw objects from scene '{0}'", scene.name);
                foreach (var obj in toRemoveDraw) {
                    GameObject.DestroyImmediate(obj);
                }
            }

            DebugConsole[] toRemoveConsole = GameObject.FindObjectsOfType<DebugConsole>();
            if (toRemoveConsole.Length > 0) {
                Debug.LogFormat("[StripDebugSceneProcessor] Removing DebugConsole GameObjects from scene '{0}'", scene.name);
                foreach (var obj in toRemoveConsole) {
                    GameObject.DestroyImmediate(obj.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Executes any custom bake processes for scene objects.
    /// </summary>
    public class BakeSceneProcessor : IProcessSceneWithReport {
        public int callbackOrder { get { return 20; } }

        public void OnProcessScene(Scene scene, BuildReport report) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            using(Profiling.Time("baking objects")) {
                Baking.BakeScene(scene, 0);
            }
        }
    }
}