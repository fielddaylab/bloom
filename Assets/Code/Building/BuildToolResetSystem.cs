using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Building
{
    // Process after BlueprintOverlaySystem and BlueprintSystem
    [SysUpdate(GameLoopPhase.Update, 500)]
    public class BuildToolResetSystem : SharedStateSystemBehaviour<BuildToolState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Reset any triggers that were fired

            if (m_State.ToolUpdated)
            {
                m_State.ToolUpdated = false;
            }
            if (m_State.RegionSwitched)
            {
                m_State.RegionSwitched = false;
            }
        }
    }
}
