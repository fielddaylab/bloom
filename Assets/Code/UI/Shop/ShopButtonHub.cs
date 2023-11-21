using FieldDay;
using FieldDay.Scripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Zavala.Building;
using Zavala.Economy;

namespace Zavala.UI
{
    public class ShopButtonHub : MonoBehaviour
    {
        private int m_selectedIndex;

        [SerializeField] private ShopItemButton[] m_shopItemBtns;
        [SerializeField] private Color m_affordableColor;
        [SerializeField] private Color m_unaffordableColor;
        [SerializeField] private float m_ySpacing;

        public void Activate() {
            this.gameObject.SetActive(true);

            // Set Button images, text, and functionality according to underlying data
            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                m_shopItemBtns[i].Cost = (int) ShopUtility.PriceLookup(m_shopItemBtns[i].BuildTool);
                m_shopItemBtns[i].CostText.text = "$" + m_shopItemBtns[i].Cost;
                int buttonIndex = i;
                m_shopItemBtns[i].Button.onClick.AddListener(delegate { HandleButtonSelected(buttonIndex); });
            }
        }

        public void Deactivate() {
            this.gameObject.SetActive(false);

            // Set Button images, text, and functionality according to underlying data
            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                m_shopItemBtns[i].Button.onClick.RemoveAllListeners();
            }

            ClearSelected();
        }

        private void ClearSelected() {
            // unselect current button
            if (m_selectedIndex != -1) {
                m_shopItemBtns[m_selectedIndex].Button.image.color = ZavalaColors.ShopItemDefault;
                m_selectedIndex = -1;
            }
        }

        public void CheckCosts(int currBudget) {
            // for each item, set the cost color according to whether player can afford it
            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                bool canAfford = m_shopItemBtns[i].Cost <= currBudget;
                m_shopItemBtns[i].CostBG.color = canAfford ? m_affordableColor : m_unaffordableColor;
                m_shopItemBtns[i].CanAfford = canAfford;
            }
        }

        #region Handlers

        private void HandleButtonSelected(int selectedIndex) {
            if (selectedIndex == m_selectedIndex) {
                // selected current button; should unselect it and return
                ClearSelected();
                return;
            }

            // unselect current button
            ClearSelected();

            ShopItemButton button = m_shopItemBtns[selectedIndex];
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("toolType", button.BuildTool.ToString());
                ScriptUtility.Trigger(GameTriggers.BuildButtonPressed, varTable);
            }

            if (button.CanAfford) {
                // set tool
                BuildToolState bts = Game.SharedState.Get<BuildToolState>();
                bts.ActiveTool = button.BuildTool;

                // set selected color
                m_shopItemBtns[selectedIndex].Button.image.color = ZavalaColors.ShopItemSelected;

                m_selectedIndex = selectedIndex;
            }
            else {
                // some disallow routine? error sound, red flash, queue Balthazar, etc.
            }
        }

        #endregion // Handlers

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