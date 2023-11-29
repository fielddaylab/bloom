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
        private BlueprintState m_StateE;

        public override void ProcessWork(float deltaTime)
        {
            if (m_StateA.ToolUpdated)
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

            if (!m_StateE) {
                m_StateE = Game.SharedState.Get<BlueprintState>(); 
            }

            if (m_StateE.ExitedBlueprintMode)
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