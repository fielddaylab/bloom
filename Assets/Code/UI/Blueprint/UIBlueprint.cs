using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI
{
    public class UIBlueprint : MonoBehaviour, IScenePreload
    {
        #region Inspector

        [SerializeField] private ShopToggleButton m_ShopToggle;
        [SerializeField] private CanvasGroup m_CommandLayout;
        [SerializeField] private Button m_DestroyButton;
        [SerializeField] private Button m_UndoButton;

        [Header("Top Bar")]
        [SerializeField] private TMP_Text m_BuildingModeText;
        [SerializeField] private TMP_Text m_RegionText;

        [SerializeField] private RectGraphic m_TopBarBG;
        [SerializeField] private Color m_TBDefault;
        [SerializeField] private Color m_TBBlueprint;

        [Header("Receipt")]
        [SerializeField] private CanvasGroup m_ReceiptGroup;
        [SerializeField] private TMP_Text m_RunningCostText;
        [SerializeField] private TMP_Text m_FundsRemainingText;


        private Routine m_TopBarRoutine;
        private Routine m_CommandLayoutRoutine;

        #endregion // Inspector

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            Game.Events.Register(GameEvents.BlueprintModeStarted, HandleStartBlueprintMode);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
            Game.Events.Register(GameEvents.BuildToolSelected, HandleBuildToolSelected);
            Game.Events.Register(GameEvents.BuildToolDeselected, HandleBuildToolDeselected);

            m_ReceiptGroup.alpha = 0;
            m_BuildingModeText.alpha = 0;
            m_CommandLayout.alpha = 0;
            return null;
        }

        public void UpdateTotalCost(int totalCost, int deltaCost, long playerFunds)
        {
            // Change total to new total
            m_RunningCostText.text = "-" + totalCost;

            // TODO: Flash delta cost animation

            m_FundsRemainingText.text = "" + (playerFunds - totalCost);
        }

        #region Handlers

        private void HandleStartBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(true));
            m_CommandLayoutRoutine.Replace(this, CommandAppearanceTransition(true));
        }

        private void HandleEndBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(false));
            m_CommandLayoutRoutine.Replace(this, CommandAppearanceTransition(false));
        }

        private void HandleBuildToolSelected()
        {
            m_CommandLayoutRoutine.Replace(this, CommandAppearanceTransition(false));
        }

        private void HandleBuildToolDeselected()
        {
            m_CommandLayoutRoutine.Replace(this, CommandAppearanceTransition(true));
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator TopBarAppearanceTransition(bool inBMode)
        {
            if (inBMode)
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBBlueprint, 0.1f),
                    m_ReceiptGroup.FadeTo(1, .1f),
                    m_BuildingModeText.FadeTo(1, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(11, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(-7, .1f, Axis.Y, Space.Self)
                    );
            }
            else
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBDefault, 0.1f),
                    m_ReceiptGroup.FadeTo(0, .1f),
                    m_BuildingModeText.FadeTo(0, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self)
                    );
            }
        }

        private IEnumerator CommandAppearanceTransition(bool appearing)
        {
            if (appearing)
            {
                SetCommandLayoutInteractable(true);
                yield return Routine.Combine(
                    m_CommandLayout.FadeTo(1, .1f)
                    );
            }
            else
            {
                SetCommandLayoutInteractable(false);
                yield return Routine.Combine(
                    m_CommandLayout.FadeTo(0, .1f)
                    );
            }
        }

        #endregion // Routines

        #region Helpers

        private void SetCommandLayoutInteractable(bool canInteract)
        {
            m_DestroyButton.interactable = canInteract;
            m_UndoButton.interactable = canInteract;
        }

        #endregion // Helpers
    }
}