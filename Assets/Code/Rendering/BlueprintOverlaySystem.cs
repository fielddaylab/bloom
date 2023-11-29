using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;
using BeauUtil;
using UnityEngine.Rendering;
using Zavala.Roads;

namespace Zavala.Rendering
{
    /// <summary>
    /// Controls an object that snaps to the tile under the mouse cursor
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 450)]
    public class BlueprintOverlaySystem : SharedStateSystemBehaviour<BlueprintState, SimGridState, SimWorldState, RoadNetwork>
    {
        private BuildToolState m_StateE;

        public override void ProcessWork(float deltaTime)
        {
            if (!m_StateE)
            {
                m_StateE = Game.SharedState.Get<BuildToolState>();
            }

            if (m_StateE.ToolUpdated)
            {
                // Regen when non-road tool is selected
                if (m_StateE.ActiveTool != UserBuildTool.Road && m_StateE.ActiveTool != UserBuildTool.None)
                {
                    BlueprintUtility.RegenerateOverlayMesh(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE);
                }
                else
                {
                    // Remove the old mesh
                    BlueprintUtility.HideOverlayMesh(m_StateA);
                }
                return;
            }
        }
    }
}