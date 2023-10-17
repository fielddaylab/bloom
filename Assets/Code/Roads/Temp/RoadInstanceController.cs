using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Roads {
    public class RoadInstanceController : MonoBehaviour
    {
        public Transform RoadMeshTransform;
        public MeshFilter RoadMesh;
    }

    static public class RoadVisualUtility {
        static public void UpdateRoadMesh(RoadInstanceController controller, RoadLibrary library, TileAdjacencyMask mask) {
            library.Lookup(mask, out var roadData);

            controller.RoadMesh.sharedMesh = roadData.Mesh;
            controller.RoadMeshTransform.localScale = roadData.Scale;
            controller.RoadMeshTransform.localRotation = roadData.Rotation;
        }
    }
}