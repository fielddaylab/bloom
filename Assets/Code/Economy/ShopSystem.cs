using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Sim;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 0)]
    public sealed class ShopSystem : SharedStateSystemBehaviour<BudgetData, ShopState>
    {
        public override void ProcessWork(float deltaTime) {
            // Update UI to display budgetData for current region
            // Get current region
            int currRegionIndex = 0;
            if (m_StateA.BudgetsPerRegion[currRegionIndex].Updated /* || region changed*/) {
                m_StateB.ShopUI.NetText.text = "Net: " + m_StateA.BudgetsPerRegion[currRegionIndex].Net;

                m_StateA.BudgetsPerRegion[currRegionIndex].Updated = false;
            }

            // Handle purchase inputs
        }
    }
}