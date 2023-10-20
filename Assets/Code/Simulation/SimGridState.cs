using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.World;

namespace Zavala.Sim {
    [SharedStateInitOrder(-1)]
    public sealed class SimGridState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly StringHash32 Event_RegionUpdated = "SimGridState::TerrainUpdated";

        #region Inspector

        public WorldAsset WorldData;

        #endregion // Inspector

        // grid state

        [NonSerialized] public HexGridSize HexSize;
        [NonSerialized] public TerrainBuffers Terrain;
        [NonSerialized] public SimBuffer<RegionInfo> Regions;
        [NonSerialized] public SimBuffer<WaterGroupInfo> WaterGroups;
        [NonSerialized] public uint RegionCount;
        [NonSerialized] public uint WaterGroupCount;

        [NonSerialized] public uint CurrRegionIndex;

        [NonSerialized] public HashSet<uint> UpdatedRegions = new HashSet<uint>();

        // miscellaneous

        [NonSerialized] public System.Random Random;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            HexSize = new HexGridSize(WorldData.Width, WorldData.Height);
            Terrain.Create(HexSize);
            Regions = SimBuffer.Create<RegionInfo>(RegionInfo.MaxRegions);
            WaterGroups = SimBuffer.Create<WaterGroupInfo>(WaterGroupInfo.MaxGroups);
            RegionCount = 0;
            WaterGroupCount = 0;

            CurrRegionIndex = 0;
            Random = new System.Random((int) (Environment.TickCount ^ DateTime.UtcNow.ToFileTimeUtc()));

