using UnityEngine;
using System;
using BeauUtil;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Asset utility methods.
    /// </summary>
    static public class AssetUtility {
        /// <summary>
        /// Manually unloads the given object.
        /// </summary>
        static public void ManualUnload(UnityEngine.Object obj) {
            if (!ReferenceEquals(obj, null)) {
                if (IsPersistent(obj)) {
                    Debug.LogFormat("[AssetUtility] Manually unloading persistent object '{0}'", obj.name);
                    Resources.UnloadAsset(obj);
                } else {
                    Debug.LogFormat("[AssetUtility] Manually unloading object '{0}'", obj.name);
#if UNITY_EDITOR
                    UnityEngine.Object.Destroy(obj);
#else
                    UnityEngine.Object.DestroyImmediate(obj, true);
#endif // UNITY_EDITOR
                }
            }
        }

        /// <summary>
        /// Manually destroys the given asset.
        /// Use carefully! In builds you won't get this asset back.
        /// </summary>
        static public void DestroyAsset(UnityEngine.Object asset) {
            if (!ReferenceEquals(asset, null)) {
                Assert.True(IsPersistent(asset), "Asset is not persistent");
                Debug.LogWarningFormat("[AssetUtility] Manually destroying asset '{0}'!", asset.name);
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(asset, true);
#else
                Resources.UnloadAsset(asset);
#endif // UNITY_EDITOR
            }
        }

        /// <summary>
        /// Unloads unused assets.
        /// Returns the async operation if asynchronous.
        /// </summary>
        static public AsyncOperation UnloadUnused() {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.UnloadUnusedAssetsImmediate(true);
                return null;
            }
#endif // UNITY_EDITOR
            return Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Returns if the given asset is persistent.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPersistent(UnityEngine.Object obj) {
            return UnityHelper.IsPersistent(obj);
        }
    }
}