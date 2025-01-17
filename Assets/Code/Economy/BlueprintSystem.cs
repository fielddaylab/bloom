using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;

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

            SimWorldCamera cam = Find.State<SimWorldCamera>();

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
                cam.CameraLayers.ShowLayers(LayerMasks.Blueprints_Mask);
            }

            // Exited blueprint mode
            if (m_StateA.ExitedBlueprintMode)
            {
                m_StateA.IsActive = false;
                SimTimeUtility.Resume(SimPauseFlags.Blueprints, ZavalaGame.SimTime);
                BlueprintUtility.OnExitedBlueprintMode(m_StateA, m_StateB, m_StateC);
                cam.CameraLayers.HideLayers(LayerMasks.Blueprints_Mask);
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
                BlueprintUtility.ConfirmDestroy(m_StateA, m_StateB, m_StateC, m_StateD, m_StateC.CurrRegionIndex);
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

                if (m_StateA.Commits.Count == 0) {
                    CameraInputState camState = Game.SharedState.Get<CameraInputState>();
                    camState.LockRegion = Tile.InvalidIndex16;
                }
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

                    if (m_StateA.Commits.Count == 0) {
                        CameraInputState camState = Game.SharedState.Get<CameraInputState>();
                        camState.LockRegion = Tile.InvalidIndex16;
                    }
                }
                else
                {
                    BlueprintUtility.OnBuildToolSelected(m_StateA);

                    // handle commits being modified
                    /*if (m_StateA.Commits.Count == 0) {
                        CameraInputState camState = Game.SharedState.Get<CameraInputState>();
                        camState.LockRegion = m_StateC.CurrRegionIndex;

                        SimWorldState world = Game.SharedState.Get<SimWorldState>();
                        Bounds b = world.RegionBounds[camState.LockRegion];
                        b.Expand(0.25f);

                        Vector3 bMin = b.min, bMax = b.max;
                        camState.LockedBounds = Rect.MinMaxRect(bMin.x, bMin.z, bMax.x, bMax.z);
                    }*/
                }
            }

            // On market ticks, update top bar box popups
            // TODO: 
            if (m_StateE.MarketTimer.HasAdvanced())
            {
                BlueprintUtility.OnMarketTickAdvanced(m_StateA, m_StateC, m_StateE);
            }
        }
    }
}
