using BeauRoutine;
using FieldDay;
using FieldDay.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Building;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIShop : SharedPanel
    {
        public TMP_Text NetText;

        [SerializeField] private GameObject m_expandedGroup;

        [SerializeField] private ShopButtonHub m_shopBtnHub;

        [SerializeField] private RectTransform m_expandedRect;

        private Routine m_shopRoutine;
        private Routine m_buttonRoutine;

        private void OnEnable() {
            if (!m_expandedRect) {
                m_expandedRect = m_expandedGroup.GetComponent<RectTransform>();
            }

            Game.Events.Register(GameEvents.BlueprintModeStarted, HandleStartBlueprintMode);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
            Game.Events.Register(GameEvents.DestroyModeStarted, HandleStartDestroyMode);
            Game.Events.Register(GameEvents.DestroyModeEnded, HandleEndDestroyModeMode);

            Collapse();
        }

        private void OnDisable() {
            Game.Events?.DeregisterAllForContext(this);
        }

        public void Expand() {
            ZavalaGame.Events.Dispatch(GameEvents.BuildMenuDisplayed);
            m_shopRoutine.Replace(this, ExpandShopRoutine()); 
        }

        public void Collapse() {
            var currTool = Game.SharedState.Get<BuildToolState>().ActiveTool;
            if (currTool != UserBuildTool.Destroy) {
                BuildToolUtility.SetTool(Game.SharedState.Get<BuildToolState>(), UserBuildTool.None);
            }

            m_shopRoutine.Replace(this, CollapseRoutine());
        }

        public void RefreshCostChecks(int currBudget) {
            m_shopBtnHub.CheckCosts(currBudget);
        }

        #region Handlers

        private void HandleStartBlueprintMode() {
            Expand();
        }

        private void HandleEndBlueprintMode() {
            Collapse();
        }

        private void HandleStartDestroyMode()
        {
            Collapse();
        }

        private void HandleEndDestroyModeMode()
        {
            Expand();
        }

        #endregion // Handlers

        public ShopButtonHub GetBtnHub() {
            return m_shopBtnHub;
        }

        #region Routines

        private IEnumerator ExpandShopRoutine() {
            m_expandedGroup.SetActive(true);
            m_shopBtnHub.Activate();
            yield return m_expandedRect.AnchorPosTo(0, 0.15f, Axis.X).Ease(Curve.CubeOut);
            yield return null;
        }

        private IEnumerator CollapseRoutine() {
            yield return m_expandedRect.AnchorPosTo(-130, 0.15f, Axis.X).Ease(Curve.CubeOut);

            m_shopBtnHub.Deactivate();
            m_expandedGroup.SetActive(false);
            yield return null;
        }

        #endregion // Routines
    }
}