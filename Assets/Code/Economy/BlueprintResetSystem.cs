using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Economy
{
    // Process after BlueprintOverlaySystem and BlueprintSystem
    [SysUpdate(GameLoopPhase.Update, 500)]
    public class BlueprintResetSystem : SharedStateSystemBehaviour<BlueprintState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Reset any triggers that were fired

            // Blueprint mode opened
            if (m_State.StartBlueprintMode) { 
                m_State.StartBlueprintMode = false; 
            }

            // Exited blueprint mode
            if (m_State.ExitedBlueprintMode) { 
                m_State.ExitedBlueprintMode = false;
            }

            // Build clicked
            if (m_State.NewBuildConfirmed) { m_State.NewBuildConfirmed = false; }

            // Changed number of commits to process
            if (m_State.NumBuildCommitsChanged) { m_State.NumBuildCommitsChanged = false; }

            // Changed number of commits to process
            if (m_State.NumDestroyActionsChanged) { m_State.NumDestroyActionsChanged = false; }

            // Clicked the Undo button (in Build mode)
            if (m_State.UndoClickedBuild) { m_State.UndoClickedBuild = false; }

            // Clicked the Undo button (in Build mode)
            if (m_State.UndoClickedDestroy) { m_State.UndoClickedDestroy = false; }

            // Clicked the Destroy Mode button (from Build mode)
            if (m_State.DestroyModeClicked) { m_State.DestroyModeClicked = false; }

            // Destroy clicked
            if (m_State.NewDestroyConfirmed) { m_State.NewDestroyConfirmed = false; }

            // Clicked the Exit button (from Destroy mode)
            if (m_State.CanceledDestroyMode) { m_State.CanceledDestroyMode = false; }
        }
    }
}