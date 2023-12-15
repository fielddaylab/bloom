using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Rendering;

namespace Zavala.Roads {
    public class RoadInstanceController : MonoBehaviour
    {
        public Transform RoadMeshTransform;
        public MeshFilter RoadMesh;
        public DecorationRenderer RampSolidDecorations;
        public DecorationRenderer RampStagedDecorations;
        public TileAdjacencyDataSet<RoadRampType> Ramps;
        public TileAdjacencyMask BPCompareMask; // The old staging mask from current blueprint mode session
        public BuildingPreview MatSwap;
        public OccupiesTile Position;
    }

    public enum RoadRampType : byte {
        None,
        Ramp,
        Tall
    }

    static public class RoadVisualUtility {
        static public void UpdateRoadMesh(RoadInstanceController controller, RoadLibrary library, TileAdjacencyMask flowMask, TileAdjacencyMask stageMask) {
            library.Lookup(flowMask | stageMask, out var roadData);

            controller.RoadMesh.sharedMesh = roadData.Mesh;
            controller.RoadMeshTransform.localScale = roadData.Scale;
            controller.RoadMeshTransform.localRotation = roadData.Rotation;

            if (!stageMask.IsEmpty)
            {
                controller.BPCompareMask = stageMask;
            }

            UpdateRampDecorations(controller, library, stageMask, true);
            UpdateRampDecorations(controller, library, flowMask, false);
        }

        static public void ClearBPMask(RoadNetwork network, int tileIndex)
        {
            for (int r = network.RoadObjects.Count - 1; r >= 0; r--)
            {
                if (network.RoadObjects[r].Position.TileIndex == tileIndex)
                {
                    network.RoadObjects[r].BPCompareMask.Clear();
                }
            }

        }

        static private void UpdateRampDecorations(RoadInstanceController controller, RoadLibrary library, TileAdjacencyMask mask, bool isStaging)
        {
            if (isStaging) { controller.RampStagedDecorations.Decorations.Clear(); }
            else { controller.RampSolidDecorations.Decorations.Clear(); }

            for (TileDirection dir = TileDirection.Self + 1; dir < TileDirection.COUNT; dir++)
            {
                if (mask.Has(dir) && controller.Ramps.TryGet(dir, out RoadRampType ramp))
                {
                    int turns = (int)dir - (int)TileDirection.S;
                    Vector3 offset = HexGrid.RotateVector(library.RampMeshOffset(), turns);
                    Quaternion rot = Quaternion.Euler(0, turns * -60, 0);

                    bool blueprintOverride = controller.BPCompareMask[dir] && mask[dir];
                    if (isStaging || blueprintOverride) { DecorationUtility.AddDecoration(controller.RampStagedDecorations, library.RampMesh(ramp), Matrix4x4.TRS(offset, rot, library.RampMeshScale())); }
                    else { DecorationUtility.AddDecoration(controller.RampSolidDecorations, library.RampMesh(ramp), Matrix4x4.TRS(offset, rot, library.RampMeshScale())); }
                }
            }
        }
    }
}