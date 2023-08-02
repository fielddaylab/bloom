using System;
using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
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
                    m_StateA.RegionBounds[idx] = approximateBounds;
                }
            }

            m_StateA.RegionCullingMask = CullingHelper.EvaluateRegionVisibilityMask(m_StateA.RegionBounds, (int) m_StateA.RegionCount, m_CachedPlaneArray);

            for(int i = 0; i < m_StateA.RegionCount; i++) {
                DebugDraw.AddBounds(m_StateA.RegionBounds[i], (m_StateA.RegionCullingMask & (1 << i)) != 0 ? Color.green : Color.red, 1, 0, true, 0);
            }
        }

        #endregion // Work
    }

    /// <summary>
    /// Culling/visibility helper functions.
    /// </summary>
    static public class CullingHelper {
        // TODO: more robust intersection tests? culling with frustum planes to aabb has a lot of false positives
        static public uint EvaluateRegionVisibilityMask(SimBuffer<Bounds> regionCounts, int count, Plane[] frustumPlanes) {
            uint mask = 0;
            for(int i = 0; i < count; i++) {
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, regionCounts[i])) {
                    mask |= 1u << i;
                }
            }
            return mask;
        }

        static public bool IsRegionVisible(uint mask, int index) {
            return Bits.Contains(mask, index);
        }
    }
}