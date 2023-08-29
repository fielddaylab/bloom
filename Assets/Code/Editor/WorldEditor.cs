using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.IO;
using UnityEditor;
using UnityEngine;

namespace Zavala.Editor {
    [CustomEditor(typeof(WorldAsset))]
    public class WorldEditor : UnityEditor.Editor {

        public const int BoxSize = 12;
        public const int BoxPadding = 2;
        public const int BoxSizeWithPadding = BoxSize + BoxPadding;

        static private GUIStyle BoxStyle;

        public override void OnInspectorGUI() {
            this.DrawDefaultInspector();

            WorldAsset world = target as WorldAsset;

            GUILayout.Space(10);

            foreach(var region in world.Regions) {
                if (!region.Region) {
                    EditorGUILayout.HelpBox("Null region", MessageType.Error);
                } else {
                    if (region.X % 2 != 0) {
                        EditorGUILayout.HelpBox(string.Format("Region '{0}' is not aligned to 2 x\nColumns will not match source map", region.Region.name), MessageType.Error);
                    }
                    if (region.X + region.Region.Width > world.Width || region.Y + region.Region.Height > world.Height) {
                        EditorGUILayout.HelpBox(string.Format("Region '{0}' extends outside world bounds", region.Region.name), MessageType.Error);
                    }
                }
            }

            GUILayout.Space(10);


            float width = world.Width * BoxSizeWithPadding;
            float height = (world.Height + 0.5f) * BoxSizeWithPadding;
            bool noOverlaps;
            using (new GUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                GUILayout.Box(GUIContent.none, EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height));
                noOverlaps = RenderMap(world);
                GUILayout.FlexibleSpace();
            }

            if (!noOverlaps) {
                EditorGUILayout.HelpBox("Overlaps detected between regions", MessageType.Error);
            }
        }

        private bool RenderMap(WorldAsset asset) {
            if (BoxStyle == null) {
                BoxStyle = new GUIStyle(GUIStyle.none);
                BoxStyle.normal.background = Texture2D.whiteTexture;
                BoxStyle.alignment = TextAnchor.MiddleCenter;
            }

            Dictionary<int, int> mappedIndices = new Dictionary<int, int>(64);
            HashSet<int> overlappedIndices = new HashSet<int>(16);

            Rect totalRect = GUILayoutUtility.GetLastRect();
            totalRect.x += BoxPadding / 2;
            totalRect.y += BoxPadding / 2;
            totalRect.width -= BoxPadding;
            totalRect.height -= BoxPadding;

            HexGridSubregion worldRegion = new HexGridSubregion(new HexGridSize(asset.Width, asset.Height));

            int regionIdx = 0;
            foreach(var region in asset.Regions) {
                try {
                    if (!region.Region) {
                        continue;
                    }
                    HexGridSubregion subRegion = worldRegion.Subregion((ushort) region.X, (ushort) region.Y, (ushort) region.Region.Width, (ushort) region.Region.Height);
                    for(int i = 0; i < subRegion.Size; i++) {
                        if (region.Region.Tiles[i].Category == Sim.TerrainCategory.Void) {
                            continue;
                        }
                        int actualIndex = subRegion.FastIndexToGridIndex(i);
                        if (mappedIndices.ContainsKey(actualIndex)) {
                            overlappedIndices.Add(actualIndex);
                        } else {
                            mappedIndices.Add(actualIndex, regionIdx);
                        }
                    }
                } catch {
                }
                regionIdx++;
            }

            if (Event.current.type == EventType.Repaint) {
                for (int i = 0; i < worldRegion.Size; i++) {
                    int x = i % worldRegion.Width;
                    int y = i / worldRegion.Width;

                    Rect r = new Rect(totalRect.x + x * BoxSizeWithPadding, totalRect.y + (((worldRegion.Height - 1 - y) + ((x & 1) == 0 ? 0.5f : 0)) * BoxSizeWithPadding), BoxSize, BoxSize);

                    if (overlappedIndices.Contains(i)) {
                        GUI.backgroundColor = ColorBank.PaleVioletRed;
                        GUI.Box(r, "x", BoxStyle);
                    } else if (mappedIndices.TryGetValue(i, out int regionIndex)) {
                        GUI.backgroundColor = RegionColors[regionIndex];
                        GUI.Box(r, regionIndex.ToStringLookup(), BoxStyle);
                    }
                }
            }

            GUI.backgroundColor = ColorBank.White;

            return overlappedIndices.Count == 0;
        }

        static protected void MarkDirty(string undoDescription, UnityEngine.Object target) {
            Undo.RecordObject(target, undoDescription);
            EditorUtility.SetDirty(target);
        }

        static private readonly Color[] RegionColors = new Color[] {
            ColorBank.Blue, ColorBank.Red, ColorBank.Plum, ColorBank.Orchid, ColorBank.DarkGreen, ColorBank.White, ColorBank.Yellow, ColorBank.SeaShell, ColorBank.HotPink, ColorBank.Honeydew,
            ColorBank.Fuchsia, ColorBank.CornflowerBlue, ColorBank.Cornsilk, ColorBank.Lime, ColorBank.Gainsboro, ColorBank.Peru
        };
    }
}