using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI
{
    public class ShopButtonHub : MonoBehaviour
    {
        private int m_selectedIndex;

        [SerializeField] private ShopItemButton[] m_shopItemBtns;
        [SerializeField] private float m_ySpacing;

        public void Activate() {
            this.gameObject.SetActive(true);

            // Set Button images, text, and functionality according to underlying data
            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                m_shopItemBtns[i].CostText.text = "$" + m_shopItemBtns[i].Cost;
                // TODO: check if cost is greater than current budget?
                // TODO: add listener: when button clicked, set BuildToolState.ActiveTool to button's BuildTool
            }
        }

        public void Deactivate() {
            this.gameObject.SetActive(false);

            SelectIndex(-1);
        }

        private void SelectIndex(int index) {
            m_selectedIndex = index;
        }

        #region Editor

        [ContextMenu("Apply Spacing")]
        private void ApplySpacing() {
            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                Vector3 currPos = m_shopItemBtns[i].transform.position;
                m_shopItemBtns[i].transform.position = new Vector3(currPos.x, m_shopItemBtns[0].transform.position.y - i * m_ySpacing, currPos.z);
            }
        }

        #endregion // Editor
    }
}