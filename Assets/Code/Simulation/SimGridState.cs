using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Sim {
    [SharedStateInitOrder(-1)]
    public sealed class SimGridState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly StringHash32 Event_RegionUpdated = "SimGridState::TerrainUpdated";

        #region Inspector

        public uint Width = 10;
        public uint Height = 10;

        #endregion // Inspector

        // grid state

        [NonSerialized] public HexGridSize HexSize;
        [NonSerialized] public TerrainBuffers Terrain;
        [NonSerialized] public SimBuffer<RegionInfo> Regions;
        [NonSerialized] public uint RegionCount;

        [NonSerialized] public HashSet<uint> UpdatedRegions = new HashSet<uint>();
        
        // miscellaneous

        [NonSerialized] public System.Random Random;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            HexSize = new HexGridSize(Width, Height);
            Terrain.Create(HexSize);
            Regions = SimBuffer.Create<RegionInfo>(HexSize);
            RegionCount = 0;
            Random = new System.Random((int) (Environment.TickCount ^ DateTime.UtcNow.ToFileTimeUtc()));

            GameLoop.QueuePreUpdate(() => SimDataUtility.LateInitializeData(this));
        }
    }

    static public class SimDataUtility {
        static public void LateInitializeData(SimGridState grid) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();
            GenerateRandomTerrain(grid, phosphorus);
            RegenTerrainDependentInfo(grid, phosphorus);
            GenerateRandomPhosphorus(grid, phosphorus);

            // TEMP TESTING ----
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            GenerateBasicRoad(grid, network);
            // -----------------

            ZavalaGame.Events.Dispatch(SimGridState.Event_RegionUpdated, 0);
        }

        static public void GenerateRandomTerrain(SimGridState grid, SimPhosphorusState phosphorus) {
            float generationOffset = grid.Random.NextFloat(500);

            HexGridSubregion globalRegion = new HexGridSubregion(grid.HexSize);
            ushort regionIndex = AddRegion(grid, globalRegion, 0);

            for(int i = 0; i < grid.HexSize.Size; i++) {
                TerrainTileInfo tileInfo = default;

                HexVector pos = grid.HexSize.FastIndexToPos(i);
                ushort height = (ushort) ((int) ((10 + 1000 * Mathf.PerlinNoise(generationOffset + pos.X * 0.23f, generationOffset * 0.6f + pos.Y * 0.19f) + grid.Random.Next(15, 100)) / 50) * 50);
                if (height < 500) {
                    tileInfo.Category = TerrainCategory.Water;
                    tileInfo.Flags |= TerrainFlags.IsWater;
                    height = 200;
                }
                tileInfo.Height = height;
                tileInfo.RegionIndex = regionIndex;
                grid.Terrain.Info[i] = tileInfo;
            }

            RegenRegionInfo(grid, regionIndex);
        }

        /// <summary>
        /// Generates a random amount of phosphorus on the grid.
        /// </summary>
        static public void GenerateRandomPhosphorus(SimGridState grid, SimPhosphorusState phosphorus) {
            for (int i = 0; i < grid.HexSize.Size; i++) {
                if (grid.Random.Chance(0.2f)) {
                    SimPhospohorusUtility.AddPhosphorus(phosphorus, i, grid.Random.Next(5, 25));
                }
            }
        }

        /// <summary>
        /// Generates a predefined road on the grid. Used for initial testing purposes.
        /// </summary>
        static public void GenerateBasicRoad(SimGridState grid, RoadNetwork network) {
            RoadUtility.AddRoad(network, grid, 26);
            RoadUtility.AddRoad(network, grid, 36);
            RoadUtility.AddRoad(network, grid, 46);
            RoadUtility.AddRoad(network, grid, 56);
            RoadUtility.AddRoad(network, grid, 66);
            RoadUtility.AddRoad(network, grid, 76);
            RoadUtility.AddRoad(network, grid, 77);
        }

        /// <summary>
        /// Adds a new region.
        /// </summary>
        static public ushort AddRegion(SimGridState grid, HexGridSubregion region, ushort palette) {
            Assert.True(grid.RegionCount < RegionInfo.MaxRegions, "Maximum '{0}' regions supported - should we increase this?", RegionInfo.MaxRegions);
            int regionIndex = (int) grid.RegionCount;
            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.GridArea = region;
            regionInfo.PaletteType = palette;
            regionInfo.MaxHeight = 0;
            grid.RegionCount++;
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
        static public void RegenRegionInfo(SimGridState grid, int regionIndex) {
            Assert.True(regionIndex < grid.RegionCount, "Region {0} is not a part of grid - currently {1} regions", regionIndex, grid.RegionCount);
            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.MaxHeight = TerrainInfo.GetMaximumHeight(grid.Terrain.Info, grid.HexSize);
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
    }
}