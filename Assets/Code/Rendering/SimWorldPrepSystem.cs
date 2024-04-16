using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.Sim;

namespace Zavala.World {
    [SysUpdate(GameLoopPhase.LateUpdate, -100)]
    public sealed class SimWorldPrepSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, SimWorldCamera> {
        #region Inspector



        #endregion // Inspector

        [NonSerialized] private Plane[] m_CachedPlaneArray = new Plane[6];

        #region Work

        public override void ProcessWork(float deltaTime) {
            // frustrum culling
            GeometryUtility.CalculateFrustumPlanes(m_StateC.Camera, m_CachedPlaneArray);

            int prevWorldRegionCount = (int) m_StateA.RegionCount;
            int regionsChanged = (int) m_StateB.RegionCount - prevWorldRegionCount;

            m_StateA.RegionCount = m_StateB.RegionCount;
            m_StateA.NewRegions = regionsChanged;
            if (regionsChanged > 0) {
                for (int i = 0; i < regionsChanged; i++) {
                    int idx = prevWorldRegionCount + i;
                    RegionInfo regionInfo = m_StateB.Regions[idx];
                    Bounds approximateBounds = RegionUtility.CalculateApproximateWorldBounds(regionInfo.GridArea, m_StateA.WorldSpace, regionInfo.MaxHeight, m_StateA.BottomBounds, m_StateA.BoundsExpand);
                    BoundingSphere approximateSphere = RegionUtility.CalculateApproximateWorldSphere(regionInfo.Edges, approximateBounds.center, m_StateB.HexSize, m_StateA.WorldSpace, regionInfo.MaxHeight, m_StateA.BoundsExpand);
                    m_StateA.RegionBounds[idx] = approximateBounds;
                    m_StateA.RegionSpheres[idx] = approximateSphere;
                }

                // approximate hull
                ApproximateHull(m_StateA.RegionBounds, (int) m_StateA.RegionCount, out m_StateA.CameraBounds);
            }

            float frustumWidth = CameraHelper.HeightForDistanceAndFOV(-m_StateC.Camera.transform.localPosition.z, m_StateC.Camera.fieldOfView) * m_StateC.Camera.aspect;
            m_StateA.RegionCullingMask = CullingHelper.EvaluateRegionVisibilityMask(m_StateA.RegionBounds, m_StateA.RegionSpheres, (int) m_StateA.RegionCount, m_StateC.LookTarget.position, frustumWidth, m_CachedPlaneArray);
        }

        #endregion // Work

        static private unsafe void ApproximateHull(SimBuffer<Bounds> bounds, int count, out Rect rect) {
            int pointCount = count * 4;
            Vector2* points = stackalloc Vector2[pointCount];
            for(int i = 0; i < count; i++) {
                Bounds b = bounds[i];
                Vector3 min = b.min, max = b.max;
                points[i * 4 + 0] = new Vector2(min.x, min.z);
                points[i * 4 + 1] = new Vector2(max.x, min.z);
                points[i * 4 + 2] = new Vector2(max.x, max.z);
                points[i * 4 + 3] = new Vector2(min.x, max.z);
            }
            HullGeneration.ComputeFastRect(points, pointCount, out rect);
        }
    }

    /// <summary>
    /// Culling/visibility helper functions.
    /// </summary>
    static public class CullingHelper {
        // TODO: more robust intersection tests? culling with frustum planes to aabb has a lot of false positives
        static public uint EvaluateRegionVisibilityMask(SimBuffer<Bounds> regionBounds, SimBuffer<BoundingSphere> regionSpheres, int count, Vector3 cameraCenter, float cameraFrustumWidth, Plane[] frustumPlanes) {
            uint mask = 0;
            for(int i = 0; i < count; i++) {
                BoundingSphere sphere = regionSpheres[i];
                Bounds bounds = regionBounds[i];

                Vector3 adjCameraCenter = cameraCenter;
                adjCameraCenter.y = sphere.position.y;

                Vector3 centerTest = bounds.ClosestPoint(adjCameraCenter);

                float distToCenter = Vector3.Distance(centerTest, adjCameraCenter);
                if (distToCenter > cameraFrustumWidth / 2) {
                    continue;
                }

                bool sphereInFrustum = false;
                for(int j = 0; j < 4; j++) {
                    Plane p = frustumPlanes[j];
                    float dist = p.GetDistanceToPoint(sphere.position);
                    if (dist < sphere.radius) {
                        sphereInFrustum = true;
                        break;
                    }
                }

                if (!sphereInFrustum) {
                    continue;
                }

                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) {
                    continue;
                }

                mask |= 1u << i;
            }
            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsRegionVisible(uint mask, int index) {
            return Bits.Contains(mask, index);
        }
    }
}