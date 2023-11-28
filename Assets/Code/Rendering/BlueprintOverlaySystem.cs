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

namespace Zavala.Rendering
{
    /// <summary>
    /// Controls an object that snaps to the tile under the mouse cursor
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 400)]
    public class BlueprintOverlaySystem : SharedStateSystemBehaviour<BlueprintState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Changed number of commits to process
            if (m_State.NumBuildCommitsChanged)
            {
                RegenerateOverlayMesh();
                return;
            }

            // Render destroy bulldozer when in destroy mode
            if (m_State.IsActive && m_State.CommandState == ActionType.Build)
            {
                if (m_State.OverlayMesh == null)
                {
                    // Generate new mesh
                   RegenerateOverlayMesh();
                   return;
                }
            }
            else if (m_State.OverlayMesh != null)
            {
                // Remove the old mesh
                DestroyOverlayMesh();
            }
        }

        private void RegenerateOverlayMesh()
        {
            m_State.OverlayMesh = new Mesh();
        }

        private void DestroyOverlayMesh()
        {
            m_State.OverlayMesh = null;
        }
    }
}