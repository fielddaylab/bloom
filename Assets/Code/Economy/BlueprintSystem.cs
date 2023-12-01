using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Sim;

namespace Zavala.Economy
{
    // Update after MarketSystem
    [SysUpdate(GameLoopPhase.Update, 400)]
    public class BlueprintSystem : SharedStateSystemBehaviour<BlueprintState, ShopState, SimGridState, BuildToolState>
    {
        private MarketData m_StateE;

        public override void ProcessWork(float deltaTime)
        {
            if (!m_StateE) { m_StateE = Game.SharedState.Get<MarketData>(); }

            // --- Process UI triggers

            // Build clicked
            if (m_StateA.NewBuildConfirmed)
            {
                BlueprintUtility.ConfirmBuild(m_StateA, m_StateB, m_StateC.CurrRegionIndex);
            }

            // Blueprint mode opened
            if (m_StateA.StartBlueprintMode)
            {
                m_StateA.IsActive = true;
                SimTimeUtility.Pause(SimPauseFlags.Blueprints, ZavalaGame.SimTime);
                BlueprintUtility.OnStartBlueprintMode(m_StateA);
            }

            // Exited blueprint mode
            if (m_StateA.ExitedBlueprintMode)
            {
                m_StateA.IsActive = false;
                SimTimeUtility.Resume(SimPauseFlags.Blueprints, ZavalaGame.SimTime);
                BlueprintUtility.OnExitedBlueprintMode(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Undo button (in Build mode)
            if (m_StateA.UndoClickedBuild)
            {
                BlueprintUtility.OnUndoClickedBuild(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Undo button (in Build mode)
            if (m_StateA.UndoClickedDestroy)
            {
                BlueprintUtility.OnUndoClickedDestroy(m_StateA, m_StateB, m_StateC);
            }

            // Clicked the Destroy Mode button (from Build mode)
            if (m_StateA.DestroyModeClicked)
            {
                BlueprintUtility.OnDestroyModeClicked(m_StateA, m_StateB, m_StateC, m_StateD);
            }

            // Destroy clicked
            if (m_StateA.NewDestroyConfirmed)
            {
                BlueprintUtility.ConfirmDestroy(m_StateA, m_StateB, m_StateC, m_StateC.CurrRegionIndex);
            }

            // Clicked the Exit button (from Destroy mode)
            if (m_StateA.CanceledDestroyMode)
            {
                BlueprintUtility.OnCanceledDestroyMode(m_StateA, m_StateB, m_StateC, m_StateD);
            }

            // Changed number of commits to process
            if (m_StateA.NumBuildCommitsChanged)
            {
                // Update Undo button
                BlueprintUtility.OnNumBuildCommitsChanged(m_StateA);
            }

            // Changed number of commits to process
            if (m_StateA.NumDestroyActionsChanged)
            {
                // Update Undo button
                BlueprintUtility.OnNumDestroyActionsChanged(m_StateA);
            }

            // Tool was deselected
            if (m_StateD.ToolUpdated)
            {
                if (m_StateD.ActiveTool == UserBuildTool.None)
                {
                    BlueprintUtility.OnBuildToolDeselected(m_StateA);
                }
                else
                {
                    BlueprintUtility.OnBuildToolSelected(m_StateA);
                }
            }


            // On market ticks, update top bar box popups
            if (m_StateE.MarketTimer.HasAdvanced())
            {
                BlueprintUtility.OnMarketTickAdvanced(m_StateA, m_StateC, m_StateE);
            }
        }
    }
}
