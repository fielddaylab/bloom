using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Rendering;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.UI;

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
        public List<TileDirection> InleadingRemoved;     // Inleading road dirs removed when this was destroyed
        public GameObject BuiltObj;                      // The physical object built
        public RoadFlags RoadFlagSnapshot;
        public TerrainFlags TerrainFlagSnapshot;
        public TileAdjacencyMask FlowMaskSnapshot;

        public ActionCommit(BuildingType bType, ActionType aType, int cost, int tileIndex, List<TileDirection> inleadingRemoved, GameObject builtObj, RoadFlags rFlags, TerrainFlags tFlags, TileAdjacencyMask flowSnapshot)
        {
            BuildType = bType;
            ActionType = aType;
            Cost = cost;
            TileIndex = tileIndex;
            InleadingRemoved = inleadingRemoved;
            BuiltObj = builtObj;
            RoadFlagSnapshot = rFlags;
            TerrainFlagSnapshot = tFlags;
            FlowMaskSnapshot = flowSnapshot;
        }
    }

    public struct CommitChain
    {
        public RingBuffer<ActionCommit> Chain;
    }

    public class BlueprintState : SharedStateComponent, IScenePreload
    {
        public RingBuffer<CommitChain> Commits;
        public CommitChain DestroyChain; // The chain that gets built during Destroy Mode
        public ActionType CommandState; // Build mode or Destroy mode

        #region Inspector

        // TODO: Consolidate this with UserBuildingSystem material of the same name
        public Material m_ValidHoloMaterial; // material applied to buildings being staged

        #endregion // Inspector

        #region Triggers

        [HideInInspector] public bool NewBuildConfirmed;
        [HideInInspector] public bool NumBuildCommitsChanged;
        [HideInInspector] public bool NumDestroyActionsChanged;
        [HideInInspector] public bool ExitedBlueprintMode;
        [HideInInspector] public bool UndoClickedBuild;
        [HideInInspector] public bool UndoClickedDestroy;
        [HideInInspector] public bool DestroyModeClicked;
        [HideInInspector] public bool NewDestroyConfirmed;
        [HideInInspector] public bool CanceledDestroyMode;

        #endregion // Triggers

        [HideInInspector] public UIBlueprint UI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            UI = FindAnyObjectByType<UIBlueprint>(FindObjectsInactive.Include);
            Commits = new RingBuffer<CommitChain>(8);
            CommandState = ActionType.Build;

            return null;
        }
    }

    public static class BlueprintUtility
    {
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

            // Re-add the building the building
            switch(prevCommit.BuildType)
            {
                case BuildingType.Road:
                case BuildingType.Storage:
                case BuildingType.Digester:
                    SimDataUtility.BuildOnTileFromUndo(grid, prevCommit.BuildType, prevCommit.TileIndex, blueprintState.m_ValidHoloMaterial);
                    break;
                default:
                    break;
            }

            // Restore flags and flow masks
            SimDataUtility.RestoreSnapshot(network, grid, prevCommit.TileIndex, prevCommit.RoadFlagSnapshot, prevCommit.TerrainFlagSnapshot, prevCommit.FlowMaskSnapshot);

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

            // Process commits
            foreach (var commitChain in blueprintState.Commits)
            {
                foreach(var commitAction in commitChain.Chain)
                {
                    // Process any confirm-time things
                }
            }

            ClearBuildCommits(blueprintState);

            // Exit build state
            blueprintState.UI.OnBuildConfirmClicked();
        }

        /// <summary>
        /// Finalizes all builds
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void ConfirmDestroy(BlueprintState blueprintState, ShopState shop, SimGridState grid, uint currRegion)
        {
            // Add pending destroy commits to the overall build mode chain
            CommitChain newChain = new CommitChain();
            newChain.Chain = new RingBuffer<ActionCommit>(blueprintState.DestroyChain.Chain.Count);
            blueprintState.DestroyChain.Chain.CopyTo(newChain.Chain);

            CommitChain(blueprintState, newChain);

            ClearDestroyCommits(blueprintState);

            // Exit Destroy state
            blueprintState.UI.OnDestroyConfirmClicked();
        }

        public static void UpdateRunningCostDisplay(BlueprintState blueprintState, int runningCost, int deltaCost, long playerFunds)
        {
            blueprintState.UI.UpdateTotalCost(runningCost, deltaCost, playerFunds);
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
            blueprintState.DestroyChain.Chain = new RingBuffer<ActionCommit>(8);

            blueprintState.CommandState = ActionType.Destroy;
            buildState.ActiveTool = UserBuildTool.Destroy;
            blueprintState.UI.OnDestroyModeClicked();
        }

        public static void OnCanceledDestroyMode(BlueprintState blueprintState, ShopState shop, SimGridState grid, BuildToolState buildState)
        {
            CancelPendingDestroyCommits(blueprintState, shop, grid);
            blueprintState.DestroyChain.Chain.Clear();
            blueprintState.CommandState = ActionType.Build;
            buildState.ActiveTool = UserBuildTool.None;
            blueprintState.UI.OnCanceledDestroyMode();
        }

        #region Helpers

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