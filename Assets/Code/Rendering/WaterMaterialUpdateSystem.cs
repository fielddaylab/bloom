using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {
    [SysUpdate(GameLoopPhase.LateUpdate, 100000)]
    public class WaterMaterialUpdateSystem : ComponentSystemBehaviour<WaterTile> {
        public override bool HasWork() {
            return base.HasWork() && Game.SharedState.Get<SimPhosphorusState>().UpdatedPhosphorusRegionMask != 0;
        }

        public override void ProcessWork(float deltaTime) {
            SimAlgaeState algaeState = Game.SharedState.Get<SimAlgaeState>();
            SimPhosphorusState phosphorusState = Game.SharedState.Get<SimPhosphorusState>();
            WaterMaterialData materialAssets = Game.SharedState.Get<WaterMaterialData>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            if (!materialAssets.TopMaterial.IsLoaded() || !materialAssets.WaterfallMaterial.IsLoaded()) {
                return;
            }
            if (!materialAssets.TopDeepMaterial.IsLoaded()) {
                return;
            }

            foreach (var tile in m_Components) {
                if (!phosphorusState.Phosphorus.Changes.AffectedTiles.Contains(tile.TileIndex)) {
                    continue;
                }

                bool isDeep = (grid.Terrain.Info[tile.TileIndex].Flags & TerrainFlags.NonBuildable) != 0;
                
                // Green-ness reflects progress towards algae growth threshold
                /* 
                int amount = phosphorusState.Phosphorus.CurrentState()[tile.TileIndex].Count; 
                float ratio = (float) amount / AlgaeSim.MinPForAlgaeGrowth;
                */
                
                // Green-ness reflects algae percentage
                float ratio = algaeState.Algae.State[tile.TileIndex].PercentAlgae;
                Material topMaterial, sideMaterial;
                if (isDeep) {
                    topMaterial = materialAssets.TopDeepMaterial.Find(ratio);
                    sideMaterial = materialAssets.WaterfallMaterial.Find(ratio);
                    if (tile.DepthObject) {
                        tile.DepthObject.WaterRenderer.material = materialAssets.SideDeepMaterial.Find(ratio);
                    }
                }
                else {
                    topMaterial = materialAssets.TopMaterial.Find(ratio);
                    sideMaterial = materialAssets.WaterfallMaterial.Find(ratio);
                    if (tile.DepthObject) {
                        tile.DepthObject.WaterRenderer.material = materialAssets.SideMaterial.Find(ratio);
                    }
                }

                tile.SurfaceRenderer.sharedMaterial = topMaterial;
                tile.EdgeRenderer.Material = sideMaterial;
            }
        }
    }
}