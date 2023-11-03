using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;
using Zavala.World;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class RegionUnlockSystem : SharedStateSystemBehaviour<RegionUnlockState, SimGridState, SimPhosphorusState, MarketData>
    {
        public override bool HasWork() {
            if(base.HasWork()) {
                return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
            }
            return false;
        }

        #region Work

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.UnlockCount >= m_StateA.UnlockGroups.Count) {
                return;
            }

            // Only check unlocks every sim tick
            if (!m_StateA.SimPhosphorusAdvanced) {
                return;
            }
            else {
                m_StateA.SimPhosphorusAdvanced = false;
            }
            
            UnlockGroup currUnlockGroup = m_StateA.UnlockGroups[m_StateA.UnlockCount];

            // Implement checks
            bool passedCheck = true;

            // ALL conditions must be passed (&&, not ||)
            foreach (UnlockConditionGroup conditionGroup in currUnlockGroup.UnlockConditions) {
                switch (conditionGroup.Type) {
                    case UnlockConditionType.AvgPhosphorusRunoff:
                        EvaluateAvgRunoffPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.TotalPhosphorusRunoff:
                        EvaluateTotalRunoffPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.MarketShareTargets:
                        EvaluateMarketShareTargetsPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.RevenueTargets:
                        EvaluateRevenueTargetsPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.AccrueWealth:
                        EvaluateWealthTargetPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.WaterHealth:
                        EvaluateWaterHealthTargetPassed(ref passedCheck, conditionGroup);
                        break;
                    case UnlockConditionType.RegionAge:
                        EvaluateRegionAgeTargetPassed(ref passedCheck, conditionGroup);
                        break;
                    default:
                        break;
                }
            }

            if (passedCheck) {
                // Unlock regions
                foreach (int region in currUnlockGroup.RegionIndexUnlocks) {
                    SimWorldState worldState = Game.SharedState.Get<SimWorldState>();
                    RegionUnlockUtility.UnlockRegion(m_StateB, region, worldState);
                }

                m_StateA.UnlockCount++;
            }
        }

        #endregion // Work

        #region Helpers

        private void EvaluateAvgRunoffPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                // TODO: would be cleaner to convert these into delegates
                if (m_StateC.HistoryPerRegion[region].TryGetAvg(conditionGroup.Scope, out float avg)) {
                    if (!MarketUtility.EvaluateTargetThreshold(avg, conditionGroup.TargetRunoff)) {
                        // did not meet threshold
                        passedCheck = false;
                        return;
                    }
                }
                else {
                    // not enough data -- false by default
                    passedCheck = false;
                    return;
                }
            }
        }

        private void EvaluateTotalRunoffPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                // TODO: would be cleaner to convert these into delegates
                if (m_StateC.HistoryPerRegion[region].TryGetTotal(conditionGroup.Scope, out int total)) {
                    if (!MarketUtility.EvaluateTargetThreshold(total, conditionGroup.TargetRunoff)) {
                        // did not meet threshold
                        passedCheck = false;
                        return;
                    }
                }
                else {
                    // not enough data -- false by default
                    passedCheck = false;
                    return;
                }
            }
        }

        private void EvaluateMarketShareTargetsPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                float[] ratios = new float[3];
                if (!m_StateD.CFertilizerSaleHistory[region].TryGetTotal(conditionGroup.Scope, out int totalCFertilizer)) {
                    // not enough data -- false by default
                    passedCheck = false;
                    return;
                }
                m_StateD.ManureSaleHistory[region].TryGetTotal(conditionGroup.Scope, out int totalManure);
                m_StateD.DFertilizerSaleHistory[region].TryGetTotal(conditionGroup.Scope, out int totalDFertilizer);
                MarketUtility.CalculateRatios(ref ratios, new int[3] { totalCFertilizer, totalManure, totalDFertilizer });

                if (!MarketUtility.EvaluateTargetThreshold(ratios[0], conditionGroup.TargetCFertilizer)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
                if (!MarketUtility.EvaluateTargetThreshold(ratios[1], conditionGroup.TargetManure)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
                if (!MarketUtility.EvaluateTargetThreshold(ratios[2], conditionGroup.TargetDFertilizer)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
            }
        }

        private void EvaluateRevenueTargetsPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                float[] ratios = new float[3];
                if (!m_StateD.SalesTaxHistory[region].TryGetTotal(conditionGroup.Scope, out int totalSales)) {
                    // not enough data -- false by default
                    passedCheck = false;
                    return;
                }
                m_StateD.ImportTaxHistory[region].TryGetTotal(conditionGroup.Scope, out int totalImports);
                m_StateD.PenaltiesHistory[region].TryGetTotal(conditionGroup.Scope, out int totalPenalties);
                MarketUtility.CalculateRatios(ref ratios, new int[3] { totalSales, totalImports, totalPenalties });

                if (!MarketUtility.EvaluateTargetThreshold(ratios[0], conditionGroup.TargetSalesRevenue)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
                if (!MarketUtility.EvaluateTargetThreshold(ratios[1], conditionGroup.TargetImportRevenue)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
                if (!MarketUtility.EvaluateTargetThreshold(ratios[2], conditionGroup.TargetPenaltyRevenue)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
            }
        }

        private void EvaluateWealthTargetPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                BudgetData budgetData = Game.SharedState.Get<BudgetData>();

                if (!MarketUtility.EvaluateTargetThreshold(budgetData.BudgetsPerRegion[region].Net, conditionGroup.TargetWealth)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
            }
        }

        private void EvaluateWaterHealthTargetPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                // TODO: implement this
            }

            passedCheck = false;
        }

        private void EvaluateRegionAgeTargetPassed(ref bool passedCheck, UnlockConditionGroup conditionGroup) {
            foreach (int region in conditionGroup.ChecksRegions) {
                if (!MarketUtility.EvaluateTargetThreshold(m_StateB.Regions[region].Age, conditionGroup.TargetAge)) {
                    // did not meet threshold
                    passedCheck = false;
                    return;
                }
            }
        }


        #endregion // Helpers
    }
}

