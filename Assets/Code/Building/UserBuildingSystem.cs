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
using Zavala.Audio;
using FieldDay.Scripting;
using Zavala.Rendering;

namespace Zavala.Building
{
    [SysUpdate(GameLoopPhase.Update)] // Before BlueprintOverlaySystem
    public class UserBuildingSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BuildToolState, ShopState>
    {
        private static int CODE_INVALID = -1; // tried to use a tool on an invalid spot
        private static int CODE_UNCHANGED = -2; // tried to use a tool on the same spot as last work process
        private static float RAYCAST_RANGE = 100; // range of build/destroy raycast
        private const string PLAYERPLACED_TAG = "PlayerPlaced";
        private const string TILE_LAYER = "HexTile";
        private const string BUILDING_LAYER = "Building";
        private const string ROAD_LAYER = "Road";

        #region Inspector

        [SerializeField] private Material m_StagingMaterial; // material applied to ground beneath road tiles being staged
        [SerializeField] private Material m_ValidHoloMaterial; // material applied to buildings being staged
        [SerializeField] private Material m_ValidHoloRoadMaterial; // material applied to roads being staged
        [SerializeField] private Material m_InvalidHoloMaterial; // material applied to buildings being staged

        #endregion // Inspector

        public override void ProcessWork(float deltaTime) {
            UpdateRoadDraggingInput();

            UserBuildTool toolInUse = ToolInUse(); // the tool that is actively being applied via button inputs
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            UserBuildTool toolPreview = ToolInPreview();

            if (bpState.IsActive && (m_StateC.ToolUpdated || m_StateC.RegionSwitched))
            {
                if (m_StateC.ActiveTool != UserBuildTool.Road) {
                    // Regenerate BlockedTiles
                    BuildToolUtility.RecalculateBlockedTiles(grid, world, network, m_StateC);
                } else {
                    BuildToolUtility.RecalculateBlockedTilesForRoads(grid, world, network, m_StateC);
                }
            }

            if (toolInUse == UserBuildTool.Destroy) {
                TryDestroyClickedBuilding(world, grid, bpState);                
            } else if (toolInUse != UserBuildTool.None) {
                TryApplyTool(grid, network, toolInUse, RaycastTileIndex(world, grid));
            } else if (m_StateC.RoadToolState.PrevTileIndex != -1) {
                // Road tool has stopped being applied, but the previous road was not finished
                // cancel the unfinished road
                CancelRoad(grid, network);
            }
        }

        private void UpdateRoadDraggingInput() {
            if (m_StateC.ActiveTool != UserBuildTool.Road) {
                return;
            }

            if (!m_StateC.RoadToolState.Dragging) {
                if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                    m_StateC.RoadToolState.Dragging = true;
                }
            } else {
                if (!m_StateA.ButtonDown(InputButton.PrimaryMouse)) {
                    m_StateC.RoadToolState.Dragging = false;
                }
            }
        }

