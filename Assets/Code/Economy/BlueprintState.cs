using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Rendering;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Economy
{

    // Commit Chain composed of BuildCommits. Roads use CommitChains. Destroy sequence uses a CommitChain. Other builds are a CommitChain of length 1.

    public enum ActionType : byte
    {
        Build,
        Destroy
    }

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

        public ActionCommit(BuildingType bType, ActionType aType, int cost, int tileIndex, List<TileDirection> inleadingRemoved, GameObject builtObj, RoadFlags rFlags, TerrainFlags tFlags)
        {
            BuildType = bType;
            ActionType = aType;
            Cost = cost;
            TileIndex = tileIndex;
            InleadingRemoved = inleadingRemoved;
            BuiltObj = builtObj;
            RoadFlagSnapshot = rFlags;
            TerrainFlagSnapshot = tFlags;
        }
    }

    public struct CommitChain
    {
        public RingBuffer<ActionCommit> Chain;
    }

    public class BlueprintState : SharedStateComponent, IScenePreload
    {
        public RingBuffer<CommitChain> Commits;

        #region Triggers

        public bool NewBuildConfirmed;
        public bool NumCommitsChanged;
        public bool ExitedBlueprintMode;
        public bool UndoClickedBuild;

        #endregion // Triggers

        public UIBlueprint UI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            UI = FindAnyObjectByType<UIBlueprint>(FindObjectsInactive.Include);
            Commits = new RingBuffer<CommitChain>(8);
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
            blueprintState.NumCommitsChanged = true;
        }

        /// <summary>
        /// Adds a chain of structures to the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void CommitBuildChain(BlueprintState blueprintState, CommitChain inChain)
        {
            blueprintState.Commits.PushBack(inChain);
            blueprintState.NumCommitsChanged = true;
        }

        /// <summary>
        /// Undoes the last commit in the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void Undo(BlueprintState blueprintState, ShopState shopState, SimGridState grid)
        {
            RoadNetwork network = Game.SharedState.Get<RoadNetwork>();

            CommitChain prevChain = blueprintState.Commits.PopBack();

            foreach (ActionCommit commit in prevChain.Chain)
            {
                // TODO: Process undo (unbuild, restore flags, modify funds, etc.)
                switch (commit.ActionType)
                {
                    case ActionType.Build:
                        // Refund the cost
                        ShopUtility.EnqueueCost(shopState, -commit.Cost);

                        // Remove the building
                        SimDataUtility.DestroyBuildingDirect(grid, commit.BuiltObj, commit.TileIndex, commit.BuildType);

                        // Restore flags
                        SimDataUtility.RestoreFlagSnapshot(network, grid, commit.TileIndex, commit.RoadFlagSnapshot, commit.TerrainFlagSnapshot);

                        break;
                    case ActionType.Destroy:
                        // If was staged, re-add the cost
                        // Else apply cost of removal

                        // Remove the building

                        // Update the flags

                        break;
                    default:
                        break;
                }
            }

            blueprintState.NumCommitsChanged = true;
        }

        /// <summary>
        /// FInalizes all builds
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

            ClearCommits(blueprintState);

            // Exit build state
        }

        public static void UpdateRunningCostDisplay(BlueprintState blueprintState, int runningCost, int deltaCost, long playerFunds)
        {
            blueprintState.UI.UpdateTotalCost(runningCost, deltaCost, playerFunds);
        }

        public static void OnNumCommitsChanged(BlueprintState blueprintState)
        {
            blueprintState.UI.OnNumCommitsChanged(blueprintState.Commits.Count);
        }

        public static void OnExitedBlueprintMode(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            CancelPendingCommits(blueprintState, shop, grid);
        }

        public static void OnUndoClicked(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            Undo(blueprintState, shop, grid);
        }

        #region Helpers

        private static void CancelPendingCommits(BlueprintState blueprintState, ShopState shop, SimGridState grid)
        {
            while (blueprintState.Commits.Count > 0)
            {
                Undo(blueprintState, shop, grid);
            }
            ClearCommits(blueprintState);
        }

        private static void ClearCommits(BlueprintState blueprintState)
        {
            blueprintState.Commits.Clear();
            blueprintState.NumCommitsChanged = true;
        }

        #endregion // Helpers
    }
}