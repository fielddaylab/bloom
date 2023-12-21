
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using System;
using System.Collections.Generic;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.Scripting {
    public class WinLossSystem : SharedStateSystemBehaviour<WinLossState, RegionUnlockState, SimPhosphorusState> {
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

            int numRegionsToCheck = m_StateB.UnlockCount;

            for (int region = 0; region <= numRegionsToCheck; region++) {
                RegionId currentRegion = m_StateA.FailureConditionsPerRegion[region].Region;
                foreach (FailureCondition cond in m_StateA.FailureConditionsPerRegion[region].IndependentFailureConditions) {
                    if (FailConditionMet(cond, (uint)currentRegion)) {
                        SendFailure(cond, region);
                        break;
                    }
                }
            }

            m_StateA.CheckTimer = false;
        }

        #endregion


        private bool FailConditionMet(FailureCondition cond, uint region) {
            // failPending starts TRUE
            bool failPending = true;

            // each condition sets failPending FALSE if set (!=0) and not met
            EvaluateBudgetBelow(ref failPending, cond.BudgetBelow, region);
            if (cond.CheckFarmsUnconnected) {
                EvaluateFarmsUnconnected(ref failPending, region);
            }
            EvaluateTotalPAbove(ref failPending, cond.TotalPhosphorusAbove, region);
            EvaluateTotalAlgaeAbove(ref failPending, cond.TotalAlgaeAbove, region);
            EvaluateCityFallingFor(ref failPending, cond.CityFallingDurationAbove, region);
            return failPending;
        }

        private void SendFailure(FailureCondition cond, int regionIndex) {
            Log.Msg("[WinLossSystem] TRIGGERED GAME FAIL {0} in Region {1}", cond.Type, regionIndex);

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("failureType", cond.Type.ToString());
                varTable.Set("regionIndex", regionIndex);
                ScriptUtility.Trigger(GameTriggers.GameFailed, varTable);
            }

        }


        #region Conditions

        private void EvaluateBudgetBelow(ref bool failed, int num, uint region) {
            if (num <= 0) return;

            if (BudgetUtility.CanSpendBudget(Game.SharedState.Get<BudgetData>(), num, region)) {
                failed = false;
            };
        }
        private void EvaluateFarmsUnconnected(ref bool failed, uint region) {
            //Log.Error("[WinLossState] EvaluateFarmsUnconnected is not implemented. Don't use it yet.");
            if (m_StateA.FarmsConnectedInRegion[region]) {
                failed = false;
            }
            // TODO: use unconnected alerts to evaluate
        }

        private void EvaluateTotalPAbove(ref bool failed, int num, uint region) {
            if (num <= 0) return;
            long phos = Game.SharedState.Get<SimPhosphorusState>().TotalPPerRegion[region];
            if (phos < num) {
                failed = false;
            }
        }

        private void EvaluateTotalAlgaeAbove(ref bool failed, float threshold, uint region) {
            if (threshold <= 0) return;
            float algae = Game.SharedState.Get<SimAlgaeState>().TotalAlgaePerRegion[region];
            if (algae < threshold && Math.Abs(algae - threshold) > 0.1) {
                failed = false;
            }
        }

        private void EvaluateCityFallingFor(ref bool failed, int num, uint region) {
            if (num <= 0) return;
            
            if (m_StateA.CityFallingTimersPerRegion[region] < num) {
                failed = false;
            }
        }

        #endregion

    }
}