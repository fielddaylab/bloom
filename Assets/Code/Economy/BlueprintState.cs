using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.Rendering;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.UI;
using Zavala.World;

namespace Zavala.Economy
{

    public enum ActionType : byte
    {
        Build,
        Destroy
    }

    // Commit Chain is composed of BuildCommits. Roads use CommitChains. Destroy sequence uses a CommitChain. Other builds are a CommitChain of length 1.

    /// <summary>
    /// Struct for history of build queue during blueprint mode
    /// </summary>
    public struct ActionCommit
    {
        public BuildingType BuildType;
        public ActionType ActionType;
        public int Cost;                                 // The price to build / remove. Negative if the player receives money back
        public int TileIndex;
        public TileAdjacencyMask InleadingRemoved;     // Inleading road dirs removed when this was destroyed
        public GameObject BuiltObj;                      // The physical object built
        public BuildingPreview Previewer;
        public RoadFlags RoadFlagSnapshot;
        public TerrainFlags TerrainFlagSnapshot;
        public TileAdjacencyMask FlowMaskSnapshot;
        public bool WasPending;

        public ActionCommit(BuildingType bType, ActionType aType, int cost, int tileIndex, TileAdjacencyMask inleadingRemoved, GameObject builtObj, RoadFlags rFlags, TerrainFlags tFlags, TileAdjacencyMask flowSnapshot, bool wasPending)
        {
            BuildType = bType;
            ActionType = aType;
            Cost = cost;
            TileIndex = tileIndex;
            InleadingRemoved = inleadingRemoved;
            BuiltObj = builtObj;
            Previewer = builtObj != null ? builtObj.GetComponent<BuildingPreview>() : null;
            RoadFlagSnapshot = rFlags;
            TerrainFlagSnapshot = tFlags;
            FlowMaskSnapshot = flowSnapshot;
            WasPending = wasPending;
        }
    }

    public struct CommitChain
    {
        public RingBuffer<ActionCommit> Chain;
    }

    public class BlueprintState : SharedStateComponent, IScenePreload, IRegistrationCallbacks
    {
        [NonSerialized] public bool IsActive;           // Whether blueprint mode is engaged
        [NonSerialized] public RingBuffer<CommitChain> Commits;
        [NonSerialized] public CommitChain DestroyChain; // The chain that gets built during Destroy Mode
        [NonSerialized] public ActionType CommandState; // Build mode or Destroy mode

        [NonSerialized] public DynamicMeshFilter OverlayFilter;
        [NonSerialized] public MeshRenderer OverlayRenderer;
        [NonSerialized] public MeshData16<TileVertexFormat> OverlayData;
        [NonSerialized] public RingBuffer<int> OverlayIdxs; // tile indices of non-buildables
        [NonSerialized] public RingBuffer<int> OverlayAdjIdxs; // temp list for gathering tiles adjacent to sources/destinations
        [NonSerialized] public ushort LockedRegion;

        #region Inspector

        // TODO: Consolidate this with UserBuildingSystem material of the same name
        public Material m_ValidHoloMaterial; // material applied to buildings being staged

        [SerializeField] private GameObject m_OverlayPrefab;

        #endregion // Inspector

        #region Triggers

        [HideInInspector] public bool NewBuildConfirmed;
        [HideInInspector] public bool NumBuildCommitsChanged;
        [HideInInspector] public bool NumDestroyActionsChanged;
        [HideInInspector] public bool StartBlueprintMode;
        [HideInInspector] public bool ExitedBlueprintMode;
        [HideInInspector] public bool PolicyTypeUnlocked;
        [HideInInspector] public bool UndoClickedBuild;
        [HideInInspector] public bool UndoClickedDestroy;
        [HideInInspector] public bool DestroyModeClicked;
        [HideInInspector] public bool NewDestroyConfirmed;
        [HideInInspector] public bool CanceledDestroyMode;

        #endregion // Triggers

        [HideInInspector] public UIBlueprint UI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            UI = Game.Gui.GetShared<UIBlueprint>();
            Commits = new RingBuffer<CommitChain>(8, RingBufferMode.Expand);
            CommandState = ActionType.Build;

            return null;
        }

