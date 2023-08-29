using System;
using System.Collections.Generic;
using System.Drawing;
using BeauUtil.IO;
using UnityEditor;
using UnityEngine;

namespace Zavala.Editor {
    [CustomEditor(typeof(RegionAsset)), CanEditMultipleObjects]
    public class RegionEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            if (targets.Length == 1) {
                SingleEditor();
            } else {
                MultiEditor();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SingleEditor() {
            RegionAsset region = target as RegionAsset;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("LeafScript"));

            EditorGUILayout.LabelField("Tiled File", EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(string.IsNullOrEmpty(region.SourceFilePath) ? "[No File]" : region.SourceFilePath);
                if (GUILayout.Button("Select File", GUILayout.Width(80))) {
                    string newFile = EditorUtility.OpenFilePanel("Select Tiled File", Environment.CurrentDirectory, "json");
                    if (!string.IsNullOrEmpty(newFile)) {
                        MarkDirty("Updated tiled file", region);
                        region.SourceFilePath = IOHelper.GetRelativePath(newFile);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(region.SourceFilePath))) {
                if (GUILayout.Button("Import")) {
                    Import(region);
                }
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Import Results", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUILayout.LabelField("Grid Size", string.Format("{0}x{1} ({2})", region.Width, region.Height, region.Width * region.Height));
                EditorGUILayout.IntField("Buildings", region.Buildings.Length);
                EditorGUILayout.IntField("Roads", region.Roads.Length);
                EditorGUILayout.IntField("Modifiers", region.Modifiers.Length);
                EditorGUILayout.IntField("Named Points", region.Points.Length);
            }
        }

        private void MultiEditor() {
            EditorGUILayout.HelpBox("Multiple RegionAssets selected", MessageType.Info);

            if (GUILayout.Button("Import")) {
                foreach(RegionAsset region in targets) {
                    Import(region);
                }
            }
        }

        protected void Import(RegionAsset region) {
            var result = RegionImport.TryReadTiledJSON(region.SourceFilePath, out var tileData);
            if (result != RegionImport.ErrorCode.Success) {
                EditorUtility.DisplayDialog("Import Failed for " + region.name, result.ToString(), "Whoops");
                return;
            }

            MarkDirty("Importing data", region);

            RegionImport.ReadMapSize(ref tileData);
            RegionImport.ReadLayers(ref tileData);
            RegionImport.ReadTileOffsets(ref tileData);

            region.Width = (int) tileData.GridSize.Width;
            region.Height = (int) tileData.GridSize.Height;

            HashSet<int> occupiedIndices = new HashSet<int>(region.Width);

            region.Tiles = RegionImport.ReadTerrain(tileData, 50);
            RegionImport.ReadStaticConstructions(tileData, occupiedIndices, region.Tiles, out region.Buildings, out region.Modifiers);
            region.Roads = RegionImport.ReadRoads(tileData, occupiedIndices);
            region.Points = RegionImport.ReadScriptPoints(tileData);

            Debug.LogFormat("[RegionEditor] Imported region information from '{0}'!", region.SourceFilePath);
        }

        static protected void MarkDirty(string undoDescription, UnityEngine.Object target) {
            Undo.RecordObject(target, undoDescription);
            EditorUtility.SetDirty(target);
        }

        static protected void MarkDirtyAll(string undoDescription, UnityEngine.Object[] targets) {
            Undo.RecordObjects(targets, undoDescription);
            foreach (var target in targets) {
                EditorUtility.SetDirty(target);
            }
        }
    }
}