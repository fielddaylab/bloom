using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Actors;
using Zavala.Building;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 0)]
    public sealed class ShopSystem : SharedStateSystemBehaviour<BudgetData, ShopState, SimGridState, BlueprintState>, IRegistrationCallbacks
    {
        public override void ProcessWork(float deltaTime) {
            if (m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Updated /* || region changed*/) {
                // Update UI to display budgetData for current region
                ShopUtility.RefreshShop(m_StateA, m_StateB, m_StateC);
                // TODO: may eventually need more control over when budgets are marked as updated if other systems also use that flag
                m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Updated = false;
            }

            // process running tally queue
            while (m_StateB.CostQueue.Count > 0)
            {
                int deltaCost = m_StateB.CostQueue.PopFront();
                ShopUtility.ModifyRunningCost(m_StateB, deltaCost);
                BlueprintUtility.UpdateRunningCostDisplay(m_StateD, m_StateB.RunningCost, deltaCost, m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Net);
            }

            if (m_StateB.ManualUpdateRequested)
            {
                BlueprintUtility.UpdateRunningCostDisplay(m_StateD, m_StateB.RunningCost, 0, m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Net);
                m_StateB.ManualUpdateRequested = false;
            }
        }

        public void OnRegister() {
            ShopUtility.RefreshShop(m_StateA, m_StateB, m_StateC);
        }

        public void OnDeregister() {
        }
    }


    static public class ShopUtility {
        private const int ROAD_COST = 5;
        private const int STORAGE_COST = 100;
        private const int DIGESTER_COST = 200;

        static public void RefreshShop(BudgetData budgetData, ShopState shopState, SimGridState gridState) {
            uint idx = gridState.CurrRegionIndex;
            shopState.ShopUI.NetText.text = /*"Net: " +*/ budgetData.BudgetsPerRegion[idx].Net.ToString();
            shopState.ShopUI.RefreshCostChecks((int)budgetData.BudgetsPerRegion[idx].Net);
        }

        public static int PriceLookup(UserBuildTool building) {
            switch (building) {
                case UserBuildTool.Road:
                    return ROAD_COST;
                case UserBuildTool.Storage:
                    return STORAGE_COST;
                case UserBuildTool.Digester:
                    return DIGESTER_COST;
                default:
                    return 0;
            }
        }

        public static int PriceLookup(BuildingType building)
        {
            switch (building)
            {
                case BuildingType.Road:
                    return ROAD_COST;
                case BuildingType.Storage:
                    return STORAGE_COST;
                case BuildingType.Digester:
                    return DIGESTER_COST;
                default:
                    return 0;
            }
        }

        [LeafMember("UnlockShopItem")]
        public static void UnlockTool(UserBuildTool tool) {
            ShopState shop = Game.SharedState.Get<ShopState>();
            ShopButtonHub hub = shop.ShopUI.GetBtnHub();
            ShopItemButton btn = hub.GetShopItemBtn(tool);
            hub.SetShopItemBtnUnlocked(btn, true);
        }

        /// <summary>
        /// Check if the current region can afford to purchase the given buildings
        /// </summary>
        /// <param name="currTool">Building tool to purchase</param>
        /// <param name="currentRegion">Region whose budget to test</param>
        /// <param name="num">Number of buildings purchased (used only for roads)</param>
        /// <returns></returns>
        public static bool CanPurchaseBuild(UserBuildTool currTool, uint currentRegion, int num, int runningCost, out int price) {
            BudgetData budgetData = Game.SharedState.Get<BudgetData>();
            price = ShopUtility.PriceLookup(currTool) * num;

            bool purchaseSuccessful = BudgetUtility.CanSpendBudget(budgetData, runningCost + price, currentRegion);
            // bool purchaseSuccessful = BudgetUtility.TrySpendBudget(budgetData, price, currentRegion);
            return purchaseSuccessful;
        }

        /// <summary>
        /// Check if the current region can afford to purchase the given buildings, and deduct the price if so.
        /// </summary>
        /// <param name="currTool">Building tool to purchase</param>
        /// <param name="currentRegion">Region whose budget to test</param>
        /// <param name="num">Number of buildings purchased (used only for roads)</param>
        /// <returns></returns>
        public static bool TryPurchaseBuild(UserBuildTool currTool, uint currentRegion, int num)
        {
            BudgetData budgetData = Game.SharedState.Get<BudgetData>();
            long price = ShopUtility.PriceLookup(currTool) * num;
            bool purchaseSuccessful = BudgetUtility.TrySpendBudget(budgetData, price, currentRegion);
            return purchaseSuccessful;
        }

        /// <summary>
        /// Check if the current region can afford to purchase the given buildings, and deduct the price if so.
        /// </summary>
        /// <param name="currTool">Building tool to purchase</param>
        /// <param name="currentRegion">Region whose budget to test</param>
        /// <param name="num">Number of buildings purchased (used only for roads)</param>
        /// <returns></returns>
        public static bool TryPurchaseAll(int totalCost, uint currentRegion)
        {
            BudgetData budgetData = Game.SharedState.Get<BudgetData>();
            bool purchaseSuccessful = BudgetUtility.TrySpendBudget(budgetData, totalCost, currentRegion);
            return purchaseSuccessful;
        }

        public static void ResetRunningCost(ShopState shop)
        {
            shop.RunningCost = 0;
        }

        public static void ModifyRunningCost(ShopState shop, int deltaCost)
        {
            shop.RunningCost += deltaCost;
            if (shop.RunningCost < 0) {
                Log.Error("[ShopSystem] WARNING: something caused running cost to reach {0}", shop.RunningCost);
                ResetRunningCost(shop);
            }
        }

        public static void EnqueueCost(ShopState shop, int cost)
        {
            shop.CostQueue.PushBack(cost);
        }

    }

}