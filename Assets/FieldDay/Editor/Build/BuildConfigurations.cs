using System;
using System.IO;
using System.Threading;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FieldDay.Editor {
    static public class BuildConfigurations {
        /// <summary>
        /// Returns the build config that matches the given branch name.
        /// </summary>
        static public BuildConfig GetDesiredConfig(string branchName) {
            if (string.IsNullOrEmpty(branchName)) {
                Debug.LogWarningFormat("No branch name?");
                return null;
            }

            BuildConfig[] configs = AssetDBUtils.FindAssets<BuildConfig>(); 
            Array.Sort(configs, (a, b) => a.Order - b.Order);

            //Debug.LogFormat("Found {0} build configurations when lookup under branch '{1}'", configs.Length, branchName);

            for(int buildIdx = 0; buildIdx < configs.Length; buildIdx++) {
                BuildConfig config = configs[buildIdx];
                if (WildcardMatch.Match(branchName, config.BranchNamePatterns, '*', true)) {
                    return config;
                }
            }

            return null;
        }

        static private readonly string LibraryBuildConfigFile = "Library/LastAppliedBuildConfig.txt";

        /// <summary>
        /// Applies the given build configuration settings.
        /// </summary>
        static public void ApplyBuildConfig(string branchName, string configName, bool development, string defines, bool forceLogs = false) {
            bool logging = forceLogs;
            if (!logging) {
                if ((InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)) {
                    logging = true;
                    Debug.LogFormat("batch mode?");
                } else if (File.Exists(LibraryBuildConfigFile)) {
                    string lastApplied = File.ReadAllText(LibraryBuildConfigFile);
                    //Debug.LogFormat("last config is '{0}' vs now '{1}'", lastApplied, configName);
                    logging = lastApplied != configName; 
                }
            }
             
            if (logging) {
                Debug.LogFormat("[BuildConfigurations] Source control branch is '{0}', applying build configuration '{1}'", branchName, configName);
            }

            EditorUserBuildSettings.development = development; 
            BuildUtils.WriteDefines(defines);

            try {
                File.WriteAllText(LibraryBuildConfigFile, configName);
                //Debug.LogFormat("Wrote config '{0}' to file {1} (attributes {2})", configName, Path.GetFullPath(LibraryBuildConfigFile), File.GetAttributes(LibraryBuildConfigFile));
            } catch(Exception e) {
                Debug.LogException(e);
            }

            if (logging && !InternalEditorUtility.inBatchMode) {
                EditorApplication.delayCall += () => BuildUtils.ForceRecompile();
            }
        }

        /// <summary>
        /// Enables BuildInfoGenerator.
        /// </summary>
        [InitializeOnLoadMethod]
        static private void EnableBuildInfo() {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;
        }

        /// <summary>
        /// Retrieves the best configuration for the current branch and applies it.
        /// </summary>
        static public void RetrieveAndApplyConfig(bool forceLogging = false) {
            string branchName = BuildUtils.GetSourceControlBranchName();
            BuildConfig config = GetDesiredConfig(branchName);
            if (config != null) {
                ApplyBuildConfig(branchName, AssetDatabase.GetAssetPath(config), config.DevelopmentBuild, config.CustomDefines, forceLogging);
            } else {
                Debug.LogWarningFormat("no build config found??");
                ApplyBuildConfig(branchName, "[fallback]", true, "DEVELOPMENT,PRESERVE_DEBUG_SYMBOLS", forceLogging);
            }
        }

        [InitializeOnLoadMethod]
        static private void Init_RefreshConfig() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) {
                return;
            }

            RetrieveAndApplyConfig(false); 
        }

        [MenuItem("Assets/Refresh Build Configuration", priority = 2000)]
        static private void Menu_RefreshConfig() {
            RetrieveAndApplyConfig(true);
        }
    }
}