        /// <summary>
        /// Check if mouse is down with road tool, or mouse is pressed with any other tool
        /// </summary>
        private UserBuildTool ToolInUse() {
            if (m_StateC.ActiveTool == UserBuildTool.Road && m_StateC.RoadToolState.Dragging) {
                return UserBuildTool.Road;
            } else if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                return m_StateC.ActiveTool;
            }
            return UserBuildTool.None;
        }

        /// <summary>
        /// Check if mouse is down with road tool, or mouse is pressed with any other tool
        /// </summary>
        private UserBuildTool ToolInPreview() {
            return m_StateC.ActiveTool;
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
            if (Game.Input.IsPointerOverCanvas()) {
                // return  if over UI
                // TODO: more permanent solution
                Log.Msg("[UserBuildingSystem] Raycast over UI, discarding.");
                return CODE_INVALID;
            }
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, RAYCAST_RANGE, LayerMasks.HexTile_Mask)) {
                if (!hit.collider) return CODE_INVALID;
                HexVector vec = HexVector.FromWorld(hit.collider.transform.position, world.WorldSpace);
                if (m_StateC.VecPrevValid && vec.Equals(m_StateC.VecPrev)) {
                    // same as last
                    return CODE_UNCHANGED;
                }
                int i = grid.HexSize.FastPosToIndex(vec);

                // lock to this region
                if (grid.Terrain.Regions[i] != grid.CurrRegionIndex) {
                    return CODE_INVALID;
                }

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
            if (Game.Input.IsPointerOverCanvas()) {
                // return  if over UI
                // TODO: more permanent solution
                Log.Msg("[UserBuildingSystem] Raycast over UI, discarding.");
                return null;
            }
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            if (Physics.Raycast(mouseRay, out RaycastHit hit, RAYCAST_RANGE, LayerMasks.Building_Mask | LayerMasks.Road_Mask | LayerMasks.UI_Mask)) {
                Log.Msg("[UserBuildingSystem] RaycastBuilding hit building {0}", hit.collider.transform.name);
                return hit.collider;
            } else return null;
        }

        /// <summary>
        /// Attempt to place tile on given tile index using active tool
        /// </summary>
        private void TryApplyTool(SimGridState grid, RoadNetwork network, UserBuildTool activeTool, int tileIndex) {
            if (tileIndex == CODE_INVALID) {
                Log.Msg("[UserBuildingSystem] Invalid build location: tile {0} out of bounds", tileIndex);

                if (activeTool == UserBuildTool.Road) {
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
                if (activeTool == UserBuildTool.Road) {
                    SfxUtility.PlaySfx("road-nonbuildable");
                }
                return;
            }

            switch (activeTool) {
                case UserBuildTool.Road:
                    TryBuildRoad(grid, network, tileIndex);
                    break;
                case UserBuildTool.Digester:
                case UserBuildTool.Storage:
                case UserBuildTool.Skimmer:
                    if (!TryBuildOnTile(grid, network, activeTool, tileIndex)) {
                        ZavalaGame.Events.Dispatch(GameEvents.BuildInvalid, tileIndex);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// For single tile builds
        /// </summary>
        private bool TryBuildOnTile(SimGridState grid, RoadNetwork network, UserBuildTool activeTool, int tileIndex) {
            // disallow: water, existing building
            bool validLocation = true;
            if (m_StateC.BlockedTileBuffer[tileIndex] == 1) { validLocation = false; }
            if (!validLocation) {
                return false;
            }

            if (!CanPurchaseBuild(activeTool, grid.CurrRegionIndex, m_StateD.RunningCost, out int price)) {
                SfxUtility.PlaySfx("insufficient-funds");
                return false;
            }

            RoadFlags rFlagSnapshot = network.Roads.Info[tileIndex].Flags;
            TerrainFlags tFlagSnapshot = grid.Terrain.Info[tileIndex].Flags;
            TileAdjacencyMask flowSnapshot = network.Roads.Info[tileIndex].FlowMask;

            bool brokenReplaced = false;
            if (activeTool == UserBuildTool.Digester) {
                BuildToolState bts = Game.SharedState.Get<BuildToolState>();
                if (bts.DigesterOnlyTiles.Contains(tileIndex)) {
                    TryDestroyClickedBuilding(ZavalaGame.SimWorld, grid, Game.SharedState.Get<BlueprintState>());
                    brokenReplaced = true;
                }
            }
            SimDataUtility.BuildOnTileFromHit(grid, activeTool, tileIndex, m_ValidHoloMaterial, out OccupiesTile occupies);

            Assert.NotNull(occupies);

            // Create the build commit
            BlueprintState blueprintState = Game.SharedState.Get<BlueprintState>();
            BlueprintUtility.CommitBuild(blueprintState, new ActionCommit(
                occupies.Type,
                ActionType.Build,
                price,
                tileIndex,
                default,
                occupies.gameObject,
                rFlagSnapshot,
                tFlagSnapshot,
                flowSnapshot,
                occupies.Pending
                ));
            if (brokenReplaced) {
                // merge the "destroy old" chain and the "build new" chain into one chain
                BlueprintUtility.MergeChains(blueprintState, 2);
            }
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

            if (hit != null && hit.gameObject.TryGetComponent(out OccupiesTile ot)) {
                // if not player placed, outside current region, or non-pending non-road
                if (!hit.gameObject.CompareTag(PLAYERPLACED_TAG) || 
                    ot.RegionIndex != grid.CurrRegionIndex || 
                    (ot.Type != BuildingType.Road && ot.Type != BuildingType.DigesterBroken && !ot.Pending)) {
                    ZavalaGame.Events.Dispatch(GameEvents.DestroyInvalid, ot.TileIndex);
                    return false;
                }

                SimDataUtility.DestroyBuildingFromHit(grid, blueprintState, hit.gameObject, ot);

                /*BuildingPopup.instance.ShowDestroyMenu(pos, "Destroy " + hit.transform.name, null, "Are you sure?", () => {
                    SimDataUtility.DestroyBuildingFromHit(grid, hit.gameObject);
                }, null);*/
                return true;
            }
            ZavalaGame.Events.Dispatch(GameEvents.DestroyInvalid, -1);
            return false;
        }

        #region Road Building

        private void TryBuildRoad(SimGridState grid, RoadNetwork network, int tileIndex) {
            if (m_StateC.RoadToolState.PrevTileIndex == -1) {
                // Start building road (this would be the first road piece)

                // Check if a valid start, (ResourceSupplier, ResourceRequester, or Road)
                if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsAnchor) != 0) {
                    if (TryStageRoad(grid, network, tileIndex, false)) {
                        Debug.Log("[UserBuildingSystem] Is road anchor. Added new tile to road path");
                        SfxUtility.PlaySfx("build-road-begin");
                        VfxUtility.PlayEffect(SimWorldUtility.GetTileCenter(tileIndex), EffectType.Road_AnchorInteract);
                    }
                }
                else {
                    Debug.Log("[UserBuildingSystem] invalid start");
                    m_StateC.RoadToolState.Dragging = false;
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

                // check for toll
                if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.IsToll) != 0) {
                    if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsConnectionEndpoint) == 0) {
                        Debug.Log("[UserBuildingSystem] Cannot build onto a pending toll");
                        SfxUtility.PlaySfx("road-nonbuildable");
                        return;
                    }
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
                            if (!TryStageRoad(grid, network, tileIndex, false)) {
                                Debug.Log("[UserBuildingSystem] Could not stage road. Connection already made.");
                                CancelRoad(grid, network);
                                return;
                            }
                            Debug.Log("[UserBuildingSystem] Is road anchor. Added new tile to road path");

                            // reached road anchor
                            if (TryFinishRoad(grid, network)) {
                                // completed
                                SfxUtility.PlaySfx("build-road-finish");
                                VfxUtility.PlayEffect(SimWorldUtility.GetTileCenter(tileIndex), EffectType.Road_AnchorInteract);
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
                            SfxUtility.PlaySfx("road-nonbuildable");
                            // CancelRoad(grid, network);
                            return;
                        }
                        else if ((m_StateC.DigesterOnlyTiles.Contains(tileIndex))) {
                            Debug.Log("[UserBuildingSystem] Cannot build a road through digester-only tile");
                            SfxUtility.PlaySfx("road-nonbuildable");
                            return;
                        }
                        else {
                            // stage road
                            TryStageRoad(grid, network, tileIndex, true);
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

        private bool TryStageRoad(SimGridState grid, RoadNetwork network, int tileIndex, bool playSfx) {
            // calculate staging if not first in road sequence
            if (m_StateC.RoadToolState.TracedTileIdxs.Count > 0) {
                // find previous tile
                int prevTileIndex = m_StateC.RoadToolState.TracedTileIdxs[m_StateC.RoadToolState.TracedTileIdxs.Count - 1];
                HexVector currPos = grid.HexSize.FastIndexToPos(tileIndex);

                TileDirection dir = grid.HexSize.FastDirectionTowards(tileIndex, prevTileIndex);
                // Stage direction from previous to curr, and curr to previous

                RoadTileInfo idxRoadTile = network.Roads.Info[tileIndex];

                if ((idxRoadTile.StagingMask | idxRoadTile.FlowMask)[dir]) {
                    Log.Msg("[UserBuildingSystem] Trying to build across a connection that already exists or is staged");
                    return false;
                }

                TileDirection currDir = dir; // to stage into curr road
                TileDirection prevDir = currDir.Reverse(); // to stage into prev road

                // For curr road, add a staging mask that gets merged into flow mask upon successful road build
                RoadUtility.StageRoad(network, grid, tileIndex, currDir);
                // For prev road, add a staging mask that gets merged into flow mask upon successful road build
                RoadUtility.StageRoad(network, grid, prevTileIndex, prevDir);

                RoadTileInfo tileInfo = network.Roads.Info[tileIndex];
                // If not the end of a road, create a road object
                if ((tileInfo.Flags & RoadFlags.IsAnchor) == 0)
                {
                    BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                    RoadUtility.CreateRoadObject(network, grid, pools, tileIndex, m_ValidHoloRoadMaterial);
                }
                // Add to running cost (include end roads)
                ShopUtility.EnqueueCost(m_StateD, ShopUtility.PriceLookup(UserBuildTool.Road));

                if (playSfx) {
                    SfxUtility.PlaySfx("build-road");
                }
            }

            m_StateC.RoadToolState.TracedTileIdxs.Add(tileIndex);
            m_StateC.RoadToolState.PrevTileIndex = tileIndex;
            return true;

            // RoadUtility.AddRoadImmediate(Game.SharedState.Get<RoadNetwork>(), grid, tileIndex); // temp debug
        }

        private bool IsContinuous(HexGridSize hexSize, int prevIndex, int currIndex, SimGridState grid) {
            if (!hexSize.IsNeighbor(prevIndex, currIndex, out var _)) {
                return false;
            }

            if (grid.Terrain.Regions[prevIndex] != grid.Terrain.Regions[currIndex]) {
                return false;
            }

            return true;
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
            // skip refunding first road, because it didn't cost anything to begin with
            if (tracedIndex <= 0) {
                return;
            } else {
                ShopUtility.EnqueueCost(m_StateD, -ShopUtility.PriceLookup(UserBuildTool.Road));
            }
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
            int prevPrev = m_StateC.RoadToolState.PrevTileIndex;
            m_StateC.RoadToolState.PrevTileIndex = m_StateC.RoadToolState.TracedTileIdxs[rewindIndex];
            if (m_StateC.RoadToolState.PrevTileIndex != prevPrev) {
                SfxUtility.PlaySfx("road-rewind");
            }
        }

        private void FinalizeRoad(SimGridState grid, RoadNetwork network, BuildingPools pools, int tileIndex, bool isEndpoint) {
            RoadUtility.FinalizeRoad(network, grid, pools, tileIndex, isEndpoint, m_ValidHoloRoadMaterial);
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
            int startIndex = -1;
            for (int i = 0; i < roadCount; i++) {
                int currIndex = m_StateC.RoadToolState.TracedTileIdxs[i];
                
                RoadTileInfo tileInfo = network.Roads.Info[currIndex];

                if ((tileInfo.Flags & RoadFlags.IsRoad) != 0) {
                    // there is a road somewhere in this path
                    anyRoad = true;
                }
                // do not count endpoints in price
                if (i == 0) { // first node
                    deductNum++;
                    startIndex = currIndex;
                } else if (i == roadCount - 1) { // last node
                    int endIndex = currIndex;
                    // if start == end (loop road), don't double-count the deduction
                    // if (endIndex != startIndex) deductNum++;

                    BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                    RoadUtility.RemoveStagedRoadObj(network, pools, currIndex, !tileInfo.FlowMask.IsEmpty);
                }
                
            }

            if (roadCount < 2 /*&& !anyRoad*/) {
                // can only connect to adj tile if at least one is a road
                return false;
            }

            bool purchaseSuccessful = ShopUtility.CanPurchaseBuild(UserBuildTool.Road, grid.CurrRegionIndex, roadCount - deductNum, m_StateD.RunningCost, out int totalPrice);


            if (purchaseSuccessful) {
                Debug.Log("[StagingRoad] Finalizing road...");

                BuildingPools pools = Game.SharedState.Get<BuildingPools>();
                BlueprintState bpState = Game.SharedState.Get<BlueprintState>();

                CommitChain roadChain = new CommitChain();
                roadChain.Chain = new RingBuffer<ActionCommit>(roadCount);

                int roadObjIdx = network.RoadObjects.Count - 1;

                // Merge staged masks into flow masks
                int unitCost;
                for (int i = 0; i < roadCount; i++) {
                    bool isEndpoint = i == 0 || i == roadCount - 1;
                    int currIndex = m_StateC.RoadToolState.TracedTileIdxs[i];

                    RoadFlags rFlagsSnapshot = network.Roads.Info[currIndex].Flags;
                    TerrainFlags tFlagsSnapshot = grid.Terrain.Info[currIndex].Flags;
                    TileAdjacencyMask flowSnapshot = network.Roads.Info[currIndex].FlowMask;

                    FinalizeRoad(grid, network, pools, currIndex, isEndpoint);

                    if (!isEndpoint || i == roadCount - 1) {
                        unitCost = ShopUtility.PriceLookup(UserBuildTool.Road);
                    } else {
                        unitCost = 0;
                    } 

                    // Add the road commit to the overall chain
                    GameObject roadObj = isEndpoint ? null : network.RoadObjects[roadObjIdx].gameObject;
                    if (!isEndpoint) {
                        network.RoadObjects[roadObjIdx].Position.Pending = true;
                    }
                    roadChain.Chain.PushBack(new ActionCommit(
                        BuildingType.Road,
                        ActionType.Build,
                        unitCost,
                        currIndex,
                        default,
                        roadObj,
                        rFlagsSnapshot,
                        tFlagsSnapshot,
                        flowSnapshot,
                        true
                        ));

                    // update road visuals
                    RoadUtility.UpdateRoadVisuals(network, currIndex);

                    if (!isEndpoint) {
                        roadObjIdx--;
                    }
                }
                // Commit chain to build stack
                BlueprintUtility.CommitChain(bpState, roadChain);

                // Deselect tools
                //BuildToolUtility.SetTool(m_StateC, UserBuildTool.None);

                // Add cost to receipt queue (now done on a tile-by-tile basis
                // ShopUtility.EnqueueCost(m_StateD, totalPrice);

                ClearRoadToolState();

                TutorialState tut = Find.State<TutorialState>();
                if (tut.AddFlag(TutorialState.Flags.ValidRoadPreviewed)) {
                    TutorialState.HidePanelWithName("ClickDragTutorial");
                }

                return true;
            } else {
                Debug.Log("[StagingRoad] Insufficient funds!");
                SfxUtility.PlaySfx("insufficient-funds");
                return false;
            }
        }

        /// <summary>
        /// Cancels the in-progress road
        /// Triggered when Player releases the button, or Player drags the road into a road termination point (supplier, another road, etc)
        /// </summary>
        private void CancelRoad(SimGridState grid, RoadNetwork network) {
            // unstage all in-progress roads
            if (m_StateC.RoadToolState.TracedTileIdxs.Count > 0) {
                SfxUtility.PlaySfx("road-cancel");
            }
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