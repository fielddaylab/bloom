using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Building;
using Zavala.Sim;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 0)]
    public sealed class ShopSystem : SharedStateSystemBehaviour<BudgetData, ShopState, SimGridState>, IRegistrationCallbacks
    {
        public override void ProcessWork(float deltaTime) {
            if (m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Updated /* || region changed*/) {
                // Update UI to display budgetData for current region
                ShopUtility.RefreshShop(m_StateA, m_StateB, m_StateC);
                // TODO: may eventually need more control over when budgets are marked as updated if other systems also use that flag
                m_StateA.BudgetsPerRegion[m_StateC.CurrRegionIndex].Updated = false;
            }
        }

        public void OnRegister() {
            ShopUtility.RefreshShop(m_StateA, m_StateB, m_StateC);
        }

        public void OnDeregister() {
        }
    }


    static public class ShopUtility { 
        static public void RefreshShop(BudgetData budgetData, ShopState shopState, SimGridState gridState) {
            uint idx = gridState.CurrRegionIndex;
            shopState.ShopUI.NetText.text = /*"Net: " +*/ budgetData.BudgetsPerRegion[idx].Net.ToString();
            shopState.ShopUI.RefreshCostChecks((int)budgetData.BudgetsPerRegion[idx].Net);
            shopState.ShopUI.RegionName.text = ((RegionId)idx).ToString();
        }

        public static long PriceLookup(UserBuildTool building) {
            switch (building) {
                case UserBuildTool.Road:
                    return 5;
                case UserBuildTool.Storage:
                    return 100;
                case UserBuildTool.Digester:
                    return 200;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Check if the current region can afford to purchase the given buildings, and deduct the price if so.
        /// </summary>
        /// <param name="currTool">Building tool to purchase</param>
        /// <param name="currentRegion">Region whose budget to test</param>
        /// <param name="num">Number of buildings purchased (used only for roads)</param>
        /// <returns></returns>
        public static bool TryPurchaseBuild(UserBuildTool currTool, uint currentRegion, int num) {
            BudgetData budgetData = Game.SharedState.Get<BudgetData>();
            long price = ShopUtility.PriceLookup(currTool) * num;
            bool purchaseSuccessful = BudgetUtility.TrySpendBudget(budgetData, price, currentRegion);
            return purchaseSuccessful;
        }
    }

}