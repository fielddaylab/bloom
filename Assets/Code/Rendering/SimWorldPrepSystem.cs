using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.LateUpdate, -100)]
    public sealed class SimWorldPrepSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState> {
        #region Inspector



        #endregion // Inspector

        [NonSerialized] private Plane[] m_CachedPlaneArray = new Plane[6];

        #region Work

        public override void ProcessWork(float deltaTime) {
            // frustrum culling
            GeometryUtility.CalculateFrustumPlanes(m_StateA.RenderCamera, m_CachedPlaneArray);

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
                DebugDraw.AddBounds(m_StateA.RegionBounds[i], Color.red);
            }
        }

        #endregion // Work
    }

    /// <summary>
    /// Culling/visibility helper functions.
    /// </summary>
    static public class CullingHelper {
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