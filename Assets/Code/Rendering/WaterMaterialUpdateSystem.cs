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

                int amount = phosphorusState.Phosphorus.CurrentState()[tile.TileIndex].Count;
                float ratio = (float) amount / AlgaeSim.MinPForAlgaeGrowth;
                Material topMaterial;
                if (isDeep) {
                    topMaterial = materialAssets.TopDeepMaterial.Find(ratio);
                }
                else {
                    topMaterial = materialAssets.TopMaterial.Find(ratio);
                }

                tile.SurfaceRenderer.sharedMaterial = topMaterial;
            }
        }
    }
}