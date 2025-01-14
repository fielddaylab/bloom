using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {

    [SysUpdate(GameLoopPhase.LateUpdate, -1)]
    public sealed class SimWorldTileSpawnSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState> {
        #region Inspector

        #endregion // Inspector

        #region Work

        public override bool HasWork() {
            if (base.HasWork()) {
                return m_StateA.NewRegions != 0;
            }
            return false;
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.NewRegions > 0) {
                int regionStart = (int) m_StateA.RegionCount - m_StateA.NewRegions;
                for(int i = 0; i < m_StateA.NewRegions; i++) {
                    int regionIdx = regionStart + i;
                    HexGridSubregion subRegion = m_StateB.Regions[regionIdx].GridArea;
                    RegionPrefabPalette palette = m_StateA.Palettes[regionIdx];
                    var terrainData = m_StateB.Terrain.Info;
                    foreach(var index in subRegion) {
                        if (terrainData[index].RegionIndex != regionIdx || terrainData[index].Category == TerrainCategory.Void) {
                            continue;
                        }
                        InstantiateTile(m_StateA, m_StateB, index, m_StateB.HexSize.FastIndexToPos(index), palette, terrainData[index]);
                    }
                }
            } else {
                Log.Error("[SimWorldTileSpawnSystem] Number of regions decreased somehow?");
            }
        }

        static private void InstantiateTile(SimWorldState world, SimGridState grid, int index, in HexVector position, RegionPrefabPalette palette, in TerrainTileInfo tileInfo) {
            Vector3 pos = HexVector.ToWorld(position, tileInfo.Height, world.WorldSpace);
            TileInstance inst;
            if ((tileInfo.Flags & TerrainFlags.IsWater) != 0) {
                inst = Instantiate(world.DefaultWaterPrefab, pos, Quaternion.identity);
            } else if (palette.InnerGroundTile && (tileInfo.Flags & TerrainFlags.CullBase) != 0) {
                inst = Instantiate(palette.InnerGroundTile, pos, Quaternion.identity);
            } else {
                inst = Instantiate(palette.GroundTile, pos, Quaternion.identity);
                if ((tileInfo.Flags & TerrainFlags.CullBase) != 0) {
                    inst.PillarRenderer.enabled = false;
                }
            }
            //inst.index = index;
#if UNITY_EDITOR
            inst.gameObject.name = "Tile " + index.ToStringLookup();
#endif // UNITY_EDITOR

            world.Tiles[index] = inst;
            if ((tileInfo.Flags & TerrainFlags.TopHidden) != 0) {
                TileEffectRendering.SetTopVisibility(inst, false);
            }

            if ((tileInfo.Flags & TerrainFlags.IsWater) != 0) {
                WaterTile wTile = inst.GetComponent<WaterTile>();
                wTile.TileIndex = index;

                WaterMaterialData materialAssets = Game.SharedState.Get<WaterMaterialData>();
                
                // If deep water, modify default material
                if ((tileInfo.Flags & TerrainFlags.NonBuildable) != 0) {
                    wTile.SurfaceRenderer.sharedMaterial = materialAssets.TopDeepMaterial.Find(0);
                    WaterTileUtility.TrySpawnDepthObject(wTile, grid, world.DefaultDepthObject, materialAssets.SideDeepMaterial.Find(0));
                } else {
                    wTile.SurfaceRenderer.sharedMaterial = materialAssets.TopMaterial.Find(0);
                    WaterTileUtility.TrySpawnDepthObject(wTile, grid, world.DefaultDepthObject, materialAssets.SideMaterial.Find(0));
                }

                WaterTileUtility.UpdateWaterfallEdges(wTile, grid, materialAssets);
                
            }
        }

        #endregion // Work
    }
}