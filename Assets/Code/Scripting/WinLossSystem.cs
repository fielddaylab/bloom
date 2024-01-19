
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.Scripting {
    public class WinLossSystem : SharedStateSystemBehaviour<WinLossState, SimGridState, RegionUnlockState> {
        public override bool HasWork() {
            if (base.HasWork()) {
                return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
            }
            return false;
        }

        #region Work

        public override void ProcessWork(float deltaTime) {
            if (!m_StateA.CheckTimer) {
                return;
            }
            // if (m_StateA.IgnoreFailure) {
            //     return;
            // }

            int numRegionsToCheck = m_StateC.UnlockCount;

            for (int region = 0; region <= numRegionsToCheck; region++) {
                RegionId currentRegion = m_StateA.EndConditionsPerRegion[region].Region;
                foreach (EndGameConditions cond in m_StateA.EndConditionsPerRegion[region].IndependentEndConditions) {
                    if (EndConditionsMet(cond, (uint)currentRegion)) {
                        TriggerEnding(cond, region);
                        break;
                    }
                }
            }

            m_StateA.CheckTimer = false;
        }

        #endregion


        private bool EndConditionsMet(EndGameConditions cond, uint region) {
            // failPending starts TRUE
            bool endPending = true;

            // each condition sets failPending FALSE if set (!=0) and not met
            EvaluateBudgetBelow(ref endPending, cond.BudgetBelow, region);
            if (cond.CheckFarmsUnconnected) {
                EvaluateFarmsUnconnected(ref endPending, region);
            }
            EvaluateTotalPAbove(ref endPending, cond.TotalPhosphorusAbove, region);
            EvaluateTotalAlgaeAbove(ref endPending, cond.TotalAlgaeAbove, region);
            EvaluateCityFallingFor(ref endPending, cond.CityFallingDurationAbove, region);
            EvaluateRegionAgeAbove(ref endPending, cond.RegionAgeAbove, region);
            return endPending;
        }

        private void TriggerEnding(EndGameConditions cond, int regionIndex) {
            if (cond.Type == EndType.Succeeded) {
                Log.Warn("[WinLossSystem] TRIGGERED GAME WIN {0} in Region {1}", cond.Type, regionIndex);
                using (TempVarTable varTable = TempVarTable.Alloc()) {
                    varTable.Set("endType", cond.Type.ToString());
                    varTable.Set("regionIndex", regionIndex);
                    ScriptUtility.Trigger(GameTriggers.GameCompleted, varTable);
                }
                return;
            } 
            Log.Warn("[WinLossSystem] TRIGGERED GAME FAIL {0} in Region {1}", cond.Type, regionIndex);

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("endType", cond.Type.ToString());
                varTable.Set("regionIndex", regionIndex);
                ScriptUtility.Trigger(GameTriggers.GameFailed, varTable);
            }
        }


        #region Conditions

        private void EvaluateBudgetBelow(ref bool triggered, int num, uint region) {
            if (num <= 0) return;

            if (BudgetUtility.CanSpendBudget(Game.SharedState.Get<BudgetData>(), num, region)) {
                triggered = false;
            };
        }
        private void EvaluateFarmsUnconnected(ref bool triggered, uint region) {
            if (m_StateA.FarmsConnectedInRegion[region]) {
                triggered = false;
            }
        }

        private void EvaluateTotalPAbove(ref bool triggered, int num, uint region) {
            if (num <= 0) return;
            long phos = Game.SharedState.Get<SimPhosphorusState>().TotalPPerRegion[region];
            if (phos < num) {
                triggered = false;
            }
        }

        private void EvaluateTotalAlgaeAbove(ref bool triggered, float threshold, uint region) {
            if (threshold <= 0) return;
            float algae = Game.SharedState.Get<SimAlgaeState>().TotalAlgaePerRegion[region];
            if (algae < threshold && Math.Abs(algae - threshold) > 0.1) {
                triggered = false;
            }
        }

        private void EvaluateCityFallingFor(ref bool triggered, int num, uint region) {
            if (num <= 0) return;
            
            if (m_StateA.CityFallingTimersPerRegion[region] < num) {
                triggered = false;
            }
        }

        private void EvaluateRegionAgeAbove(ref bool triggered, int age, uint region) {
            if (age <= 0) return;

            if (m_StateB.Regions[(int)region].Age < age) {
                triggered = false;
            }
        }

        private void EvaluateNodeReached(ref bool triggered, string title) {
            if (title == "" || title == null) return;

            if (!title.Contains("region")) {
                Log.Warn("[WinLossSystem] NodeReached condition: use format region1.nodeTitle. Yours: " + title);
            }
            if (!ScriptUtility.Persistence.SessionViewedNodeIds.Contains(new StringHash32(title))) {
                triggered = false;
            }

        }

        #endregion

    }
}