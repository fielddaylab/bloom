using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using UnityEditor;
using UnityEngine;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [CreateAssetMenu(menuName = "Zavala/Road Library")]
    public sealed class RoadLibrary : ScriptableObject {

        [Serializable]
        private struct RotationEntry {
            public ushort TileIndex;
            public ushort Rotation;
        }

        [Serializable]
        private struct TileData {
            public Mesh Mesh;
            public Quaternion Rotation;
            public float Scale;
            public bool FlipX;
        }

        public struct AssembledRoadData {
            public Mesh Mesh;
            public Quaternion Rotation;
            public Vector3 Scale;
            public int Turns;
        }

        [SerializeField] private float m_RadiusReference = 0.43f;
        [SerializeField] private Mesh m_ErrorMesh = null;
        [SerializeField] private Mesh m_RampMesh = null;
        [SerializeField] private Mesh m_SteepRampMesh = null;
        [SerializeField] private Vector3 m_RampScale = Vector3.one;

        [Space]
        [SerializeField] private RotationEntry[] m_RotationEntries = new RotationEntry[64];
        [SerializeField] private TileData[] m_Tiles = new TileData[16];

        public bool Lookup(TileAdjacencyMask mask, out AssembledRoadData roadData) {
            int lookup = mask.Value >> 1;
            RotationEntry rotData = m_RotationEntries[lookup];
            if (rotData.TileIndex == ushort.MaxValue) {
                Log.Error("[RoadLibrary] No road data available for mask {0}", mask.ToString());
                roadData = new AssembledRoadData() {
                    Mesh = m_ErrorMesh,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,
                    Turns = 0
                };
                return false;
            }

            TileData tileData = m_Tiles[rotData.TileIndex];

            roadData.Mesh = tileData.Mesh;
            roadData.Rotation = HexGrid.RotateQuaternion(tileData.Rotation, rotData.Rotation);
            roadData.Scale = new Vector3(tileData.Scale, tileData.Scale, tileData.Scale);
            if (tileData.FlipX) {
                roadData.Scale.x = -roadData.Scale.x;
            }
            roadData.Turns = rotData.Rotation;
            return true;
        }

        public Mesh RampMesh(RoadRampType type) {
            switch (type) {
                case RoadRampType.Ramp:
                    return m_RampMesh;
                case RoadRampType.Tall:
                    return m_SteepRampMesh;
                default:
                    return null;
            }
        }

        public Vector3 RampMeshScale() {
            return m_RampScale;
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(RoadLibrary))]
        private class Inspector : Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                RoadLibrary lib = target as RoadLibrary;

                if (GUILayout.Button("Rebuild")) {
                    lib.Build();
                }
            }
        }

        [ContextMenu("Refresh")]
        private void Build() {
            Undo.RecordObject(this, "rebuilding road data");
            EditorUtility.SetDirty(this);

            RoadPrefabData[] allPrefabs = FindAllRoadPrefabs(Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)));
            Construct(allPrefabs, out m_RotationEntries, out m_Tiles, m_RadiusReference);
        }

        static private RoadPrefabData[] FindAllRoadPrefabs(string directory) {
            string[] assetGuids = AssetDatabase.FindAssets("t:GameObject", new string[] { directory });
            List<RoadPrefabData> prefabs = new List<RoadPrefabData>(assetGuids.Length);
            foreach(var guid in assetGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj && obj.TryGetComponent(out RoadPrefabData component)) {
                    prefabs.Add(component);
                }
            }
            return prefabs.ToArray();
        }

        static private void Construct(RoadPrefabData[] prefabDatas, out RotationEntry[] outRotations, out TileData[] outTiles, float radiusReference) {
            outTiles = new TileData[prefabDatas.Length];
            outRotations = new RotationEntry[64];

            HashSet<int> untouchedMasks = new HashSet<int>(64);
            for(int i = 0; i < 64; i++) {
                outRotations[i].TileIndex = ushort.MaxValue;
                if (Bits.Count(i) > 1) {
                    untouchedMasks.Add(i);
                }
            }

            int tileIndex = 0;

            try {
                foreach (var data in prefabDatas) {
                    EditorUtility.DisplayProgressBar("Analyzing Roads", data.gameObject.name, tileIndex / (float) prefabDatas.Length);
                    Log.Msg("Analyzing road {0}", data.gameObject.name);

                    int shiftedMask = data.Mask.Value >> 1;

                    outTiles[tileIndex] = new TileData() {
                        Mesh = data.Mesh,
                        Rotation = data.transform.localRotation,
                        Scale = radiusReference / data.Radius,
                        FlipX = data.transform.localScale.x < 0
                    };

                    int rotCount = 0;
                    int appliedCount = 0;
                    while (rotCount < 6) {
                        ref RotationEntry entry = ref outRotations[shiftedMask];
                        if (entry.TileIndex == ushort.MaxValue) {
                            entry.TileIndex = (ushort) tileIndex;
                            entry.Rotation = (ushort) rotCount;
                            appliedCount++;
                        } else if (entry.TileIndex != tileIndex) {
                            Log.Error("Tile rotation overlap - {0} rot {1} vs {2} rot {3}", data.Mesh.name, rotCount, outTiles[entry.TileIndex].Mesh.name, entry.Rotation);
                        }

                        untouchedMasks.Remove(shiftedMask);

                        shiftedMask = Rotate(shiftedMask);
                        rotCount++;
                    }

                    Log.Msg("filled {0} entries", appliedCount);
                    tileIndex++;
                }
            } finally {
                EditorUtility.ClearProgressBar();
            }

            if (untouchedMasks.Count > 0) {
                Log.Warn("{0} untouched masks", untouchedMasks.Count);
                foreach(var mask in untouchedMasks) {
                    Log.Warn(MaskToString(mask));
                }
            }

            Log.Msg("Road analysis complete");
        }

        private const int BitCount = 6;
        private const int BitMask = (1 << BitCount) - 1;

        static private string MaskToString(int value) {
            return new TileAdjacencyMask(value << 1).ToString();
        }

        static private int Rotate(int value) {
            return ((value >> 1) | (value << (BitCount - 1))) & BitMask;
        }

#endif // UNITY_EDITOR
    }
}