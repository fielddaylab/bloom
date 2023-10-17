using FieldDay.Scenes;
using UnityEngine;
using FieldDay;
using System;
using BeauUtil;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Zavala {
    public sealed class RoadPrefabData : MonoBehaviour, IDevModeOnly {
#if UNITY_EDITOR

        public Mesh Mesh;
        public TileAdjacencyMask Mask;
        public float Radius;

        private void Reset() {
            Mesh = GetComponent<MeshFilter>().sharedMesh;
        }

        [CustomEditor(typeof(RoadPrefabData))]
        private class Inspector : Editor {
            private void OnSceneGUI() {
                RoadPrefabData prefabData = target as RoadPrefabData;
                if (!Frame.IsActive(prefabData)) {
                    return;
                }

                Vector3 currentPos = prefabData.transform.position;
                float newRadius = Handles.RadiusHandle(Quaternion.identity, currentPos, prefabData.Radius);
                Update(prefabData, ref prefabData.Radius, newRadius);

                TileAdjacencyMask mask = prefabData.Mask;

                for(TileDirection dir = (TileDirection) 1; dir < TileDirection.COUNT; dir++) {
                    Vector3 offset = currentPos + Geom.SwizzleYZ(HexVector.Offset2D(dir)) * (newRadius + 0.2f);
                    Handles.color = mask[dir] ? Color.yellow : Color.white;
                    if (Handles.Button(offset, Quaternion.identity, 0.1f, 0.1f, Handles.SphereHandleCap)) {
                        mask[dir] = !mask[dir];
                    }
                }

                Update(prefabData, ref prefabData.Mask, mask);
            }

            static private void Update<T>(UnityEngine.Object host, ref T val, in T newVal) {
                if (!CompareUtils.DefaultEquals<T>().Equals(val, newVal)) {
                    Undo.RecordObject(host, "Updating property");
                    EditorUtility.SetDirty(host);
                    val = newVal;
                }
            }
        }

#endif // UNITY_EDITOR
    }
}