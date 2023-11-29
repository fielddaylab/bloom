using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;
using Zavala.Input;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;
using Zavala.UI;
using UnityEngine.EventSystems;
using Zavala.Rendering;
using System.Collections.Generic;

namespace Zavala.Building
{
    // TODO: is this the right update phase?
    [SysUpdate(GameLoopPhase.Update)]
    public class UserBuildingSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BuildToolState, ShopState>
    {
        private static int CODE_INVALID = -1; // tried to use a tool on an invalid spot
        private static int CODE_UNCHANGED = -2; // tried to use a tool on the same spot as last work process
        private static float RAYCAST_RANGE = 100; // range of build/destroy raycast
        private static string PLAYERPLACED_TAG = "PlayerPlaced";
        private static string TILE_LAYER = "HexTile";
        private static string BUILDING_LAYER = "Building";
        private static string ROAD_LAYER = "Road";

        #region Inspector

        [SerializeField] private Material m_StagingMaterial; // material applied to ground beneath road tiles being staged
        [SerializeField] private Material m_ValidHoloMaterial; // material applied to buildings being staged
        [SerializeField] private Material m_InvalidHoloMaterial; // material applied to buildings being staged

        #endregion // Inspector

        public override void ProcessWork(float deltaTime) {
            UserBuildTool toolInUse = ToolInUse(); // the tool that is actively being applied via button inputs
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();

            if (toolInUse == UserBuildTool.Destroy) {
                TryDestroyClickedBuilding(world, grid, bpState);
            } else if (toolInUse != UserBuildTool.None) {
                TryApplyTool(grid, toolInUse, RaycastTileIndex(world, grid));
            } else if (m_StateC.RoadToolState.PrevTileIndex != -1) {
                RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

                // Road tool has stopped being applied, but the previous road was not finished
                // cancel the unfinished road
                CancelRoad(grid, network);
            }
        }

        /// <summary>
        /// Check if mouse is down with road tool, or mouse is pressed with any other tool
        /// </summary>
        private UserBuildTool ToolInUse() {
            if (m_StateC.ActiveTool == UserBuildTool.Road && m_StateA.ButtonDown(InputButton.PrimaryMouse)) {
                return UserBuildTool.Road;
            } else if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                return m_StateC.ActiveTool;
            }
            return UserBuildTool.None;
        }

