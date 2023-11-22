using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Rendering;
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
        public int Cost;                    // The price to build / remove. Negative if the player receives money back
        public int TileIndex;
        public MaterialSwap MatSwap;        // The component controlling the material swap

        public ActionCommit(BuildingType bType, ActionType aType, int cost, int tileIndex, MaterialSwap matSwap)
        {
            BuildType = bType;
            ActionType = aType;
            Cost = cost;
            TileIndex = tileIndex;
            MatSwap = matSwap;
        }
    }

    public struct CommitChain
    {
        public RingBuffer<ActionCommit> Chain;
    }

    public class BlueprintState : SharedStateComponent, IScenePreload
    {
        public RingBuffer<CommitChain> Commits;
        public bool NewBuildConfirmed;

        private UIBlueprint m_BlueprintUI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            m_BlueprintUI = FindAnyObjectByType<UIBlueprint>(FindObjectsInactive.Include);
            Commits = new RingBuffer<CommitChain>(8);
            return null;
        }

        public void UpdateRunningCostDisplay(int runningCost, int deltaCost, long playerFunds)
        {
            m_BlueprintUI.UpdateTotalCost(runningCost, deltaCost, playerFunds);
        }
    }

    public static class BlueprintUtility
    {
        /// <summary>
        /// Adds to the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        /// <param name="commit"></param>
        public static void CommitBuild(BlueprintState blueprintState, ActionCommit commit)
        {
            CommitChain newChain = new CommitChain();
            newChain.Chain = new RingBuffer<ActionCommit>(1);
            newChain.Chain.PushBack(commit);

            blueprintState.Commits.PushBack(newChain);
        }

        /// <summary>
        /// Undoes the last commit in the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void Undo(BlueprintState blueprintState)
        {
            CommitChain prevChain = blueprintState.Commits.PopBack();

            foreach(ActionCommit commit in prevChain.Chain)
            {
                // TODO: process undo (unbuild, restore flags, modify funds, etc.)
            }

            prevChain.Chain.Clear();
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
                    // change material
                    commitAction.MatSwap.ResetMaterial();

                    // TODO: handle multiple materials per 1 commit (e.g. roads)
                }
            }

            blueprintState.Commits.Clear();

            // Exit build state
        }
    }
}