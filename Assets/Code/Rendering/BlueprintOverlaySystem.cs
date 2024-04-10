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
    [SysUpdate(GameLoopPhase.Update, 450)]
    public class BlueprintOverlaySystem : SharedStateSystemBehaviour<BlueprintState, SimGridState, SimWorldState, RoadNetwork>
    {
        private BuildToolState m_StateE;

        public override void Initialize()
        {
            base.Initialize();

            Game.Events.Register(GameEvents.RegionSwitched, OnRegionSwitched);
        }

        public override void ProcessWork(float deltaTime)
        {
            if (!m_StateE)
            {
                m_StateE = Game.SharedState.Get<BuildToolState>();
            }

            SimWorldCamera cam = Find.State<SimWorldCamera>();

            if (m_StateE.ToolUpdated)
            {
                // Regen when non-road tool is selected
                if (m_StateE.ActiveTool != UserBuildTool.None && m_StateE.ActiveTool != UserBuildTool.Destroy)
                {
                    BlueprintUtility.RegenerateOverlayMesh(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE);
                }
                else
                {
                    // Remove the old mesh
                    BlueprintUtility.HideOverlayMesh(m_StateA);
                }
            }

            if (m_StateE.RegionSwitched && m_StateA.OverlayRenderer.enabled)
            {
                // handle commits being modified
                if (m_StateA.Commits.Count == 0)
                {
                    BlueprintUtility.RegenerateOverlayMesh(m_StateA, m_StateB, m_StateC, m_StateD, m_StateE);
                }
            }

            if (m_StateA.NumBuildCommitsChanged)
            {
                CameraInputState camState = Game.SharedState.Get<CameraInputState>();

                if (m_StateA.Commits.Count > 0 && camState.LockRegion == Tile.InvalidIndex16)
                {
                    camState.LockRegion = m_StateB.CurrRegionIndex;

                    SimWorldState world = Game.SharedState.Get<SimWorldState>();
                    Bounds b = world.RegionBounds[camState.LockRegion];
                    b.Expand(0.25f);

                    Vector3 bMin = b.min, bMax = b.max;
                    camState.LockedBounds = Rect.MinMaxRect(bMin.x, bMin.z, bMax.x, bMax.z);
                }
                else if (m_StateA.Commits.Count == 0)
                {
                    camState.LockRegion = Tile.InvalidIndex16;
                }
            }
        }

        private void OnRegionSwitched()
        {
            m_StateE.RegionSwitched = true;
        }
    }
}