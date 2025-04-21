
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using System;
using Zavala.Building;
using Zavala.Data;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Scripting {
    public class WinLossSystem : SharedStateSystemBehaviour<WinLossState, SimGridState, RegionUnlockState, BuildToolState> {
        public override bool HasWork() {
            if (base.HasWork()) {
                //return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
                return true; 
            }
            return false;
        }

        #region Work

        public override void ProcessWork(float deltaTime) {
            if (!m_StateA.EndCheckTimer.Advance(deltaTime, ZavalaGame.SimTime)) {
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
                        WinLossUtility.TriggerEnding(cond.Type, region);
                        m_StateA.HasMetEnding = true;
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
            EvaluateBudgetBelow(ref endPending, cond.BudgetBelow, region, ref cond);
            if (cond.CheckFarmsUnconnected) {
                EvaluateFarmsUnconnected(ref endPending, region, ref cond);
            }
            EvaluateTotalPAbove(ref endPending, cond.TotalPhosphorusAbove, region, ref cond);
            EvaluateTotalAlgaeAbove(ref endPending, cond.TotalAlgaeAbove, region, ref cond);
            EvaluateCityFallingFor(ref endPending, cond.CityFallingDurationAbove, region, ref cond);
            EvaluateRegionAgeAbove(ref endPending, cond.RegionAgeAbove, region, ref cond);
            EvaluateNodeReached(ref endPending, cond.NodeReached, region, ref cond);
            EvaluateFertilizerProportions(ref endPending, cond.MFertilizerRatio, cond.DFertilizerRatio, region, ref cond);
            if (cond.CheckCitiesConnected) {
                EvaluateCityConnected(ref endPending, region, ref cond);
            }
            return endPending;
        }

        #region Conditions

        private void EvaluateBudgetBelow(ref bool triggered, int num, uint region, ref EndGameConditions cond) {
            if (num <= 0) return;

            if (BudgetUtility.CanSpendBudget(Game.SharedState.Get<BudgetData>(), num, region)) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.OutOfMoney, EndConditionType.BudgetBelow, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.OutOfMoney, EndConditionType.BudgetBelow, region);
            }
        }
        private void EvaluateFarmsUnconnected(ref bool triggered, uint region, ref EndGameConditions cond) {
            if (m_StateD.FarmsConnectedInRegion[(int)region]) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.OutOfMoney, EndConditionType.FarmsUnconnected, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.OutOfMoney, EndConditionType.FarmsUnconnected, region);
            }
        }

        private void EvaluateCityConnected(ref bool triggered, uint region, ref EndGameConditions cond) {
            if (!m_StateD.CityConnectedInRegion[(int)region]) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.Succeeded, EndConditionType.CitiesConnected, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.Succeeded, EndConditionType.CitiesConnected, region);

            }
        }

        private void EvaluateTotalPAbove(ref bool triggered, int num, uint region, ref EndGameConditions cond) {
            if (num <= 0) return;
            long phos = Game.SharedState.Get<SimPhosphorusState>().TotalPPerRegion[region];
            if (phos < num) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.TooManyBlooms, EndConditionType.PhosphorusAbove, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.TooManyBlooms, EndConditionType.PhosphorusAbove, region);
            }
        }

        private void EvaluateTotalAlgaeAbove(ref bool triggered, float threshold, uint region, ref EndGameConditions cond) {
            if (threshold <= 0) return;
            float algae = Game.SharedState.Get<SimAlgaeState>().TotalAlgaePerRegion[region];
            if (algae < threshold && Math.Abs(algae - threshold) > 0.1) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.TooManyBlooms, EndConditionType.AlgaeAbove, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.TooManyBlooms, EndConditionType.AlgaeAbove, region);
            }
        }

        private void EvaluateCityFallingFor(ref bool triggered, int num, uint region, ref EndGameConditions cond) {
            if (num <= 0) return;
            
            if (m_StateA.CityFallingTimersPerRegion[region] < num) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.CityFailed, EndConditionType.CityFallingDurationAbove, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.CityFailed, EndConditionType.CityFallingDurationAbove, region);
            }
        }

        private void EvaluateRegionAgeAbove(ref bool triggered, int age, uint region, ref EndGameConditions cond) {
            if (age <= 0) return;

            if (m_StateB.Regions[(int)region].Age < age) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.Succeeded, EndConditionType.RegionAgeAbove, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.Succeeded, EndConditionType.RegionAgeAbove, region);
            }
        }

        private void EvaluateNodeReached(ref bool triggered, string title, uint region, ref EndGameConditions cond) {
            if (title == "" || title == null) return;

            if (!title.Contains("region")) {
                Log.Warn("[WinLossSystem] NodeReached condition: use format region1.nodeTitle. Yours: " + title);
            }
            if (!ScriptUtility.Persistence.SessionViewedNodeIds.Contains(new StringHash32(title))) {
                triggered = false;
                TryLogConditionChanged(false, ref cond, EndType.Succeeded, EndConditionType.NodeReached, region);
            } else {
                TryLogConditionChanged(true, ref cond, EndType.Succeeded, EndConditionType.NodeReached, region);
            }

        }

        private void EvaluateFertilizerProportions(ref bool triggered, TargetRatio MFertProp, TargetRatio DFertProp, uint condRegion, ref EndGameConditions cond) {
            if (MFertProp.Target <= 0 && DFertProp.Target <= 0) return;
            bool MFertMet;
            bool DFertMet;
            MarketData data = Game.SharedState.Get<MarketData>();
            int totalMFert = 0, totalDFert = 0, totalManure = 0;
            int depth = (int)m_StateA.EndCheckTimer.Period;
            for (int region = 0; region < ZavalaGame.SimGrid.RegionCount; region++) {
                data.CFertilizerSaleHistory[region].TryGetTotal(depth, out int regionMFert);
                totalMFert += regionMFert;
                data.ManureSaleHistory[region].TryGetTotal(depth, out int regionManure);
                totalManure += regionManure;
                data.DFertilizerSaleHistory[region].TryGetTotal(depth, out int regionDFert);
                totalDFert += regionDFert;
            }
            float[] ratios = new float[3];
            MarketUtility.CalculateRatios(ref ratios, new int[3] { totalMFert, totalManure, totalDFert });
            
            float MFertVal = ratios[0];
            float DFertVal = ratios[2];
            Log.Debug("[WinLossSystem: Evaluating Fert. proportions: {0}:{1}:{2} over past {3} ticks", ratios[0], ratios[1], ratios[2], depth );

            if (MFertProp.Target == 0 && !MFertProp.Above) {
                // uninitialized
                MFertMet = true;
            } else {
                MFertMet = MFertProp.Above ? MFertVal > MFertProp.Target : MFertVal < MFertProp.Target;
            }
            if (DFertProp.Target == 0 && !DFertProp.Above) {
                // uninitialized
                DFertMet = true;
            } else {
                DFertMet = DFertProp.Above ? DFertVal > DFertProp.Target : DFertVal < DFertProp.Target;
            }
            TryLogConditionChanged(MFertMet, ref cond, EndType.Succeeded, EndConditionType.MFertilizerRatio, condRegion);
            TryLogConditionChanged(DFertMet, ref cond, EndType.Succeeded, EndConditionType.DFertilizerRatio, condRegion);
            triggered = MFertMet && DFertMet;
            return;
        }

        #endregion

        #region Analytics
         
        private void TryLogConditionChanged(bool met, ref EndGameConditions cond, EndType endType, EndConditionType condType, uint region) {
            if (met) {
                TriggerConditionMet(endType, condType, region);
                cond.CondWasMet[(int)condType] = true;
            } else if (cond.CondWasMet[(int)condType]) {
                TriggerConditionLost(endType, condType, region);
                cond.CondWasMet[(int)condType] = false;
            }
        }
        private void TriggerConditionMet(EndType endType, EndConditionType condType, uint region) {
            ZavalaGame.Events.Dispatch(GameEvents.EndConditionMet, EvtArgs.Box(new EndConditionData(EnumLookup.Get(endType), EnumLookup.Get(condType), (ushort)region)));
        }

        private void TriggerConditionLost(EndType endType, EndConditionType condType, uint region) {
            ZavalaGame.Events.Dispatch(GameEvents.EndConditionLost, EvtArgs.Box(new EndConditionData(EnumLookup.Get(endType), EnumLookup.Get(condType), (ushort)region)));
        }
        #endregion
    }
}