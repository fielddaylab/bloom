using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Rendering
{
    [SysUpdate(GameLoopPhase.Update, 450)]
    public class RoadAnchorRenderSystem : SharedStateSystemBehaviour<BuildToolState, RoadNetwork, BuildingPools, RoadAnchorRenderState>
    {
        public override void ProcessWork(float deltaTime)
        {
            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();

            if (bpState.IsActive && (m_StateA.ToolUpdated || m_StateA.RegionSwitched))
            {
                // On select road tool
                if (m_StateA.ActiveTool == UserBuildTool.Road)
                {
                    if (m_StateD.AnchorRenderers.Count != 0)
                    {
                        ClearVisualizations();
                    }

                    SimGridState grid = Game.SharedState.Get<SimGridState>();

                    List<int> vizTiles = new List<int>();

                    // for each endpoint in the network, create a new anchor visualization
                    foreach (var dest in m_StateB.Destinations)
                    {
                        if (dest.isExternal || dest.RegionIdx != grid.CurrRegionIndex)
                        {
                            continue;
                        }

                        vizTiles.Add(dest.TileIdx);
                    }

                    foreach (var src in m_StateB.Sources)
                    {
                        if (vizTiles.Contains(src.TileIdx) || src.IsExternal || src.RegionIdx != grid.CurrRegionIndex)
                        {
                            continue;
                        }
                        vizTiles.Add(src.TileIdx);
                    }

                    foreach (int tileIndex in vizTiles)
                    {
                        HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
                        Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
                        var newRender = m_StateC.VizAnchors.Alloc(worldPos);
                        m_StateD.AnchorRenderers.Add(newRender);
                    }
                }
                else
                {
                    // Remove the anchor visualizations
                    ClearVisualizations();
                }
            }

            if (bpState.ExitedBlueprintMode)
            {
                ClearVisualizations();
            }
        }

        private void ClearVisualizations()
        {
            foreach (var renderer in m_StateD.AnchorRenderers)
            {
                m_StateC.VizAnchors.Free(renderer);
            }
            m_StateD.AnchorRenderers.Clear();
        }
    }
}