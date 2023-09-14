using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.UI
{
    public class UIBarChart : BatchedComponent
    {
        /*
        private static int SALES_INDEX = 0;
        private static int IMPORT_INDEX = 1;
        private static int PENALTIES_INDEX = 2;
        */

        [SerializeField] private UIFinancialTarget[] m_Targets;

        [SerializeField] private int m_HistoryDepth = 10;

        private float[] m_Ratios;

        private void Start() {
            SetTargetLines(new FinancialTargetThreshold[3] {
                new FinancialTargetThreshold(false, 0.9f),
                new FinancialTargetThreshold(true, 0.5f),
                new FinancialTargetThreshold(true, 0.1f)}
            );

            SetAmounts(new int[3] { 5, 5, 3 });

            ZavalaGame.Events.Register(GameEvents.MarketCycleTickCompleted, HandleMarketCycleTickCompleted);
            ZavalaGame.Events.Register(GameEvents.RegionSwitched, HandleRegionSwitched);
        }


        /// <summary>
        /// How much revenue is generated from sales, imports, and penalties
        /// </summary>
        /// <param name="amounts"></param>
        public void SetAmounts(int[] amounts) {
            RecalculateRatios(amounts);
            UpdateRatioVisuals();
        }

        public void SetTargetLines(FinancialTargetThreshold[] targets) {
            for (int i = 0; i < m_Targets.Count(); i++) {
                // reposition target line
                m_Targets[i].SetTargetLine(targets[i].Value);

                // Orient dir arrow
                int dir = 1; // point up
                if (!targets[i].Dir) {
                    // point down
                    dir = -1;
                }
                m_Targets[i].SetArrowDir(dir);
            }
        }

        private void RecalculateRatios(int[] amounts) {
            int total = 0;

            for (int i = 0; i < amounts.Length; i++) {
                total += amounts[i];
            }

            if (total <= 0) {
                total = 1;
            }

            if (m_Ratios == null) {
                m_Ratios = new float[amounts.Length];
            }

            for (int i = 0; i < m_Ratios.Length; i++) {
                m_Ratios[i] = (float)amounts[i] / total;
            }
        }

        private void UpdateRatioVisuals() {
            for (int i = 0; i < m_Targets.Count(); i++) {
                m_Targets[i].SetRatio(m_Ratios[i]);
            }
        }

        private void UpdateData() {
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            uint regionIndex = grid.CurrRegionIndex;

            MarketData marketData = Game.SharedState.Get<MarketData>();

            marketData.SalesTaxHistory[regionIndex].TryGetTotal(m_HistoryDepth, out int salesVal);
            marketData.ImportTaxHistory[regionIndex].TryGetTotal(m_HistoryDepth, out int importVal);
            marketData.PenaltiesHistory[regionIndex].TryGetTotal(m_HistoryDepth, out int penaltyVal);

            SetAmounts(new int[3] { salesVal, importVal, penaltyVal });
        }


        #region Handlers

        private void HandleMarketCycleTickCompleted() {
            UpdateData();
        }

        private void HandleRegionSwitched() {
            UpdateData();
        }

        #endregion // Handlers
    }
}
