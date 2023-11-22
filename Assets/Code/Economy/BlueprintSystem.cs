using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Economy
{
    public class BlueprintSystem : SharedStateSystemBehaviour<BlueprintState, ShopState, SimGridState>
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
            if (m_StateA.NumCommitsChanged)
            {
                m_StateA.NumCommitsChanged = false;

                // Update Undo button
                BlueprintUtility.OnNumCommitsChanged(m_StateA);
            }

            // Exited blueprint mode
            if (m_StateA.ExitedBlueprintMode)
            {
                m_StateA.ExitedBlueprintMode = false;

                BlueprintUtility.OnExitedBlueprintMode(m_StateA);
            }

            // Clicked the Undo button (in Build mode)
            if (m_StateA.UndoClickedBuild)
            {
                m_StateA.UndoClickedBuild = false;

                BlueprintUtility.OnUndoClicked(m_StateA);
            }
        }
    }
}
