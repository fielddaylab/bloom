using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Sim;

namespace Zavala.Economy
{
    public class BlueprintSystem : SharedStateSystemBehaviour<BlueprintState, ShopState, SimGridState, BuildToolState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // --- Process UI triggers

            // Build clicked
            if (m_StateA.NewBuildConfirmed)
            {
                m_StateA.NewBuildConfirmed = false;

                BlueprintUtility.ConfirmBuild(m_StateA, m_StateB, m_StateC.CurrRegionIndex);
            }

            // Changed number of commits to process
            if (m_StateA.NumBuildCommitsChanged)
            {
                m_StateA.NumBuildCommitsChanged = false;

                // Update Undo button
                BlueprintUtility.OnNumBuildCommitsChanged(m_StateA);
            }

            // Changed number of commits to process
            if (m_StateA.NumDestroyActionsChanged)
            {
                m_StateA.NumDestroyActionsChanged = false;

                // Update Undo button
                BlueprintUtility.OnNumDestroyActionsChanged(m_StateA);
            }

            // Exited blueprint mode
            if (m_StateA.ExitedBlueprintMode)
            {
                m_StateA.ExitedBlueprintMode = false;

                BlueprintUtility.OnExitedBlueprintMode(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Undo button (in Build mode)
            if (m_StateA.UndoClickedBuild)
            {
                m_StateA.UndoClickedBuild = false;

                BlueprintUtility.OnUndoClickedBuild(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Undo button (in Build mode)
            if (m_StateA.UndoClickedDestroy)
            {
                m_StateA.UndoClickedDestroy = false;

                BlueprintUtility.OnUndoClickedDestroy(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Destroy Mode button (from Build mode)
            if (m_StateA.DestroyModeClicked)
            {
                m_StateA.DestroyModeClicked = false;

                BlueprintUtility.OnDestroyModeClicked(m_StateA, m_StateB, m_StateC, m_StateD);
            }

            // Build clicked
            if (m_StateA.NewDestroyConfirmed)
            {
                m_StateA.NewDestroyConfirmed = false;

                BlueprintUtility.ConfirmDestroy(m_StateA, m_StateB, m_StateC, m_StateC.CurrRegionIndex);
            }

            // Clicked the Exit button (from Destroy mode)
            if (m_StateA.CanceledDestroyMode)
            {
                m_StateA.CanceledDestroyMode = false;

                BlueprintUtility.OnCanceledDestroyMode(m_StateA, m_StateB, m_StateC, m_StateD);
            }
        }
    }
}