        /// <summary>
        /// Meant to be a single multi-purpose raycast method - haven't yet implemented
        /// </summary>
        /// <param name="world"></param>
        /// <param name="grid"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private Collider RaycastCollider(SimWorldState world, SimGridState grid, LayerMask mask) {
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, RAYCAST_RANGE, mask)) {
                return hit.collider;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Try to get a tile index by raycasting to a tile.
        /// </summary>
        private int RaycastTileIndex(SimWorldState world, SimGridState grid) {
            // do a raycast
            // TODO: only raycast if the mouse has moved significantly since last placed tile?
            if (EventSystem.current.IsPointerOverGameObject()) {
                // return  if over UI
                // TODO: more permanent solution
                Log.Msg("[UserBuildingSystem] Raycast over UI, discarding.");
                return CODE_INVALID;
            }
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, RAYCAST_RANGE, LayerMask.GetMask(TILE_LAYER))) {
                if (!hit.collider) return CODE_INVALID;
                HexVector vec = HexVector.FromWorld(hit.collider.transform.position, world.WorldSpace);
                if (m_StateC.VecPrevValid && vec.Equals(m_StateC.VecPrev)) {
                    // same as last
                    return CODE_UNCHANGED;
                }
                int i = grid.HexSize.FastPosToIndex(vec);
                Log.Msg("[UserBuildingSystem] New raycast hit Tile {0}", i);

                // TODO: check if valid neighbor (otherwise try to draw line between current and previous, or wait for return)

                m_StateC.VecPrev = vec;
                m_StateC.VecPrevValid = true;

                return i;
            }
            else {
                Log.Msg("[UserBuildingSystem] Raycast missed.");
                return CODE_INVALID;
            }
        }

        /// <summary>
        /// Attempt to access a Building collider with a raycast from the mouse position
        /// </summary>
        /// <param name="world"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        private Collider RaycastBuilding(SimWorldState world, SimGridState grid) {
            if (EventSystem.current.IsPointerOverGameObject()) {
                // return  if over UI
                // TODO: more permanent solution
                Log.Msg("[UserBuildingSystem] Raycast over UI, discarding.");
                return null;
            }
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, RAYCAST_RANGE, LayerMask.GetMask(BUILDING_LAYER, ROAD_LAYER, "UI"))) {
                Log.Msg("[UserBuildingSystem] RaycastBuilding hit building {0}", hit.collider.transform.name);
                return hit.collider;
            } else return null;
        }

        /// <summary>
        /// Attempt to place tile on given tile index using active tool
        /// </summary>
        private void TryApplyTool(SimGridState grid, UserBuildTool activeTool, int tileIndex) {
            if (tileIndex == CODE_INVALID) {
                Log.Msg("[UserBuildingSystem] Invalid build location: tile {0} out of bounds", tileIndex);

                if (activeTool == UserBuildTool.Road) {
                    RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
                    // cancel in-progress road 
                    CancelRoad(grid, network);
                }
                return;
            }
            if (tileIndex == CODE_UNCHANGED) {
                // player has not moved to a new tile yet, and has not stopped applying the build tool
                // if (activeTool == UserBuildTool.Road) { return; }

                return;
            }
            if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.NonBuildable) != 0) {
                Log.Msg("[UserBuildingSystem] Invalid build location: tile {0} unbuildable", tileIndex);
                return;
            }

            switch (activeTool) {
                case UserBuildTool.Road:
                    RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
                    TryBuildRoad(grid, network, tileIndex);
                    break;
                case UserBuildTool.Digester:
                case UserBuildTool.Storage:
                case UserBuildTool.Skimmer:
                    TryBuildOnTile(grid, activeTool, tileIndex);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// For single tile builds
        /// </summary>
        private bool TryBuildOnTile(SimGridState grid, UserBuildTool activeTool, int tileIndex) {
            // disallow: water, existing building
            // TODO: disallow road?
            bool validLocation = true;
            if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.IsWater) != 0) validLocation = false;
            if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.IsOccupied) != 0) validLocation = false;
            if (!validLocation) {
                return false;
            }

            if (!CanPurchaseBuild(activeTool, grid.CurrRegionIndex, m_StateD.RunningCost, out int price)) {
                return false;
            }

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
            RoadFlags rFlagSnapshot = network.Roads.Info[tileIndex].Flags;
            TerrainFlags tFlagSnapshot = grid.Terrain.Info[tileIndex].Flags;
            TileAdjacencyMask flowSnapshot = network.Roads.Info[tileIndex].FlowMask;

            SimDataUtility.BuildOnTileFromHit(grid, activeTool, tileIndex, m_ValidHoloMaterial, out OccupiesTile occupies);

            Assert.NotNull(occupies);

            // Create the build commit
            BlueprintState blueprintState = Game.SharedState.Get<BlueprintState>();
            BlueprintUtility.CommitBuild(blueprintState, new ActionCommit(
                occupies.Type,
                ActionType.Build,
                price,
                tileIndex,
                new List<TileDirection>(),
                occupies.gameObject,
                rFlagSnapshot,
                tFlagSnapshot,
                flowSnapshot
                ));

            // Add cost to receipt queue
            ShopUtility.EnqueueCost(m_StateD, price);

            // Deselect tools
            BuildToolUtility.SetTool(m_StateC, UserBuildTool.None);
            m_StateC.VecPrevValid = false;

            return true;
        }

        private bool CanPurchaseBuild(UserBuildTool currTool, uint currentRegion, int runningCost, out int price) {
            return ShopUtility.CanPurchaseBuild(currTool, currentRegion, 1, runningCost, out price);
        }

        /// <summary>
        /// Attempt a raycast to destroy a building at the mouse position.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="grid"></param>
        /// <returns>true if building destroy dialog spawned, false otherwise</returns>
        private bool TryDestroyClickedBuilding(SimWorldState world, SimGridState grid, BlueprintState blueprintState) {
            // TODO: streamline this?
            Collider hit = RaycastBuilding(world, grid);
            if (hit != null && hit.gameObject.tag == PLAYERPLACED_TAG) {
                Vector3 pos = m_StateB.Camera.WorldToScreenPoint(hit.transform.position + new Vector3(0,0.5f,0));
                SimDataUtility.DestroyBuildingFromHit(grid, blueprintState, hit.gameObject);

                /*BuildingPopup.instance.ShowDestroyMenu(pos, "Destroy " + hit.transform.name, null, "Are you sure?", () => {
                    SimDataUtility.DestroyBuildingFromHit(grid, hit.gameObject);
                }, null);*/
                return true;
            }
            return false;
        }

        #region Road Building

        private void TryBuildRoad(SimGridState grid, RoadNetwork network, int tileIndex) {
            if (m_StateC.RoadToolState.PrevTileIndex == -1) {
                // Start building road (this would be the first road piece)

                // Check if a valid start, (ResourceSupplier, ResourceRequester, or Road)
                if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsAnchor) != 0) {
                    StageRoad(grid, network, tileIndex);
                    Debug.Log("[UserBuildingSystem] Is road anchor. Added new tile to road path");
                }
                else {
                    Debug.Log("[UserBuildingSystem] invalid start");
                }
            }
            else {
                // Continue building road

                // Verify road is continuous
                if (!IsContinuous(grid.HexSize, m_StateC.RoadToolState.PrevTileIndex, tileIndex, grid)) {
                    Debug.Log("[UserBuildingSystem] Cannot build a non-continuous road");
                    CancelRoad(grid, network);
                    return;
                }

                if (m_StateC.RoadToolState.TracedTileIdxs.Contains(tileIndex)) {
                    // Handle a change to a tile that is already part of the road to be built
                    // don't add to list, and rewind back to that tile

                    int rewindIndex = m_StateC.RoadToolState.TracedTileIdxs.IndexOf(tileIndex);
                    int numToUnstage = m_StateC.RoadToolState.TracedTileIdxs.Count - 1 - rewindIndex;
                    Debug.Log("[UserBuildingSystem] num to unstage: " + numToUnstage);
                    RewindStagedRoads(grid, network, rewindIndex);

                    Debug.Log("[UserBuildingSystem] rewound to index " + rewindIndex);
                }
                else {
                    // Handle a change to a tile that is not already part of the road to be built
                    // add if possible

                    bool passedTollCheck = true;
                    // TODO: implement toll checking

                    if (passedTollCheck) {

                        // Check if reached a road anchor
                        if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsAnchor) != 0) {
                            // stage road
                            StageRoad(grid, network, tileIndex);
                            Debug.Log("[UserBuildingSystem] Is road anchor. Added new tile to road path");

                            // reached road anchor
                            if (TryFinishRoad(grid, network)) {
                                // completed
                                return;
                            }
                            else {
                                // unable to complete road (whether invalid, insufficient funds, or some other reason)
                                CancelRoad(grid, network);
                            }
                            return;
                        }
                        else if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.NonBuildable) != 0) {
                            // Tile is not buildable
                            // cannot build a road through non-buildable tiles
                            Debug.Log("[UserBuildingSystem] Cannot build a road through non-buildable tiles");
                            // CancelRoad(grid, network);
                            return;
                        }
                        else {
                            // stage road
                            StageRoad(grid, network, tileIndex);
                            Debug.Log("[UserBuildingSystem] added new tile to road path");
                        }
                    }
                    else {
                        // cannot cross regions through non-toll tiles
                        Debug.Log("[UserBuildingSystem] Cannot cross regions through non-toll tiles");
                        CancelRoad(grid, network);
                        return;
                    }
                }
            }
        }

        private void StageRoad(SimGridState grid, RoadNetwork network, int tileIndex) {
            m_StateC.RoadToolState.TracedTileIdxs.Add(tileIndex);

            // calculate staging if not first in road sequence
            if (m_StateC.RoadToolState.TracedTileIdxs.Count > 1) {
                // find previous tile
                int prevTileIndex = m_StateC.RoadToolState.TracedTileIdxs[m_StateC.RoadToolState.TracedTileIdxs.Count - 2];
                HexVector currPos = grid.HexSize.FastIndexToPos(tileIndex);
                for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                    HexVector adjPos = HexVector.Offset(currPos, dir);
                    if (!grid.HexSize.IsValidPos(adjPos)) {
                        continue;
                    }
                    int adjIdx = grid.HexSize.PosToIndex(adjPos);
                    if (adjIdx == prevTileIndex) {
                        // Stage direction from previous to curr, and curr to previous
                        int prevIdx = adjIdx;

                        TileDirection currDir = dir; // to stage into curr road
                        TileDirection prevDir = currDir.Reverse(); // to stage into prev road

                        // For curr road, add a staging mask that gets merged into flow mask upon successful road build
                        RoadUtility.StageRoad(network, grid, tileIndex, currDir);
                        // For prev road, add a staging mask that gets merged into flow mask upon successful road build
                        RoadUtility.StageRoad(network, grid, prevTileIndex, prevDir);
                        break;
                    }
                }

                RoadTileInfo tileInfo = network.Roads.Info[tileIndex];
                // If not the end of a road, create a road object
                if ((tileInfo.Flags & RoadFlags.IsAnchor) == 0)
                {
                    BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                    RoadUtility.CreateRoadObject(network, grid, pools, tileIndex, m_ValidHoloMaterial);
                }
            }

            m_StateC.RoadToolState.PrevTileIndex = tileIndex;

            // add staging visuals
            SetStagingRenderer(tileIndex, true);

            // RoadUtility.AddRoadImmediate(Game.SharedState.Get<RoadNetwork>(), grid, tileIndex); // temp debug
        }

        private bool IsContinuous(HexGridSize hexSize, int prevIndex, int currIndex, SimGridState grid) {
            HexVector currPos = hexSize.FastIndexToPos(currIndex);
            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                HexVector adjPos = HexVector.Offset(currPos, dir);
                if (!hexSize.IsValidPos(adjPos)) {
                    continue;
                }
                int adjIdx = hexSize.PosToIndex(adjPos);
                if (adjIdx != prevIndex) {
                    // prev tile found in adj neighbors
                    continue;
                }
                if (grid.Terrain.Regions[prevIndex] != grid.Terrain.Regions[currIndex]) {
                    continue;
                }
                return true;
            }

            // prev tile not found anywhere in adj neighbors
            return false;
        }

        /// <summary>
        /// Undo a staged road (not used during finaliziation). Main use is for rewinding roads.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="network"></param>
        /// <param name="tileIndex"></param>
        /// <param name="tracedIndex"></param>
        private void UnstageRoad(SimGridState grid, RoadNetwork network, int tileIndex, int tracedIndex) {
            // remove staging mask
            RoadUtility.UnstageRoad(network, grid, tileIndex);

            m_StateC.RoadToolState.TracedTileIdxs.RemoveAt(tracedIndex);

            // remove staging visuals
            SetStagingRenderer(tileIndex, false);
        }

        private void UnstageForward(SimGridState grid, RoadNetwork network, int prevTileIndex) {
            // remove staging mask from prev leading into this
            RoadUtility.UnstageForward(network, grid, prevTileIndex);
        }

        private void RewindStagedRoads(SimGridState grid, RoadNetwork network, int rewindIndex) {
            for (int i = m_StateC.RoadToolState.TracedTileIdxs.Count - 1; i > rewindIndex; i--) {
                UnstageRoad(grid, network, m_StateC.RoadToolState.TracedTileIdxs[i], i);
                // if last before rewindIndex && not first traced tile, unstage the forward direction of previous staged road
                if (i == rewindIndex + 1 && i > 0) {
                    UnstageForward(grid, network, m_StateC.RoadToolState.TracedTileIdxs[i - 1]);
                }
            }
            m_StateC.RoadToolState.PrevTileIndex = m_StateC.RoadToolState.TracedTileIdxs[rewindIndex];
        }

        private void FinalizeRoad(SimGridState grid, RoadNetwork network, BuildingPools pools, int tileIndex, bool isEndpoint) {
            RoadUtility.FinalizeRoad(network, grid, pools, tileIndex, isEndpoint, m_ValidHoloMaterial);

            // remove staging visuals
            SetStagingRenderer(tileIndex, false);
        }

        private void SetStagingRenderer(int tileIndex, bool isStaging) {
            SimWorldState world = ZavalaGame.SimWorld;
            if (isStaging) {
                TileEffectRendering.SetMaterial(world.Tiles[tileIndex], m_StagingMaterial);
            }
            else {
                TileEffectRendering.RestoreDefaultMaterial(world.Tiles[tileIndex]);
            }
        }

        /// <summary>
        /// Finalizes the in-progress road
        /// Triggered when Player releases the button, or Player drags the road into a road termination point (supplier, another road, etc)
        /// </summary>
        private bool TryFinishRoad(SimGridState grid, RoadNetwork network) {
            // Check if road can be purchased
            int roadCount = m_StateC.RoadToolState.TracedTileIdxs.Count;

            bool anyRoad = false;

            // count num of actual road segments being built
            int deductNum = 0;
            for (int i = 0; i < m_StateC.RoadToolState.TracedTileIdxs.Count; i++) {
                bool isEndpoint = i == 0 || i == m_StateC.RoadToolState.TracedTileIdxs.Count - 1;
                int currIndex = m_StateC.RoadToolState.TracedTileIdxs[i];

                RoadTileInfo tileInfo = network.Roads.Info[currIndex];

                if ((tileInfo.Flags & RoadFlags.IsRoad) != 0) {
                    anyRoad = true;
                }
                else if (isEndpoint) {
                    // only if counting each half of a road
                    // deductNum++;

                    if (i == m_StateC.RoadToolState.TracedTileIdxs.Count - 1)
                    {
                        BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                        RoadUtility.RemoveStagedRoadObj(network, pools, currIndex, !tileInfo.FlowMask.IsEmpty);
                    }
                }
            }

            if (roadCount < 3 && !anyRoad) {
                // can only connect to adj tile if at least one is a road
                return false;
            }

            bool purchaseSuccessful = ShopUtility.CanPurchaseBuild(UserBuildTool.Road, grid.CurrRegionIndex, roadCount - deductNum, m_StateD.RunningCost, out int totalPrice);


            int unitCost = ShopUtility.PriceLookup(UserBuildTool.Road); // price for 1 segment
            if (purchaseSuccessful) {
                Debug.Log("[StagingRoad] Finalizing road...");

                BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                BlueprintState bpState = Game.SharedState.Get<BlueprintState>();

                CommitChain roadChain = new CommitChain();
                roadChain.Chain = new RingBuffer<ActionCommit>(m_StateC.RoadToolState.TracedTileIdxs.Count);

                int roadObjIdx = network.RoadObjects.Count - 1;

                // Merge staged masks into flow masks
                for (int i = 0; i < m_StateC.RoadToolState.TracedTileIdxs.Count; i++) {
                    bool isEndpoint = i == 0 || i == m_StateC.RoadToolState.TracedTileIdxs.Count - 1;
                    int currIndex = m_StateC.RoadToolState.TracedTileIdxs[i];

                    RoadFlags rFlagsSnapshot = network.Roads.Info[currIndex].Flags;
                    TerrainFlags tFlagsSnapshot = grid.Terrain.Info[currIndex].Flags;
                    TileAdjacencyMask flowSnapshot = network.Roads.Info[currIndex].FlowMask;


                    FinalizeRoad(grid, network, pools, currIndex, isEndpoint);

                    // Add the road commit to the overall chain
                    GameObject roadObj = isEndpoint ? null : network.RoadObjects[roadObjIdx].gameObject;
                    roadChain.Chain.PushBack(new ActionCommit(
                        BuildingType.Road,
                        ActionType.Build,
                        unitCost,
                        currIndex,
                        new List<TileDirection>(),
                        roadObj,
                        rFlagsSnapshot,
                        tFlagsSnapshot,
                        flowSnapshot
                        ));

                    // update road visuals
                    RoadUtility.UpdateRoadVisuals(network, currIndex);

                    if (!isEndpoint)
                    {
                        roadObjIdx--;
                    }
                }
                // Commit chain to build stack
                BlueprintUtility.CommitChain(bpState, roadChain);

                // Deselect tools
                BuildToolUtility.SetTool(m_StateC, UserBuildTool.None);

                // Add cost to receipt queue
                ShopUtility.EnqueueCost(m_StateD, totalPrice);

                ClearRoadToolState();

                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// Cancels the in-progress road
        /// Triggered when Player releases the button, or Player drags the road into a road termination point (supplier, another road, etc)
        /// </summary>
        private void CancelRoad(SimGridState grid, RoadNetwork network) {
            // unstage all in-progress roads
            for (int i = m_StateC.RoadToolState.TracedTileIdxs.Count - 1; i >= 0; i--) {
                UnstageRoad(grid, network, m_StateC.RoadToolState.TracedTileIdxs[i], i);
            }

            ClearRoadToolState();
        }

        /// <summary>
        /// Helper function to reset road state
        /// </summary>
        private void ClearRoadToolState() {
            m_StateC.RoadToolState.ClearState();
            m_StateC.VecPrevValid = false;
        }

        #endregion // Road Building
    }
}