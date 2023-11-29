using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;

namespace Zavala.UI {
    public class EconDialogueModule : DialogueModuleBase, IDialogueModule
    {
        private enum DisplayState {
            None,
            Pie,
            Market,
            Bar
        }

        #region Inspector

        [SerializeField] private Button m_MarketButton, m_BarButton;
        //[SerializeField] private Button m_PieButton;

        [Space(5)]
        [Header("Visuals")]
        //[SerializeField] private UIPieChart m_PieChart;
        [SerializeField] private UIMarketShareGraph m_MarketShare;
        [SerializeField] private UIBarChart m_BarChart;

        private GameObject[] m_GraphObjs = null;
        private Button[] m_GraphButtons = null;

        private DisplayState m_DisplayState;


        #endregion // Inspector


        #region IDialogueModule

        public override void Activate(bool allowReactivate) {
            base.Activate(allowReactivate);

            // Save objects for selective showing later
            if (m_GraphObjs == null) {
                m_GraphObjs = new GameObject[2] {
                    //m_PieChart.gameObject,
                    m_MarketShare.gameObject,
                    m_BarChart.gameObject
                };
            }

            if (m_GraphButtons == null) {
                m_GraphButtons = new Button[2] {
                    //m_PieButton,
                    m_MarketButton,
                    m_BarButton
                };
            }

            m_DisplayState = DisplayState.None;

            // Visuals start hidden
            HideGraphs();

            // Register buttons
            //m_PieButton.onClick.AddListener(HandlePieButtonClicked);
            m_MarketButton.onClick.AddListener(HandleMarketButtonClicked);
            m_BarButton.onClick.AddListener(HandleBarButtonClicked);
        }

        public override void Deactivate() {
            base.Deactivate();

            // Deregister buttons
            //m_PieButton.onClick.RemoveAllListeners();
            m_MarketButton.onClick.RemoveAllListeners();
            m_BarButton.onClick.RemoveAllListeners();
        }

        #endregion // IDialogueModule

        #region Handlers
/*
        private void HandlePieButtonClicked() {
            HideGraphs();
            if (m_DisplayState == DisplayState.Pie) {
                // Keep hidden
                m_DisplayState = DisplayState.None;
            } else {
                // Show graph
                m_PieChart.gameObject.SetActive(true);
                m_DisplayState = DisplayState.Pie;
                SetColorPressed(m_PieButton, true);
            }
        }
*/
        private void HandleMarketButtonClicked() {
            HideGraphs();
            if (m_DisplayState == DisplayState.Market) {
                // Keep hidden
                m_DisplayState = DisplayState.None;
            } else {
                // Show graph
                m_MarketShare.gameObject.SetActive(true);
                m_DisplayState = DisplayState.Market;
                SetColorPressed(m_MarketButton, true);
            }
        }

        private void HandleBarButtonClicked() {
            HideGraphs();
            if (m_DisplayState == DisplayState.Bar) {
                // Keep hidden
                m_DisplayState = DisplayState.None;
            } else {
                // Show graph
                m_BarChart.gameObject.SetActive(true);
                m_DisplayState = DisplayState.Bar;
                SetColorPressed(m_BarButton, true);

            }
        }

        #endregion // Handlers

        private void HideGraphs() {
            foreach(GameObject obj in m_GraphObjs) {
                obj.SetActive(false);
            }
            foreach(Button btn in m_GraphButtons) {
                btn.enabled = true;
                SetColorPressed(btn, false);
            }
        }
    }
}
