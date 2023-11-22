using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Roads {
    public class RoadInstanceController : MonoBehaviour
    {
        public Transform RoadMeshTransform;
        public MeshFilter RoadMesh;
        public DecorationRenderer RampDecorations;
        public TileAdjacencyDataSet<RoadRampType> Ramps;
        public float Radius;
    }

    public enum RoadRampType : byte {
        None,
        Ramp,
        Tall
    }

    static public class RoadVisualUtility {
        static public void UpdateRoadMesh(RoadInstanceController controller, RoadLibrary library, TileAdjacencyMask mask) {
            library.Lookup(mask, out var roadData);

            controller.RoadMesh.sharedMesh = roadData.Mesh;
            controller.RoadMeshTransform.localScale = roadData.Scale;
            controller.RoadMeshTransform.localRotation = roadData.Rotation;

            controller.RampDecorations.Decorations.Clear();

            // TODO: Set material based on current blueprint state
            for(TileDirection dir = TileDirection.Self + 1; dir < TileDirection.COUNT; dir++) {
                if (mask.Has(dir) && controller.Ramps.TryGet(dir, out RoadRampType ramp)) {
                    int turns = (int) dir - (int) TileDirection.S;
                    Vector3 offset = HexGrid.RotateVector(new Vector3(0, 0, -controller.Radius), turns);
                    Quaternion rot = Quaternion.Euler(0, turns * -60, 0);
                    DecorationUtility.AddDecoration(controller.RampDecorations, library.RampMesh(ramp), Matrix4x4.TRS(offset, rot, library.RampMeshScale()));
                }
            }
        }
    }
}