            GameLoop.QueuePreUpdate(() => SimDataUtility.LateInitializeData(this, WorldData));
        }
    }

    static public class SimDataUtility {
        static public void LateInitializeData(SimGridState grid, WorldAsset world) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();
            LoadRegionDataFromWorld(grid, world, 0);
            RegenTerrainDependentInfo(grid, phosphorus);
            GenerateRandomPhosphorus(grid, phosphorus);

            ZavalaGame.Events.Dispatch(SimGridState.Event_RegionUpdated, 0);
        }

        static public void LoadAndRegenRegionDataFromWorld(SimGridState grid, WorldAsset world, int regionIndex) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();
            LoadRegionDataFromWorld(grid, world, regionIndex);
            RegenTerrainDependentInfo(grid, phosphorus);
        }

        static public void LoadRegionDataFromWorld(SimGridState grid, WorldAsset world, int regionIndex) {
            var offsetRegion = world.Regions[regionIndex];
            LoadRegionData(grid, offsetRegion.Region, offsetRegion.X, offsetRegion.Y, offsetRegion.Elevation, world.Palette(regionIndex));
        }

        static public void LoadRegionData(SimGridState grid, RegionAsset asset, uint offsetX, uint offsetY, uint offsetElevation, RegionPrefabPalette palette) {
            HexGridSubregion totalRegion = new HexGridSubregion(grid.HexSize);
            HexGridSubregion subRegion = totalRegion.Subregion((ushort) offsetX, (ushort) offsetY, (ushort) asset.Width, (ushort) asset.Height);

            Assert.True(offsetX % 2 == 0, "Region offset x must be multiple of 2");

            ushort regionIndex = AddRegion(grid, subRegion, asset.Id);

            for(int subIndex = 0; subIndex < subRegion.Size; subIndex++) {
                int mapIndex = subRegion.FastIndexToGridIndex(subIndex);

                TerrainTileInfo fromRegion = asset.Tiles[subIndex];
                fromRegion.Height += (ushort)(offsetElevation * ImportSettings.HEIGHT_SCALE);
                
                ref TerrainTileInfo currentTile = ref grid.Terrain.Info[mapIndex];

                if (fromRegion.Category == TerrainCategory.Void) {
                    if (currentTile.Category == TerrainCategory.Void) {
                        currentTile.RegionIndex = regionIndex;
                    }
                    continue;
                }

                if (currentTile.Category != TerrainCategory.Void) {
                    Log.Warn("[SimDataUtility] Skipping data import for tile {0} - tile is not empty", mapIndex);
                    continue;
                }

                fromRegion.RegionIndex = regionIndex;
                currentTile = fromRegion;
            }

            SimWorldState world = Game.SharedState.Get<SimWorldState>();

            world.Palettes[regionIndex] = palette;

            // water proxies
            foreach(var waterGroup in asset.WaterGroups) {
                ushort groupIndex = AddWaterGroup(grid, subRegion, regionIndex, asset.WaterGroupLocalIndices, new OffsetLengthU16(waterGroup.Offset, waterGroup.Length));
                WaterGroupInstance group = GameObject.Instantiate(world.WaterProxyPrefab, SimWorldUtility.GetWaterCentroid(world, grid.WaterGroups[groupIndex]), Quaternion.identity);
                group.GroupIndex = groupIndex;
            }

            // spawn buildings
            foreach (var obj in asset.Buildings) {
                int mapIndex = subRegion.FastIndexToGridIndex(obj.LocalTileIndex);
                world.Spawns.QueuedBuildings.PushBack(new SpawnRecord<BuildingType>() {
                    TileIndex = (ushort) mapIndex,
                    RegionIndex = regionIndex,
                    Id = obj.ScriptName,
                    Data = obj.Type
                });
            }

            // TODO: spawn roads?

            // spawn modifiers
            foreach(var obj in asset.Modifiers) {
                int mapIndex = subRegion.FastIndexToGridIndex(obj.LocalTileIndex);
                world.Spawns.QueuedModifiers.PushBack(new SpawnRecord<RegionAsset.TerrainModifier>() {
                    TileIndex = (ushort)mapIndex,
                    RegionIndex = regionIndex,
                    Id = obj.ScriptName,
                    Data = obj.Modifier
                });
            }

            // load script
            if (asset.LeafScript != null) {
                ScriptDatabaseUtility.LoadNow(ScriptUtility.Database, asset.LeafScript);
            }

            RegenRegionInfo(grid, regionIndex, subRegion);
        }

        /// <summary>
        /// Adds a new region.
        /// </summary>
        static public ushort AddRegion(SimGridState grid, HexGridSubregion region, RegionId id) {
            Assert.True(grid.RegionCount < RegionInfo.MaxRegions, "Maximum '{0}' regions supported - should we increase this?", RegionInfo.MaxRegions);
            int regionIndex = (int) grid.RegionCount;
            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.GridArea = region;
            regionInfo.MaxHeight = 0;
            regionInfo.Id = id;
            grid.RegionCount++;
            foreach(var index in region) {
                grid.Terrain.Regions[index] = (ushort)regionIndex;
            }
            return (ushort) regionIndex;
        }

        /// <summary>
        /// Returns the HexGridSubregion for the given region asset.
        /// </summary>
        static public HexGridSubregion GetSubregion(SimGridState grid, RegionAsset asset, int offsetX, int offsetY) {
            HexGridSubregion source = new HexGridSubregion(grid.HexSize);
            HexGridSubregion region = source.Subregion((ushort) offsetX, (ushort) offsetY, (ushort) asset.Width, (ushort) asset.Height);
            return region;
        }

        /// <summary>
        /// Recalculates terrain-dependent region info.
        /// </summary>
        static public void RegenRegionInfo(SimGridState grid, int regionIndex, HexGridSubregion subregion) {
            Assert.True(regionIndex < grid.RegionCount, "Region {0} is not a part of grid - currently {1} regions", regionIndex, grid.RegionCount);
            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.MaxHeight = TerrainInfo.GetMaximumHeight(grid.Terrain.Info, grid.HexSize);
            regionInfo.GridArea = subregion;
        }

        /// <summary>
        /// Regenerates dependent buffers from terrain information.
        /// </summary>
        static public void RegenTerrainDependentInfo(SimGridState grid, SimPhosphorusState phosphorus) {
            TerrainInfo.ExtractHeightAndRegionData(grid.Terrain.Info, grid.Terrain.Height, grid.Terrain.Regions);
            PhosphorusSim.ExtractPhosphorusTileInfoFromTerrain(grid.Terrain.Info, phosphorus.Phosphorus.Info);
            PhosphorusSim.EvaluateFlowField(phosphorus.Phosphorus.Info, grid.HexSize);
        }

        /// <summary>
        /// Executes an action for each tile in the given region.
        /// </summary>
        static public void ForEachTileInRegion(SimGridState grid, int regionIndex, RegionTileHandlerDelegate tileIndexAction) {
            foreach(var tileIndex in grid.Regions[regionIndex].GridArea) {
                if (grid.Terrain.Regions[tileIndex] == regionIndex) {
                    tileIndexAction((ushort) regionIndex, tileIndex);
                }
            }
        }

        static public bool TryUpdateCurrentRegion(SimGridState grid, SimWorldState world, Ray viewportCenter) {
            if (Physics.Raycast(viewportCenter, out RaycastHit hit, 100f, LayerMask.GetMask("HexTile"))
                && SimWorldUtility.TryGetTileIndexFromWorld(hit.transform.position, out int index) 
                && index >= 0) {
                ushort newRegionIndex = grid.Terrain.Info[index].RegionIndex;
                if (newRegionIndex == grid.CurrRegionIndex) {
                    // region unchanged.
                    return false;
                }
                Debug.Log("[SimGridState] Region updated from " + grid.CurrRegionIndex + " to " + newRegionIndex);
                grid.CurrRegionIndex = newRegionIndex;
                ShopUtility.RefreshShop(Game.SharedState.Get<BudgetData>(), Game.SharedState.Get<ShopState>(), grid);
                return true;
            } 
            return false;
        }

        #region Water Groups

        static public ushort AddWaterGroup(SimGridState grid, HexGridSubregion region, ushort regionIndex, ushort[] tileIndices, OffsetLengthU16 range) {
            Assert.True(grid.WaterGroupCount < WaterGroupInfo.MaxGroups, "Maximum {0} water groups supported - should we increase this?", WaterGroupInfo.MaxGroups);
            int index = (int) grid.WaterGroupCount;
            ref WaterGroupInfo info = ref grid.WaterGroups[index];
            info.RegionId = regionIndex;
            info.TileCount = range.Length;
            unsafe {
                for (int i = 0; i < range.Length; i++) {
                    info.TileIndices[i] = (ushort) region.FastIndexToGridIndex(tileIndices[range.Offset + i]);
                }
            }
            grid.WaterGroupCount++;
            return (ushort) index;
        }

        #endregion // Water Groups

        #region Generation

        /// <summary>
        /// Generates a random amount of phosphorus on the grid.
        /// </summary>
        static public void GenerateRandomPhosphorus(SimGridState grid, SimPhosphorusState phosphorus) {
            for (int i = 0; i < grid.HexSize.Size; i++) {
                if (grid.Terrain.Info[i].Category != TerrainCategory.Void && grid.Random.Chance(0.2f)) {
                    SimPhospohorusUtility.AddPhosphorus(phosphorus, i, grid.Random.Next(2, 8));
                }
            }
        }

        #endregion // Generation

    }
}