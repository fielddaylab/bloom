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

            if (!materialAssets.TopMaterial.IsLoaded() || !materialAssets.WaterfallMaterial.IsLoaded()) {
                return;
            }

            foreach(var tile in m_Components) {
                if (!phosphorusState.Phosphorus.Changes.AffectedTiles.Contains(tile.TileIndex)) {
                    continue;
                }

                int amount = phosphorusState.Phosphorus.CurrentState()[tile.TileIndex].Count;
                float ratio = (float) amount / AlgaeSim.MinPForAlgaeGrowth;
                Material topMaterial = materialAssets.TopMaterial.Find(ratio);

                tile.SurfaceRenderer.sharedMaterial = topMaterial;
            }
        }
    }
}