        public void OnRegister()
        {
            OverlayFilter = Instantiate(m_OverlayPrefab, Vector3.zero, Quaternion.identity).GetComponent<DynamicMeshFilter>();
            OverlayRenderer = OverlayFilter.GetComponent<MeshRenderer>();
            OverlayData = new MeshData16<TileVertexFormat>(256);
        }

        public void OnDeregister()
        {

        }
    }

    public static class BlueprintUtility
    {
        private const float cosDim = 0.5f; // 1/2
        private const float sinDim = 0.8660254038f; // sqrt(3)/2

        /// <summary>
        /// Adds one building to the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        /// <param name="commit"></param>
        public static void CommitBuild(BlueprintState blueprintState, ActionCommit commit)
        {
            CommitChain newChain = new CommitChain();
            newChain.Chain = new RingBuffer<ActionCommit>(1);
            newChain.Chain.PushBack(commit);

            blueprintState.Commits.PushBack(newChain);
            blueprintState.NumBuildCommitsChanged = true;
        }

        /// <summary>
        /// Adds one action to the destroy stack
        /// </summary>
        /// <param name="blueprintState"></param>
        /// <param name="commit"></param>
        public static void CommitDestroyAction(BlueprintState blueprintState, ActionCommit commit)
        {
            blueprintState.DestroyChain.Chain.PushBack(commit);
            blueprintState.NumDestroyActionsChanged = true;
        }

        /// <summary>
        /// Adds a chain of structures to the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void CommitChain(BlueprintState blueprintState, CommitChain inChain)
        {
            blueprintState.Commits.PushBack(inChain);
            blueprintState.NumBuildCommitsChanged = true;
        }

        /// <summary>
        /// Undoes the last commit in the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void Undo(BlueprintState blueprintState, ShopState shopState, SimGridState grid, RoadNetwork network)
        {
            CommitChain prevChain = blueprintState.Commits.PopBack();

            foreach (ActionCommit commit in prevChain.Chain)
            {
                // Process undo (unbuild, restore flags, modify funds, etc.)
                switch (commit.ActionType)
                {
                    case ActionType.Build:
                        // Refund the cost
                        ShopUtility.EnqueueCost(shopState, -commit.Cost);

                        // Remove the building
                        SimDataUtility.DestroyBuildingFromUndo(grid, network, commit.BuiltObj, commit.TileIndex, commit.BuildType);

                        // Restore flags and flow masks
                        SimDataUtility.RestoreSnapshot(network, grid, commit.TileIndex, commit.RoadFlagSnapshot, commit.TerrainFlagSnapshot, commit.FlowMaskSnapshot);

                        if (commit.Previewer != null) {
                            commit.Previewer.Cancel();
                        }

                        break;
                    case ActionType.Destroy:
                        UndoDestroy(blueprintState, shopState, grid, network, commit);

                        break;
                    default:
                        break;
                }

                if (!commit.FlowMaskSnapshot.IsEmpty)
                {
                    RoadUtility.UpdateRoadVisuals(network, commit.TileIndex);
                }
            }

            blueprintState.NumBuildCommitsChanged = true;
        }

        private static void UndoDestroy(BlueprintState blueprintState, ShopState shopState, SimGridState grid, RoadNetwork network, ActionCommit prevCommit)
        {
            // Re-apply the cost for pending building, or refund the cost of demolition for existing building 
            ShopUtility.EnqueueCost(shopState, -prevCommit.Cost);

            // Re-add the building
            switch(prevCommit.BuildType)
            {
                case BuildingType.Road:
                case BuildingType.Storage:
                case BuildingType.Digester:
                    SimDataUtility.BuildOnTileFromUndo(grid, prevCommit.BuildType, prevCommit.TileIndex, blueprintState.m_ValidHoloMaterial, prevCommit.WasPending);
                    break;
                default:
                    break;
            }

            // Restore flags and flow masks
            SimDataUtility.RestoreSnapshot(network, grid, prevCommit.TileIndex, prevCommit.RoadFlagSnapshot, prevCommit.TerrainFlagSnapshot, prevCommit.FlowMaskSnapshot);
            RoadUtility.UpdateRoadVisuals(network, prevCommit.TileIndex);


            // Restore inleading flow masks
            HexVector currPos = grid.HexSize.FastIndexToPos(prevCommit.TileIndex);
            foreach (TileDirection dir in prevCommit.InleadingRemoved)
            {
                HexVector adjPos = HexVector.Offset(currPos, dir);
                if (!grid.HexSize.IsValidPos(adjPos))
                {
                    continue;
                }
                int adjIdx = grid.HexSize.FastPosToIndex(adjPos);

                ref RoadTileInfo adjTileInfo = ref network.Roads.Info[adjIdx];

                TileDirection currDir = dir;
                TileDirection adjDir = currDir.Reverse();

                adjTileInfo.FlowMask[adjDir] = true;

                // Update prev road rendering
                RoadUtility.UpdateRoadVisuals(network, adjIdx);
            }

            blueprintState.NumDestroyActionsChanged = true;
        }

        /// <summary>
        /// Finalizes all builds
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void ConfirmBuild(BlueprintState blueprintState, ShopState shop, uint currRegion)
        {
            if (!ShopUtility.TryPurchaseAll(shop.RunningCost, currRegion))
            {
                // TODO: handle faulty accounting
                return;
            }

            ShopUtility.ResetRunningCost(shop);

            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            // Process commits
            foreach (var commitChain in blueprintState.Commits)
            {
                foreach(var commitAction in commitChain.Chain)
                {
                    BuildingType builtType = commitAction.BuildType;
                    if (builtType == BuildingType.Digester || builtType == BuildingType.Storage) {
                        using (TempVarTable varTable = TempVarTable.Alloc()) {
                            varTable.Set("buildingType", commitAction.BuildType.ToString());
                            ScriptUtility.Trigger(GameTriggers.PlayerBuiltBuilding, varTable);
                        }
                    }
                    // Process any confirm-time things
                    if (builtType == BuildingType.Road)
                    {
                        RoadVisualUtility.ClearBPMask(network, commitAction.TileIndex);
                        RoadUtility.UpdateRoadVisuals(network, commitAction.TileIndex);
                    }

                    if (commitAction.Previewer != null) {
                        commitAction.Previewer.Apply();
                    }
                }
            }

            ClearBuildCommits(blueprintState);

            network.UpdateNeeded = true;

            MarketData market = Game.SharedState.Get<MarketData>();
            market.UpdatePrioritiesNow = true;

            // Exit build state
            blueprintState.UI.OnBuildConfirmClicked();
        }

        /// <summary>
        /// Finalizes all builds
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void ConfirmDestroy(BlueprintState blueprintState, ShopState shop, SimGridState grid, BuildToolState toolState, uint currRegion)
        {
            // Add pending destroy commits to the overall build mode chain
            CommitChain newChain = new CommitChain();
            newChain.Chain = new RingBuffer<ActionCommit>(blueprintState.DestroyChain.Chain.Count);
            blueprintState.DestroyChain.Chain.CopyTo(newChain.Chain);

            CommitChain(blueprintState, newChain);

            ClearDestroyCommits(blueprintState);

            blueprintState.CommandState = ActionType.Build;
            BuildToolUtility.SetTool(toolState, UserBuildTool.None);

            // Exit Destroy state
            blueprintState.UI.OnDestroyConfirmClicked();
        }

        public static void UpdateRunningCostDisplay(BlueprintState blueprintState, int runningCost, int deltaCost, long playerFunds)
        {
            blueprintState.UI.UpdateTotalCost(runningCost, deltaCost, playerFunds, blueprintState.Commits.Count);
        }

        public static void OnStartBlueprintMode(BlueprintState blueprintState)
        {
            blueprintState.UI.OnStartBlueprintMode();
        }

        public static void OnNumBuildCommitsChanged(BlueprintState blueprintState)
        {
            blueprintState.UI.OnNumBuildCommitsChanged(blueprintState.Commits.Count);
        }

        public static void OnNumDestroyActionsChanged(BlueprintState blueprintState)
        {
            blueprintState.UI.OnNumDestroyActionsChanged(blueprintState.DestroyChain.Chain.Count);
        }

        public static void OnExitedBlueprintMode(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            blueprintState.UI.OnExitedBlueprintMode();
            CancelPendingBuildCommits(blueprintState, shop, grid);
        }

        public static void OnUndoClickedBuild(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            Undo(blueprintState, shop, grid, network);
        }

        public static void OnUndoClickedDestroy(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            UndoDestroy(blueprintState, shop, grid, network, blueprintState.DestroyChain.Chain.PopBack());
        }

        public static void OnDestroyModeClicked(BlueprintState blueprintState, ShopState shop, SimGridState grid, BuildToolState buildState)
        {
            blueprintState.DestroyChain = new CommitChain();
            blueprintState.DestroyChain.Chain = new RingBuffer<ActionCommit>(8, RingBufferMode.Expand);

            blueprintState.CommandState = ActionType.Destroy;
            BuildToolUtility.SetTool(buildState, UserBuildTool.Destroy);
            blueprintState.UI.OnDestroyModeClicked();
        }

        public static void OnCanceledDestroyMode(BlueprintState blueprintState, ShopState shop, SimGridState grid, BuildToolState buildState)
        {
            CancelPendingDestroyCommits(blueprintState, shop, grid);
            blueprintState.DestroyChain.Chain.Clear();
            blueprintState.CommandState = ActionType.Build;
            BuildToolUtility.SetTool(buildState, UserBuildTool.None);
            blueprintState.UI.OnCanceledDestroyMode();
        }

        public static void OnBuildToolSelected(BlueprintState blueprintState)
        {
            blueprintState.UI.OnBuildToolSelected();
        }

        public static void OnBuildToolDeselected(BlueprintState blueprintState)
        {
            if (blueprintState.IsActive)
            {
                blueprintState.UI.OnBuildToolDeselected();
            }
        }

        public static void OnMarketTickAdvanced(BlueprintState blueprintState, SimGridState grid, MarketData market)
        {
            if (!blueprintState.IsActive)
            {
                // Only add popups when top bar boxes are visible
                blueprintState.UI.OnMarketTickAdvanced(market, grid);
            }
        }

        #region Mesh Overlay

        public static void RegenerateOverlayMesh(BlueprintState bpState, SimGridState grid, SimWorldState world, RoadNetwork network, BuildToolState btState)
        {
            bpState.OverlayRenderer.enabled = true;
            bpState.OverlayData.Clear();

            // Render the mesh for all blocked tiles in the current region
            foreach(int index in grid.Regions[(int) grid.CurrRegionIndex].GridArea)
            {
                if (btState.BlockedTileBuffer[index] == 1 && grid.CurrRegionIndex == grid.Terrain.Info[index].RegionIndex)
                {
                    AddTileToOverlayMesh(grid, world, index, ref bpState.OverlayData);
                }
            }

            bpState.OverlayFilter.Upload(bpState.OverlayData);
        }

        public static void HideOverlayMesh(BlueprintState bpState)
        {
            bpState.OverlayRenderer.enabled = false;
        }

        #endregion // Mesh Overlay

        #region Helpers

        private static void AddTileToOverlayMesh(SimGridState grid, SimWorldState world, int tileIndex, ref MeshData16<TileVertexFormat> overlayData)
        {
            // Generate the mesh overlay
            Vector3 heightOffset = new Vector3(0, 0, 0);
            Vector3 centerPos = HexVector.ToWorld(tileIndex, grid.Terrain.Info[tileIndex].Height, world.WorldSpace) + heightOffset;

            TileRendering.GenerateTileMeshData(centerPos, 1, Color.white, overlayData);
        }

        private static void CancelPendingBuildCommits(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            while (blueprintState.Commits.Count > 0)
            {
                Undo(blueprintState, shop, grid, network);
            }
            ClearBuildCommits(blueprintState);
        }

        private static void ClearBuildCommits(BlueprintState blueprintState)
        {
            blueprintState.Commits.Clear();
            blueprintState.NumBuildCommitsChanged = true;
        }

        private static void CancelPendingDestroyCommits(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            while (blueprintState.DestroyChain.Chain.Count > 0)
            {
                UndoDestroy(blueprintState, shop, grid, network, blueprintState.DestroyChain.Chain.PopBack());
            }
            ClearDestroyCommits(blueprintState);
        }

        private static void ClearDestroyCommits(BlueprintState blueprintState)
        {
            blueprintState.DestroyChain.Chain.Clear();
            blueprintState.NumDestroyActionsChanged = true;
        }

        #endregion // Helpers
    }
}