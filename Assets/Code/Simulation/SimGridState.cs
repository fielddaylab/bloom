using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Rendering;
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
        [NonSerialized] public SimArena<ushort> RegionEdgeArena;

        [NonSerialized] public ushort CurrRegionIndex;

        [NonSerialized] public HashSet<ushort> UpdatedRegions = new HashSet<ushort>();

        [NonSerialized] public int GlobalMaxHeight; // the highest tile height across all regions

        // miscellaneous

        [NonSerialized] public System.Random Random;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            HexSize = new HexGridSize(WorldData.Width, WorldData.Height);
            Terrain.Create(HexSize);
            Regions = SimBuffer.Create<RegionInfo>(RegionInfo.MaxRegions);
            WaterGroups = SimBuffer.Create<WaterGroupInfo>(WaterGroupInfo.MaxGroups);
            RegionEdgeArena = SimArena.Create<ushort>(RegionInfo.MaxRegions * 64);
            RegionCount = 0;
            WaterGroupCount = 0;

            CurrRegionIndex = 0;
            Random = new System.Random((int) (Environment.TickCount ^ DateTime.UtcNow.ToFileTimeUtc()));

            GlobalMaxHeight = 0;

            GameLoop.QueuePreUpdate(() => SimDataUtility.LateInitializeData(this, WorldData));
        }
    }

    static public class SimDataUtility {
        static public void LateInitializeData(SimGridState grid, WorldAsset world) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();
            SimWorldState worldState = Game.SharedState.Get<SimWorldState>();
            LoadRegionDataFromWorld(grid, world, 0, worldState);
            RegenTerrainDependentInfo(grid, phosphorus);
            // GenerateRandomPhosphorus(grid, phosphorus);

            ZavalaGame.Events.Dispatch(SimGridState.Event_RegionUpdated, 0);
        }

        static public void LoadAndRegenRegionDataFromWorld(SimGridState grid, WorldAsset world, int regionIndex, SimWorldState worldState) {
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();
            LoadRegionDataFromWorld(grid, world, regionIndex, worldState);
            RegenTerrainDependentInfo(grid, phosphorus);
        }

        static public void LoadRegionDataFromWorld(SimGridState grid, WorldAsset world, int regionIndex, SimWorldState worldState) {
            var offsetRegion = world.Regions[regionIndex];
            LoadRegionData(grid, offsetRegion.Region, offsetRegion.X, offsetRegion.Y, offsetRegion.Elevation, world.Palette(regionIndex), worldState);
        }

        static public void LoadRegionData(SimGridState grid, RegionAsset asset, uint offsetX, uint offsetY, uint offsetElevation, RegionPrefabPalette palette, SimWorldState worldState) {
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

            ref RegionInfo regionInfo = ref grid.Regions[regionIndex];
            regionInfo.Edges = grid.RegionEdgeArena.Alloc((uint) asset.Borders.Length);
            for(int i = 0; i < regionInfo.Edges.Length; i++) {
                regionInfo.Edges[i] = (ushort) subRegion.FastIndexToGridIndex(asset.Borders[i].LocalTileIndex);
            }
            regionInfo.WaterEdges = grid.RegionEdgeArena.Alloc((uint) asset.EdgeVisualUpdateSet.Length);
            for(int i = 0; i < regionInfo.WaterEdges.Length; i++) {
                regionInfo.WaterEdges[i] = (ushort) subRegion.FastIndexToGridIndex(asset.EdgeVisualUpdateSet[i]);
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
                world.Spawns.QueuedBuildings.PushBack(new SpawnRecord<BuildingSpawnData>() {
                    TileIndex = (ushort) mapIndex,
                    RegionIndex = regionIndex,
                    Id = obj.ScriptName,
                    Data = new BuildingSpawnData() {
                        Type = obj.Type,
                        CharacterId = obj.CharacterId,
                        TitleId = obj.LocationName,
                    }
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

            // spawn toll booths
            foreach (var obj in asset.Spanners) {
                int mapIndex = subRegion.FastIndexToGridIndex(obj.LocalTileIndex);

                // check if both regions are unlocked (another toll booth is in surrounding tiles)
                HexVector currPos = grid.HexSize.FastIndexToPos(mapIndex);
                for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                    HexVector adjPos = HexVector.Offset(currPos, dir);
                    if (!grid.HexSize.IsValidPos(adjPos)) {
                        continue;
                    }
                    int adjIdx = grid.HexSize.FastPosToIndex(adjPos);

                    ref TerrainTileInfo adjTileInfo = ref grid.Terrain.Info[adjIdx];

                    if ((regionIndex != adjTileInfo.RegionIndex) && (adjTileInfo.Flags & TerrainFlags.IsToll) != 0) {
                        world.QueuedSpanners.PushBack(new SimWorldState.SpanSpawnRecord<BuildingType>() {
                            TileIndexA = (ushort)mapIndex,
                            TileIndexB = (ushort)adjIdx,
                            // RegionIndexA = regionIndex,
                            // RegionIndexB = adjTileInfo.RegionIndex,
                            Id = obj.ScriptName,
                            Data = obj.Type
                        });

                        break;
                    }
                }
            }

            for(int i = 0; i < regionIndex - 1; i++) {
                var visualEdges = grid.Regions[i].WaterEdges;
                for(int j = 0; j < visualEdges.Length; j++) {
                    world.QueuedVisualUpdates.PushBack(new VisualUpdateRecord() {
                        TileIndex = visualEdges[j],
                        Type = VisualUpdateType.Water
                    });
                }
            }

            // load script
            if (asset.LeafScript != null) {
                ScriptDatabaseUtility.LoadNow(ScriptUtility.Database, asset.LeafScript);
            }

            RegenRegionInfo(grid, regionIndex, subRegion);
            worldState.MaxHeight = HexVector.ToWorld(new HexVector(), grid.GlobalMaxHeight, worldState.WorldSpace).y;
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
            regionInfo.Age = 0;
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
            regionInfo.MaxHeight = TerrainInfo.GetMaximumHeight(grid.Terrain.Info, subregion, (ushort) regionIndex);
            if (regionInfo.MaxHeight > grid.GlobalMaxHeight) { grid.GlobalMaxHeight = regionInfo.MaxHeight; }
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

        static public bool TryUpdateCurrentRegion(SimGridState grid, SimWorldState world, Transform lookRoot) {
            if (SimWorldUtility.TryGetTileIndexFromWorld(grid, world, lookRoot.position, out int index)  
                && index >= 0) {
                ushort newRegionIndex = grid.Terrain.Regions[index];
                if (newRegionIndex >= grid.RegionCount || newRegionIndex == grid.CurrRegionIndex) {
                    // region unchanged.
                    return false;
                }
                Debug.Log("[SimGridState] Region updated from " + grid.CurrRegionIndex + " to " + newRegionIndex);
                grid.CurrRegionIndex = newRegionIndex;
                ShopUtility.RefreshShop(Game.SharedState.Get<BudgetData>(), Game.SharedState.Get<ShopState>(), grid);
                Game.Events.Dispatch(GameEvents.RegionSwitched);
                return true;
            } 
            return false;
        }

        /// <summary>
        /// Destroys a building with the hit collider
        /// </summary>
        /// <param name="hit">Collider hit by a raycast</param>
        public static void DestroyBuildingFromHit(SimGridState grid, BlueprintState bpState, GameObject hitObj, OccupiesTile ot)
        {
            SimWorldUtility.TryGetTileIndexFromWorld(hitObj.transform.position, out int tileIndex);

            // check if obj is staging/pending or already built
            int costToRemove = 0;

            if (ot.Pending)
            {
                // negative price of building
                costToRemove = -ShopUtility.PriceLookup(ot.Type);
            }
            else
            {
                // removal cost
                costToRemove = 0;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            RoadFlags rFlagSnapshot = network.Roads.Info[tileIndex].Flags;
            TerrainFlags tFlagSnapshot = grid.Terrain.Info[tileIndex].Flags;
            TileAdjacencyMask flowSnapshot = network.Roads.Info[tileIndex].FlowMask;

            DestroyBuilding(grid, network, hitObj, tileIndex, ot.Type, true, out List<TileDirection> inleadingDirsRemoved);

            // Commit the destroy action
            BlueprintUtility.CommitDestroyAction(bpState, new ActionCommit(
                ot.Type,
                ActionType.Destroy,
                costToRemove,
                tileIndex,
                inleadingDirsRemoved,
                hitObj.gameObject,
                rFlagSnapshot,
                tFlagSnapshot,
                flowSnapshot,
                ot.Pending
                ));

            // Add cost to receipt queue
            ShopState shop = Game.SharedState.Get<ShopState>();
            ShopUtility.EnqueueCost(shop, costToRemove);
        }


        /// <summary>
        /// Destroys a building directly
        /// </summary>
        /// <param name="hit">Collider hit by a raycast</param>
        public static void DestroyBuildingFromUndo(SimGridState grid, RoadNetwork network, GameObject hitObj, int tileIndex, BuildingType buildingType)
        {
            DestroyBuilding(grid, network, hitObj, tileIndex, buildingType, false, out List<TileDirection> inleadingDirsRemoved);
        }

        /// <summary>
        /// Destroys a building at the given tile, with optional functionality if there is an object attached with the building.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="hitObj">May be null</param>
        /// <param name="tileIndex"></param>
        public static void DestroyBuilding(SimGridState grid, RoadNetwork network, GameObject buildingObj, int tileIndex, BuildingType buildingType, bool removeInleadingRoads, out List<TileDirection> inleadingDirsRemoved)
        {
            inleadingDirsRemoved = new List<TileDirection>();

            network.Roads.Info[tileIndex].Flags &= ~RoadFlags.IsAnchor;
            grid.Terrain.Info[tileIndex].Flags &= ~TerrainFlags.IsOccupied;

            if (buildingObj)
            {
                if (buildingObj.TryGetComponent(out SnapToTile snap) && snap.m_hideTop)
                {
                    TileEffectRendering.SetTopVisibility(ZavalaGame.SimWorld.Tiles[tileIndex], true);
                }
            }
           
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();
            Log.Msg("[UserBuildingSystem] Attempting delete, found type {0}", buildingType.ToString());
            switch (buildingType)
            {
                case BuildingType.Road:
                    // Clear from adj roads
                    RoadUtility.RemoveRoad(network, grid, pools, tileIndex, removeInleadingRoads, out inleadingDirsRemoved);

                    if (buildingObj)
                    {
                        // TODO: differentiate between staged road objs and existing road objs
                        for (int i = network.RoadObjects.Count - 1; i >= 0; i--)
                        {
                            if (network.RoadObjects[i].GetComponent<OccupiesTile>().TileIndex == tileIndex)
                            {
                                // TODO: Check if there is nothing after staging mask is removed
                                pools.Roads.Free(network.RoadObjects[i]);
                                network.RoadObjects.RemoveAt(i);
                                break;
                            }
                        }

                        ZavalaGame.SimWorld.QueuedVisualUpdates.PushBack(new VisualUpdateRecord()
                        {
                            TileIndex = (ushort)tileIndex,
                            Type = VisualUpdateType.Road
                        });
                    }
                    break;
                case BuildingType.Digester:
                    RoadUtility.RemoveRoad(network, grid, pools, tileIndex, removeInleadingRoads, out inleadingDirsRemoved);
                    if (buildingObj)
                    {
                        pools.Digesters.Free(buildingObj.GetComponent<OccupiesTile>());
                    }
                    break;
                case BuildingType.Storage:
                    RoadUtility.RemoveRoad(network, grid, pools, tileIndex, removeInleadingRoads, out inleadingDirsRemoved);
                    if (buildingObj)
                    {
                        pools.Storages.Free(buildingObj.GetComponent<OccupiesTile>());
                    }
                    break;
                default:
                    break;
            }
        }

        public static void BuildOnTileFromHit(SimGridState grid, UserBuildTool activeTool, int tileIndex, Material inMat, out OccupiesTile occupies)
        {
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();
            switch (activeTool)
            {
                case UserBuildTool.Digester:
                    SimDataUtility.BuildOnTile(grid, pools.Digesters, tileIndex, inMat, out occupies, false);
                    break;
                case UserBuildTool.Storage:
                    SimDataUtility.BuildOnTile(grid, pools.Storages, tileIndex, inMat, out occupies, false);
                    break;
                case UserBuildTool.Skimmer:
                    SimDataUtility.BuildOnTile(grid, pools.Skimmers, tileIndex, inMat, out occupies, false);
                    break;
                default:
                    occupies = null;
                    break;
            }
        }

        public static void BuildOnTileFromUndo(SimGridState grid, BuildingType buildingType, int tileIndex, Material inMat, bool wasPending)
        {
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            OccupiesTile occupies;
            switch (buildingType)
            {
                case BuildingType.Road:
                    BuildRoadOnTile(grid, network, pools.Roads, tileIndex, inMat, out RoadInstanceController controller, true, wasPending);
                    break;
                case BuildingType.Digester:
                    BuildOnTile(grid, pools.Digesters, tileIndex, inMat, out occupies, true, wasPending);
                    break;
                case BuildingType.Storage:
                    BuildOnTile(grid, pools.Storages, tileIndex, inMat, out occupies, true, wasPending);
                    break;
                case BuildingType.Skimmer:
                    BuildOnTile(grid, pools.Skimmers, tileIndex, inMat, out occupies, true, wasPending);
                    break;

                default:
                    occupies = null;
                    break;
            }
        }

        private static void BuildOnTile(SimGridState grid, SerializablePool<OccupiesTile> pool, int tileIndex, Material inMat, out OccupiesTile occupies, bool inheritPending, bool wasPending = false)
        {
            // add build, snap to tile
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            grid.Terrain.Info[tileIndex].Flags |= TerrainFlags.IsOccupied;
            occupies = pool.Alloc(worldPos);

            if (!inheritPending || (inheritPending && wasPending))
            {
                occupies.Pending = true;
                // temporarily render the build as holo and commit to build queue
                var matSwap = occupies.GetComponent<MaterialSwap>();
                if (matSwap) { matSwap.SetMaterial(inMat); }
            }
        }

        /// <summary>
        /// Primarily used when Undoing single destroy actions
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="pool"></param>
        /// <param name="tileIndex"></param>
        /// <param name="inMat"></param>
        /// <param name="occupies"></param>
        private static void BuildRoadOnTile(SimGridState grid, RoadNetwork network, SerializablePool<RoadInstanceController> pool, int tileIndex, Material inMat, out RoadInstanceController controller, bool inheritPending, bool wasPending = false)
        {
            // add build, snap to tile
            HexVector pos = grid.HexSize.FastIndexToPos(tileIndex);
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            TerrainTileInfo terrainInfo = grid.Terrain.Info[tileIndex];
            RoadTileInfo roadInfo = network.Roads.Info[tileIndex];

            terrainInfo.Flags |= TerrainFlags.IsOccupied; // Necessary? Do we do this with other roads?
            roadInfo.Flags |= RoadFlags.IsAnchor;
            roadInfo.Flags |= RoadFlags.IsRoad;

            controller = pool.Alloc(worldPos);
            network.RoadObjects.PushBack(controller);

            if (!inheritPending || (inheritPending && wasPending))
            {
                controller.Position.Pending = true;
                // temporarily render the build as holo and commit to build queue
                var matSwap = controller.GetComponent<MaterialSwap>();
                if (matSwap) { matSwap.SetMaterial(inMat); }
            }
        }

        public static void RestoreSnapshot(RoadNetwork network, SimGridState grid, int tileIndex, RoadFlags rFlags, TerrainFlags tFlags, TileAdjacencyMask flowSnapshot)
        {
            network.Roads.Info[tileIndex].Flags = rFlags;
            grid.Terrain.Info[tileIndex].Flags = tFlags;
            network.Roads.Info[tileIndex].FlowMask = flowSnapshot;
        }
        
        [LeafMember("CameraInRegion")]
        static public bool CameraInRegion(uint regionIndex) {
            return Game.SharedState.Get<SimGridState>().CurrRegionIndex == (regionIndex-1); // 1-indexed to 0-indexed
        }

        [LeafMember("AgeOfRegion")]
        static public int AgeOfRegion(int regionIndex) {
            return Game.SharedState.Get<SimGridState>().Regions[regionIndex-1].Age; // 1-indexed to 0-indexed
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