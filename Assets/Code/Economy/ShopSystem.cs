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
            shopState.ShopUI.NetText.text = /*"Net: " +*/ budgetData.BudgetsPerRegion[gridState.CurrRegionIndex].Net.ToString();
            shopState.ShopUI.RefreshCostChecks((int)budgetData.BudgetsPerRegion[gridState.CurrRegionIndex].Net);
        }

        public static long PriceLookup(UserBuildTool building) {
            switch (building) {
                case UserBuildTool.Road:
                    return 10;
                case UserBuildTool.Storage:
                    return 20;
                case UserBuildTool.Digester:
                    return 30;
                default:
                    return 0;
            }
        }
    }

}