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
    /// <summary>
    /// Struct for history of build queue during blueprint mode
    /// </summary>
    public struct BuildCommit
    {
        public int Cost;
        public int TileIndex;
        public MaterialSwap MatSwap; // The component controlling the material swap

        public BuildCommit(int cost, int tileIndex, MaterialSwap matSwap)
        {
            Cost = cost;
            TileIndex = tileIndex;
            MatSwap = matSwap;
        }
    }

    public class BlueprintState : SharedStateComponent, IScenePreload
    {
        public RingBuffer<BuildCommit> Commits;
        public bool NewBuildConfirmed;

        private UIBlueprint m_BlueprintUI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            m_BlueprintUI = FindAnyObjectByType<UIBlueprint>(FindObjectsInactive.Include);
            Commits = new RingBuffer<BuildCommit>(8);
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
        public static void CommitBuild(BlueprintState blueprintState, BuildCommit commit)
        {
            blueprintState.Commits.PushBack(commit);
        }

        /// <summary>
        /// Undoes the last commit in the build stack
        /// </summary>
        /// <param name="blueprintState"></param>
        public static void Undo(BlueprintState blueprintState)
        {
            BuildCommit prevCommit = blueprintState.Commits.PopBack();

            // TODO: Unbuild the building
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
            foreach (var commit in blueprintState.Commits)
            {
                // change material
                commit.MatSwap.ResetMaterial();

                // TODO: handle multiple materials per 1 commit (e.g. roads)
            }

            blueprintState.Commits.Clear();

            // Exit build state
        }
    }
}