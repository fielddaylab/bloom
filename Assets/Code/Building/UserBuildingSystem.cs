using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Building
{
    // TODO: is this the right update phase?
    [SysUpdate(GameLoopPhase.Update)]
    public class UserBuildingSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BuildToolState, SimWorldState>
    {
        private static int CODE_INVALID = -1; // tried to use a tool on an invalid spot
        private static int CODE_UNCHANGED = -2; // tried to use a tool on the same spot as last work process

        #region Inspector

        [SerializeField] private Material m_stagingMaterial; // material applied to tiles being staged

        #endregion // Inspector

        public override void ProcessWork(float deltaTime) {
            UserBuildTool toolInUse = ToolInUse(); // the tool that is actively being applied via button inputs
            if (toolInUse != UserBuildTool.None) {
                SimWorldState world = m_StateD;
                SimGridState grid = ZavalaGame.SimGrid;
                RoadNetwork network = Game.SharedState.Get<RoadNetwork>();
                TryBuildTile(grid, network, toolInUse, RaycastTileIndex(world, grid));
            }
            else if (m_StateC.RoadToolState.PrevTileIndex != -1) {
                SimGridState grid = ZavalaGame.SimGrid;
                RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

                // Road tool has stopped being applied, but the previous road was not finished
                // cancel the unfinished road
                CancelRoad(grid, network);
            }
        }

        /// <summary>
        /// Check if player clicked with a tool selected
        /// </summary>
        private UserBuildTool ToolInUse() {
            // if mouse pressed
            // TODO: change to if ButtonDown && position significantly different from pressed position
            if (m_StateA.ButtonDown(InputButton.PrimaryMouse) || m_StateA.MouseDragging)
                return m_StateC.ActiveTool;
            return UserBuildTool.None;
        }

        /// <summary>
        /// Try to get a tile index by raycasting to a tile. 
        /// </summary>
        private int RaycastTileIndex(SimWorldState world, SimGridState grid) {
            // do a raycast
            // TODO: only raycast if the mouse has moved significantly since last placed tile?
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            // TODO: 100 units a reasonable max raycast distance?
            if (Physics.Raycast(mouseRay, out RaycastHit hit, 100f)) {
                if (!hit.collider) return CODE_INVALID;
                HexVector vec = HexVector.FromWorld(hit.collider.transform.position, world.WorldSpace);
                if (vec.Equals(m_StateC.VecPrev)) {
                    // same as last
                    return CODE_UNCHANGED;
                }
                int i = grid.HexSize.FastPosToIndex(vec);
                Log.Msg("[UserBuildingSystem] New raycast hit Tile {0}", i);

                // TODO: check if valid neighbor (otherwise try to draw line between current and previous, or wait for return)

                m_StateC.VecPrev = vec;
                return i;
            }
            else {
                Log.Msg("[UserBuildingSystem] Raycast missed.");
                return CODE_INVALID;
            }
        }

        /// <summary>
        /// Attempt to place tile on given tile index using active tool
        /// </summary>
        private void TryBuildTile(SimGridState grid, RoadNetwork network, UserBuildTool activeTool, int tileIndex) {
            if (tileIndex == CODE_INVALID) {
                Log.Msg("[UserBuildingSystem] Invalid build location: tile {0} out of bounds", tileIndex);
                // cancel in-progress road 
                CancelRoad(grid, network);
                return;
            }
            if (tileIndex == CODE_UNCHANGED) {
                // player has not moved to a new tile yet, and has not stopped applying the build tool
                return;
            }
            if ((grid.Terrain.Info[tileIndex].Flags & TerrainFlags.NonBuildable) != 0) {
                Log.Msg("[UserBuildingSystem] Invalid build location: tile {0} unbuildable", tileIndex);
                // cancel in-progress road 
                CancelRoad(grid, network);
                return;
            }
            switch (activeTool) {
                // TODO: Add building costs

                case UserBuildTool.Destroy:
                    // TODO: Add road removal
                    break;
                case UserBuildTool.Road:
                    TryBuildRoad(grid, network, tileIndex);
                    break;
                default:
                    break;
            }
        }

        #region Road Building

        private void TryBuildRoad(SimGridState grid, RoadNetwork network, int tileIndex) {
            if (m_StateC.RoadToolState.PrevTileIndex == -1) {
                // Start building road (this would be the first road piece)

                // Check if a valid start, (ResourceSupplier, ResourceRequester, or Road)
                if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsRoadAnchor) != 0) {
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
                if (!IsContinuous(grid.HexSize, m_StateC.RoadToolState.PrevTileIndex, tileIndex)) {
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
                        if ((network.Roads.Info[tileIndex].Flags & RoadFlags.IsRoadAnchor) != 0) {
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
                            CancelRoad(grid, network);
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
                        TileDirection prevDir = grid.HexSize.InvertDir(currDir); // to stage into prev road

                        // For curr road, add a staging mask that gets merged into flow mask upon successful road build
                        RoadUtility.StageRoad(network, grid, tileIndex, new TileDirection[] { currDir });
                        // For prev road, add a staging mask that gets merged into flow mask upon successful road build
                        RoadUtility.StageRoad(network, grid, prevTileIndex, new TileDirection[] { prevDir });
                        break;
                    }
                }
            }

            m_StateC.RoadToolState.PrevTileIndex = tileIndex;

            // add staging visuals
            SetStagingRenderer(tileIndex, true);

            // RoadUtility.AddRoadImmediate(Game.SharedState.Get<RoadNetwork>(), grid, tileIndex); // temp debug
        }

        private bool IsContinuous(HexGridSize hexSize, int prevIndex, int currIndex) {
            HexVector currPos = hexSize.FastIndexToPos(currIndex);
            for (TileDirection dir = (TileDirection)1; dir < TileDirection.COUNT; dir++) {
                HexVector adjPos = HexVector.Offset(currPos, dir);
                if (!hexSize.IsValidPos(adjPos)) {
                    continue;
                }
                int adjIdx = hexSize.PosToIndex(adjPos);
                if (adjIdx == prevIndex) {
                    // prev tile found in adj neighbors
                    return true;
                }
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

        private void FinalizeRoad(SimGridState grid, RoadNetwork network, int tileIndex, bool isEndpoint) {
            RoadUtility.FinalizeRoad(network, grid, tileIndex, isEndpoint);

            // remove staging visuals
            SetStagingRenderer(tileIndex, false);
        }

        private void SetStagingRenderer(int tileIndex, bool isStaging) {
            if (isStaging) {
                TileEffectRendering.SetMaterial(m_StateD.Tiles[tileIndex], m_stagingMaterial);
            }
            else {
                TileEffectRendering.RestoreDefaultMaterial(m_StateD.Tiles[tileIndex]);
            }
        }

        /// <summary>
        /// Finalizes the in-progress road
        /// Triggered when Player releases the button, or Player drags the road into a road termination point (supplier, another road, etc)
        /// </summary>
        private bool TryFinishRoad(SimGridState grid, RoadNetwork network) {
            // Check if road can be purchased
            if (m_StateC.RoadToolState.TracedTileIdxs.Count < 3) {
                // cannot connect a single tile, or only two tiles
                return false;
            }

            // TODO: try to purchase road
            bool purchaseSuccessful = true;

            /*
            // try to purchase road
            Debug.Log("[RoadMgr] trying to purchase road");

            ShopMgr.Instance.TryPurchaseRoad(m_tracedTiles.Count)
            if () {
                Debug.Log("[RoadMgr] Finalizing road");
                // save road in mgr and connected nodes
                FinalizeRoad(m_tracedTiles, m_stagedSegments);

                return;
            }
            else {
                // clear road
                Debug.Log("[InteractMgr] shop failure");
                return;
            }
            */

            if (purchaseSuccessful) {
                Debug.Log("[StagingRoad] Finalizing road...");

                // Merge staged masks into flow masks
                for (int i = 0; i < m_StateC.RoadToolState.TracedTileIdxs.Count; i++) {
                    bool isEndpoint = i == 0 || i == m_StateC.RoadToolState.TracedTileIdxs.Count - 1;
                    FinalizeRoad(grid, network, m_StateC.RoadToolState.TracedTileIdxs[i], isEndpoint);
                }

                // TODO: create physical roads

                // TODO: calculate orientations of roads

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
        }

        #endregion // Road Building
    }
}