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
using Zavala.Roads;
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
        [NonSerialized] public uint RegionCount;
        [NonSerialized] public uint CurrRegionIndex;

        [NonSerialized] public HashSet<uint> UpdatedRegions = new HashSet<uint>();

        // miscellaneous

        [NonSerialized] public System.Random Random;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            HexSize = new HexGridSize(WorldData.Width, WorldData.Height);
            Terrain.Create(HexSize);
            Regions = SimBuffer.Create<RegionInfo>(HexSize);
            RegionCount = 0;
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
            LoadRegionData(grid, offsetRegion.Region, offsetRegion.X, offsetRegion.Y, offsetRegion.Elevation);
        }

        static public void LoadRegionData(SimGridState grid, RegionAsset asset, uint offsetX, uint offsetY, uint offsetElevation) {
            HexGridSubregion totalRegion = new HexGridSubregion(grid.HexSize);
            HexGridSubregion subRegion = totalRegion.Subregion((ushort) offsetX, (ushort) offsetY, (ushort) asset.Width, (ushort) asset.Height);

            Assert.True(offsetX % 2 == 0, "Region offset x must be multiple of 2");

            ushort regionIndex = AddRegion(grid, subRegion, (ushort) asset.PaletteIndex, asset.Id);

            for(int subIndex = 0; subIndex < subRegion.Size; subIndex++) {
                int mapIndex = subRegion.FastIndexToGridIndex(subIndex);

                TerrainTileInfo fromRegion = asset.Tiles[subIndex];
                ref TerrainTileInfo currentTile = ref grid.Terrain.Info[mapIndex];
                fromRegion.Height += (ushort)(offsetElevation * ImportSettings.HEIGHT_SCALE);

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
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();

            // spawn buildings
            foreach(var obj in asset.Buildings) {
                int mapIndex = subRegion.FastIndexToGridIndex(obj.LocalTileIndex);
                HexVector pos = grid.HexSize.FastIndexToPos(mapIndex);
                Vector3 worldPos = HexVector.ToWorld(pos, grid.Terrain.Info[mapIndex].Height, world.WorldSpace);
                switch (obj.Type) {
                    case RegionAsset.BuildingType.City: {
                        GameObject.Instantiate(pools.City, worldPos, Quaternion.identity);
                        break;
                    }
                    case RegionAsset.BuildingType.DairyFarm: {
                        GameObject.Instantiate(pools.DairyFarm, worldPos, Quaternion.identity);
                        break;
                    }
                    case RegionAsset.BuildingType.GrainFarm: {
                        GameObject.Instantiate(pools.GrainFarm, worldPos, Quaternion.identity);
                        break;
                    }
                }
            }

            // TODO: spawn roads?

            // load script
            if (asset.LeafScript != null) {
                ScriptDatabaseUtility.LoadNow(ScriptUtility.Database, asset.LeafScript);
            }

            RegenRegionInfo(grid, regionIndex, subRegion);
            RegenBorderInfo(grid, regionIndex, world);
        }

        /// <summary>
        /// Generates a random amount of phosphorus on the grid.
        /// </summary>
        static public void GenerateRandomPhosphorus(SimGridState grid, SimPhosphorusState phosphorus) {
            for (int i = 0; i < grid.HexSize.Size; i++) {
                if (grid.Terrain.Info[i].Category != TerrainCategory.Void && grid.Random.Chance(0.2f)) {
                    SimPhospohorusUtility.AddPhosphorus(phosphorus, i, grid.Random.Next(5, 25));
                }
            }
        }

        /// <summary>
        /// Generates a predefined road on the grid. Used for initial testing purposes.
        /// </summary>
        static public void GenerateBasicRoad(SimGridState grid, RoadNetwork network) {
            TileDirection[] allDirs = new TileDirection[] {
                TileDirection.N,
                TileDirection.S,
                TileDirection.NE,
                TileDirection.NW,
                TileDirection.SE,
                TileDirection.SW
            };
            RoadUtility.AddRoadImmediate(network, grid, 26, true, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 36, false, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 46, false, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 56, false, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 66, false, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 76, false, allDirs);
            RoadUtility.AddRoadImmediate(network, grid, 77, true, allDirs);
        }

        /// <summary>
        /// Adds a new region.
        /// </summary>
        static public ushort AddRegion(SimGridState grid, HexGridSubregion region, ushort palette, RegionId id) {
            Assert.True(grid.RegionCount < RegionInfo.MaxRegions, "Maximum '{0}' regions supported - should we increase this?", RegionInfo.MaxRegions);
            int regionIndex = (int) grid.RegionCount;
            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.GridArea = region;
            regionInfo.PaletteType = palette;
            regionInfo.MaxHeight = 0;
            regionInfo.IsUnlocked = true;
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
        /// Copies data from the given asset to the grid state.
        /// </summary>
        static public void CopyRegionInfo(SimGridState grid, RegionAsset asset, HexGridSubregion gridSubregion) {
            SimBuffer<TerrainTileInfo> tiles = grid.Terrain.Info;
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
        /// Recalculates terrain-dependent border info within region
        /// </summary>
        static public void RegenBorderInfo(SimGridState grid, int regionIndex, SimWorldState worldState) {
            Assert.True(regionIndex < grid.RegionCount, "Region {0} is not a part of grid - currently {1} regions", regionIndex, grid.RegionCount);
            SimBuffer<TerrainTileInfo> infoBuffer = grid.Terrain.Info;

            foreach (var index in grid.HexSize) {
                EvaluateBorders_Step(index, infoBuffer, grid.HexSize, worldState);
            }
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

        // internal evaluation
        static private unsafe void EvaluateBorders_Step(int tileIndex, SimBuffer<TerrainTileInfo> infoBuffer, in HexGridSize gridSize, SimWorldState worldState) {
            bool isBorder = false;

            ref TerrainTileInfo center = ref infoBuffer[tileIndex];
            HexVector pos = gridSize.FastIndexToPos(tileIndex);
            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                HexVector adjPos = HexVector.Offset(pos, dir);

                // check if bordering void
                if (!gridSize.IsValidPos(adjPos)) {
                    isBorder = true;
                    break;
                }
                int adjIndex = gridSize.FastPosToIndex(adjPos);
                if ((infoBuffer[adjIndex].Category == TerrainCategory.Void)) {
                    isBorder = true;
                    break;
                }

                // check if bordering adj region
                if (center.RegionIndex != infoBuffer[adjIndex].RegionIndex) {
                    isBorder = true;
                    break;
                }
            }

            if (isBorder) {
                center.Flags |= TerrainFlags.IsBorder;
            }
            else if ((center.Flags & TerrainFlags.IsBorder) != 0) {
                // was border, now is not. Somehow?
                center.Flags -= TerrainFlags.IsBorder;
            }
        }
    }
}