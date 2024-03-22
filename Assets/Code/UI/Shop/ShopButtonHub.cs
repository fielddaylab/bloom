using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Animation;
using FieldDay.Scenes;
using FieldDay.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.Economy;

namespace Zavala.UI
{
    public class ShopButtonHub : MonoBehaviour, IScenePreload
    {
        private int m_selectedIndex;

        [SerializeField] private ShopItemButton[] m_shopItemBtns;
        [SerializeField] private Color m_affordableColor;
        [SerializeField] private Color m_unaffordableColor;
        [SerializeField] private float m_ySpacing;
        [SerializeField] private Sprite UnlockedBG;
        [SerializeField] private Sprite LockedBG;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            Game.Events.Register(GameEvents.BuildToolDeselected, HandleBuildToolDeselected);

            for (int i = 0; i < m_shopItemBtns.Length; i++) {
                SetShopItemBtnUnlocked(m_shopItemBtns[i], m_shopItemBtns[i].StartUnlocked);
            }

            return null;
        }

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

    public ShopItemButton GetShopItemBtn(UserBuildTool tool) {
        return Array.Find(m_shopItemBtns, b => b.BuildTool == tool);
    }

    public StringBuilder GetUnlockedToolData() {
            using (var psb = PooledStringBuilder.Create()) {
                psb.Builder.Append('[');
                foreach(ShopItemButton b in m_shopItemBtns) {
                    if (b.Button.interactable) {
                        psb.Builder.Append('{');
                        psb.Builder.Append("building_type: ").Append(b.BuildTool);
                        psb.Builder.Append("cost: ").Append(b.Cost);
                        psb.Builder.Append("},");
                    }
                }
                if (psb.Builder.Length > 0) psb.Builder.Length -= 1; // trim last comma
                psb.Builder.Append(']');
                return psb.Builder;
            }
    }

    public void SetShopItemBtnUnlocked(ShopItemButton btn, bool unlocked) {
        btn.ButtonBG.sprite = unlocked ? UnlockedBG : LockedBG;
        btn.Button.interactable = unlocked;
        btn.transform.GetChild(0).gameObject.SetActive(unlocked);
    }

        #region Handlers

        private void HandleButtonSelected(int selectedIndex) {
            BuildToolState bts = Game.SharedState.Get<BuildToolState>();
            ShopItemButton button = m_shopItemBtns[selectedIndex];

            if (selectedIndex == m_selectedIndex) {
                // selected current button; should unselect it and return
                ClearSelected();

                BuildToolUtility.SetTool(bts, UserBuildTool.None);
                Game.Animation.AddLiteAnimator(button, 0.15f);
                return;
            }

            // unselect current button
            ClearSelected();

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("toolType", button.BuildTool.ToString());
                ScriptUtility.Trigger(GameTriggers.BuildButtonPressed, varTable);
            }

            if (button.CanAfford) {
                // set tool
                BuildToolUtility.SetTool(bts, button.BuildTool);

                // set selected color
                m_shopItemBtns[selectedIndex].Button.image.color = ZavalaColors.ShopItemSelected;

                m_selectedIndex = selectedIndex;
                Game.Animation.AddLiteAnimator(button, 0.15f);
            }
            else {
                // some disallow routine? error sound, red flash, queue Balthazar, etc.
                Game.Animation.AddLiteAnimator(button, 0.3f);
            }
        }

        private void HandleBuildToolDeselected()
        {
            ClearSelected();
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