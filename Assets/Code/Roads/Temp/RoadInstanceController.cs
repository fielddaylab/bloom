using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Roads {
    public class RoadInstanceController : MonoBehaviour
    {
        public GameObject[] DirSegments;

        public void UpdateSegmentVisuals(TileAdjacencyMask roadFlowMask) {
            for (int i = (int)TileDirection.SW; i < (int)TileDirection.COUNT; i++) {
                // activate / deactivate direction segment base on road flow
                DirSegments[i - 1].SetActive(roadFlowMask[(TileDirection)i]);   
            }
        }
    }
}