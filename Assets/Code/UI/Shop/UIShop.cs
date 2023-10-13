using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIShop : MonoBehaviour
    {
        public TMP_Text NetText;
        public TMP_Text RegionName;

        [SerializeField] private Button m_expandBtn;
        [SerializeField] private Button m_collapseBtn;
        [SerializeField] private GameObject m_expandedGroup;

        [SerializeField] private ShopButtonHub m_shopBtnHub;

        [SerializeField] private RectTransform m_expandedRect;
        [SerializeField] private RectTransform m_buttonRect;

        private Routine m_shopRoutine;
        private Routine m_buttonRoutine;

        private void OnEnable() {
            if (!m_expandedRect) {
                m_expandedRect = m_expandedGroup.GetComponent<RectTransform>();
            }
            if (!m_buttonRect) {
                m_buttonRect = m_expandBtn.gameObject.GetComponent<RectTransform>();
            }
                m_expandBtn.onClick.AddListener(HandleExpandBtnClicked);
            m_collapseBtn.onClick.AddListener(HandleCollapeBtnClicked);
            Collapse();
            
        }

        private void OnDisable() {
            m_expandBtn.onClick.RemoveListener(HandleExpandBtnClicked);
            m_collapseBtn.onClick.RemoveListener(HandleCollapeBtnClicked);
        }

        public void Expand() {
            SimTimeUtility.Pause(SimPauseFlags.Blueprints, ZavalaGame.SimTime);
            m_shopRoutine.Replace(ExpandShopRoutine()); 
            m_buttonRoutine.Replace(HideShopButtonRoutine());
        }

        public void Collapse() {
            SimTimeUtility.Resume(SimPauseFlags.Blueprints, ZavalaGame.SimTime);
            m_shopRoutine.Replace(CollapseRoutine());
            m_buttonRoutine.Replace(ShowShopButtonRoutine());
        }

        public void RefreshCostChecks(int currBudget) {
            m_shopBtnHub.CheckCosts(currBudget);
        }

        #region Handlers

        private void HandleExpandBtnClicked() {
            Expand();
        }

        private void HandleCollapeBtnClicked() {
            Collapse();
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator ExpandShopRoutine() {
            m_expandedGroup.SetActive(true);
            m_shopBtnHub.Activate();
            NetText.enabled = true;
            yield return m_expandedRect.AnchorPosTo(0, 0.3f, Axis.X).Ease(Curve.CubeIn);
            yield return null;
        }

        private IEnumerator HideShopButtonRoutine() {
            yield return m_buttonRect.AnchorPosTo(-160, 0.3f, Axis.X).Ease(Curve.CubeIn);
            m_expandBtn.gameObject.SetActive(false);
            yield return null;
        }

        private IEnumerator CollapseRoutine() {
            yield return m_expandedRect.AnchorPosTo(-200, 0.3f, Axis.X).Ease(Curve.CubeIn);

            m_shopBtnHub.Deactivate();
            NetText.enabled = false;
            m_expandedGroup.SetActive(false);
            yield return null;
        }

        private IEnumerator ShowShopButtonRoutine() {
            m_expandBtn.gameObject.SetActive(true);
            yield return m_buttonRect.AnchorPosTo(40, 0.3f, Axis.X).Ease(Curve.CubeIn);
            yield return null;
        }

        #endregion // Routines
    }
}