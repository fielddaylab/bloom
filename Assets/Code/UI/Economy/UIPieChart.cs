using System;
using System.Text;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.UI
{
    public class UIPieChart : BatchedComponent
    {
        /*
        static private int CFERTILIZER_INDEX = 0;
        static private int MANURE_INDEX = 1;
        static private int DFERTILIZER_INDEX = 2;
        */

        // [SerializeField] private Transform m_Root;
        [SerializeField] private EllipseGraphic[] m_Portions;
        [SerializeField] private RectTransform[] m_PortionIcons;

        [SerializeField] private TMP_Text[] m_PortionLabels;

        [SerializeField] private float m_IconInset = 20;

        [SerializeField] private int m_HistoryDepth = 10;

        [NonSerialized] private float[] m_Ratios;

        private void Start() {

            ZavalaGame.Events.Register(GameEvents.MarketCycleTickCompleted, HandleMarketCycleTickCompleted);
            // ZavalaGame.Events.Register(GameEvents.RegionSwitched, HandleRegionSwitched);
            UpdateData();
        }

        public void SetAmounts(int[] amounts) {
            MarketUtility.CalculateRatios(ref m_Ratios, amounts);
            RefreshVisuals();
        }

        public void SetAmounts(UnsafeSpan<int> amounts) {
            MarketUtility.CalculateRatios(ref m_Ratios, amounts);
            RefreshVisuals();
        }

        private void RefreshVisuals() {
            if (m_Ratios.Length > m_Portions.Length) {
                Debug.Log("[PieChart] Not enough portions for the amount of data passed in!");
                return;
            }

            float radius = m_Portions[0].rectTransform.rect.width / 2 - m_IconInset;
            float zRotation = 90;
            float prevRotation;
            float portionArc; 
            for (int i = 0; i < m_Ratios.Length; i++) {
                portionArc = m_Ratios[i] * 360;
                m_Portions[i].ArcDegrees = portionArc;
                m_Portions[i].StartDegrees = zRotation % 360;
                //m_Portions[i].transform.localRotation = Quaternion.Euler(new Vector3(0, 0, zRotation));
                prevRotation = zRotation;
                zRotation += m_Portions[i].ArcDegrees;

                // Position icons
                float rad = Mathf.Deg2Rad * ((zRotation + prevRotation) / 2);
                float x = Mathf.Cos(rad) * radius;
                float y = Mathf.Sin(rad) * radius;
                m_PortionIcons[i].anchoredPosition = new Vector2(x, y);
                m_PortionIcons[i].gameObject.SetActive(m_Ratios[i] > 0);
                using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    if (m_Ratios[i] <= 0) {
                        psb.Builder.Append("N/A");
                    } else {
                        psb.Builder.AppendNoAlloc((int)Math.Round(100 * m_Ratios[i])).Append('%');
                    }
                    m_PortionLabels[i].SetText(psb);
                }
            }
        }

        private void UpdateData() {
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            //uint regionIndex = grid.CurrRegionIndex;

            MarketData marketData = Game.SharedState.Get<MarketData>();

            int cFert = 0;
            int manure = 0;
            int dFert = 0;
            for (int i = 0; i < ZavalaGame.SimGrid.RegionCount; i++) {
                marketData.CFertilizerSaleHistory[i].TryGetTotal(m_HistoryDepth, out int cFertVal);
                cFert += cFertVal;
                marketData.ManureSaleHistory[i].TryGetTotal(m_HistoryDepth, out int manureVal);
                manure += manureVal;
                marketData.DFertilizerSaleHistory[i].TryGetTotal(m_HistoryDepth, out int dFertVal);
                dFert += dFertVal;
            }

            unsafe {
                int* values = stackalloc int[3];
                values[0] = cFert;
                values[1] = manure;
                values[2] = dFert;
                SetAmounts(new UnsafeSpan<int>(values, 3));
            }
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
