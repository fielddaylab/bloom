using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class UIShop : MonoBehaviour
    {
        public TMP_Text NetText;

        [SerializeField] private Button m_expandBtn;
        [SerializeField] private Button m_collapseBtn;
        [SerializeField] private GameObject m_expandedGroup;

        [SerializeField] private ShopButtonHub m_shopBtnHub;

        private Routine m_expandRoutine;

        private void OnEnable() {
            m_expandBtn.onClick.AddListener(HandleExpandBtnClicked);
            m_collapseBtn.onClick.AddListener(HandleCollapeBtnClicked);
            Collapse();
        }

        private void OnDisable() {
            m_expandBtn.onClick.RemoveListener(HandleExpandBtnClicked);
            m_collapseBtn.onClick.RemoveListener(HandleCollapeBtnClicked);
        }

        public void Expand() {
            m_shopBtnHub.Activate();
            NetText.enabled = true;

            m_expandRoutine.Replace(ExpandRoutine());
        }

        public void Collapse() {
            m_shopBtnHub.Deactivate();
            NetText.enabled = false;

            m_expandRoutine.Replace(CollapseRoutine());
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

        private IEnumerator ExpandRoutine() {
            m_expandedGroup.SetActive(true);
            yield return null;
        }

        private IEnumerator CollapseRoutine() {
            m_expandedGroup.SetActive(false);
            yield return null;
        }

        #endregion // Routines
    }
}