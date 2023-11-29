using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.UI
{
    public class UIBlueprint : SharedPanel, IScenePreload
    {
        #region Inspector

        [SerializeField] private ShopToggleButton m_ShopToggle;
        [SerializeField] private CanvasGroup m_BuildCommandLayout;
        [SerializeField] private RectTransform m_CommandRect;
        [SerializeField] private Button m_DestroyModeButton; // Button to enter Destroy Mode
        [SerializeField] private Button m_BuildUndoButton;   // Undo button when in Build Mode

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
        [SerializeField] private Button m_BuildButton;

        [Header("Destroy Mode")]
        [SerializeField] private CanvasGroup m_DestroyCommandLayout;
        [SerializeField] private CanvasGroup m_DestroyOverlay;
        [SerializeField] private Button m_DestroyConfirmButton;   // Confirms queued destructions
        [SerializeField] private Button m_DestroyUndoButton;   // Undo button when in Destroy Mode
        [SerializeField] private Button m_DestroyExitButton;   // Exit button when in Destroy Mode

        private Routine m_TopBarRoutine;
        private Routine m_ReceiptRoutine;
        private Routine m_BuildCommandLayoutRoutine;
        private Routine m_DestroyCommandLayoutRoutine;

        #endregion // Inspector

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            Game.Events.Register(GameEvents.BlueprintModeStarted, HandleStartBlueprintMode);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
            Game.Events.Register(GameEvents.BuildToolSelected, HandleBuildToolSelected);
            Game.Events.Register(GameEvents.BuildToolDeselected, HandleBuildToolDeselected);

            m_ReceiptGroup.alpha = 0;
            m_BuildingModeText.alpha = 0;
            m_BuildCommandLayout.alpha = 0;
            m_BuildCommandLayout.blocksRaycasts = false;
            m_DestroyCommandLayout.alpha = 0;
            m_DestroyCommandLayout.blocksRaycasts = false;

            m_DestroyCommandLayout.gameObject.SetActive(true);
            m_BuildUndoButton.gameObject.SetActive(false);

            m_DestroyConfirmButton.interactable = false;

            m_BuildButton.onClick.AddListener(HandleBuildConfirmButtonClicked);
            m_BuildUndoButton.onClick.AddListener(HandleUndoBuildButtonClicked);
            m_DestroyUndoButton.onClick.AddListener(HandleUndoDestroyButtonClicked);
            m_DestroyModeButton.onClick.AddListener(HandleDestroyModeButtonClicked);
            m_DestroyConfirmButton.onClick.AddListener(HandleDestroyConfirmButtonClicked);
            m_DestroyExitButton.onClick.AddListener(HandleDestroyExitButtonClicked);

            return null;
        }

        public void UpdateTotalCost(int totalCost, int deltaCost, long playerFunds)
        {
            // Change total to new total
            m_RunningCostText.text = totalCost == 0 ? "" + totalCost : "-" + totalCost;

            // TODO: Flash delta cost animation
            
            // Update funds remaining
            m_FundsRemainingText.text = "" + (playerFunds - totalCost);
        }

        #region UI Handlers

        private void HandleStartBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(true));
            m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(true));
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(true));
        }

        private void HandleEndBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(false));
            m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(false));
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(false));

            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();
            bpState.ExitedBlueprintMode = true;
        }

        private void HandleBuildToolSelected()
        {
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(false));
        }

        private void HandleBuildToolDeselected()
        {
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(true));
        }

        private void HandleBuildConfirmButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.NewBuildConfirmed = true;
        }

        private void HandleUndoBuildButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.UndoClickedBuild = true;
        }

        private void HandleUndoDestroyButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.UndoClickedDestroy = true;
        }

        private void HandleDestroyModeButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.DestroyModeClicked = true;
        }

        private void HandleDestroyConfirmButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.NewDestroyConfirmed = true;
        }

        private void HandleDestroyExitButtonClicked()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.CanceledDestroyMode = true;
        }

        #endregion // UI Handlers

        #region System Handlers

        public void OnBuildConfirmClicked()
        {
            // Exit blueprint mode
            m_ShopToggle.ManualAppear(false);
        }

        // Handle when number of build commits changes
        public void OnNumBuildCommitsChanged(int num)
        {
            m_BuildUndoButton.gameObject.SetActive(num > 0);
        }

        // Handle when number of destroy action commits changes
        public void OnNumDestroyActionsChanged(int num)
        {
            m_DestroyUndoButton.interactable = num > 0;
            m_DestroyConfirmButton.interactable = num > 0;
        }

        public void OnDestroyModeClicked()
        {
            m_BuildCommandLayoutRoutine.Replace(BuildCommandAppearanceTransition(false));
            m_DestroyCommandLayoutRoutine.Replace(DestroyCommandAppearanceTransition(true));
            m_ShopToggle.gameObject.SetActive(false);

            Game.Events.Dispatch(GameEvents.DestroyModeStarted);
        }

        public void OnDestroyConfirmClicked()
        {
            OnExitedDestroyMode();
        }


        public void OnCanceledDestroyMode()
        {
            OnExitedDestroyMode();
        }

        private void OnExitedDestroyMode()
        {
            m_BuildCommandLayoutRoutine.Replace(BuildCommandAppearanceTransition(true));
            m_DestroyCommandLayoutRoutine.Replace(DestroyCommandAppearanceTransition(false));
            m_ShopToggle.gameObject.SetActive(true);

            Game.Events?.Dispatch(GameEvents.DestroyModeEnded);
        }

        #endregion // System Handlers

        #region Routines

        private IEnumerator TopBarAppearanceTransition(bool inBMode)
        {
            if (inBMode)
            {
                var shopState = Game.SharedState.Get<ShopState>();
                shopState.ManulUpdateRequested = true;

                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBBlueprint, 0.1f),
                    m_BuildingModeText.FadeTo(1, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(11, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(-7, .1f, Axis.Y, Space.Self)
                    );
            }
            else
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBDefault, 0.1f),
                    m_BuildingModeText.FadeTo(0, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self)
                    );
            }
        }

        private IEnumerator ReceiptAppearanceTransition(bool inBMode)
        {
            if (inBMode)
            {
                yield return Routine.Combine(
                    m_ReceiptGroup.FadeTo(1, .1f)
                    );
            }
            else
            {
                yield return Routine.Combine(
                    m_ReceiptGroup.FadeTo(0, .1f)
                    );
            }
        }

        private IEnumerator BuildCommandAppearanceTransition(bool appearing)
        {
            if (appearing)
            {
                // nothing to undo at first
                SetBuildCommandLayoutInteractable(true);
                yield return Routine.Combine(
                    m_BuildCommandLayout.FadeTo(1, .1f)
                    );
            }
            else
            {
                SetBuildCommandLayoutInteractable(false);
                yield return Routine.Combine(
                    m_BuildCommandLayout.FadeTo(0, .1f)
                    );
            }
        }

        private IEnumerator DestroyCommandAppearanceTransition(bool appearing)
        {
            if (appearing)
            {
                SetDestroyCommandLayoutInteractable(true);
                yield return Routine.Combine(
                    m_DestroyCommandLayout.FadeTo(1, 0.1f),
                    m_DestroyOverlay.FadeTo(1, 0.1f)
                );
            }
            else
            {
                SetDestroyCommandLayoutInteractable(false);
                yield return Routine.Combine(
                    m_DestroyCommandLayout.FadeTo(0, 0.1f),
                    m_DestroyOverlay.FadeTo(0, 0.1f)
                );
            }
        }

        #endregion // Routines

        #region Helpers

        private void SetBuildCommandLayoutInteractable(bool canInteract)
        {
            m_BuildCommandLayout.blocksRaycasts = canInteract;
            m_DestroyModeButton.interactable = canInteract;
            m_BuildUndoButton.interactable = canInteract;
        }

        private void SetDestroyCommandLayoutInteractable(bool canInteract)
        {
            m_DestroyCommandLayout.blocksRaycasts = canInteract;
        }

        #endregion // Helpers
    }
}