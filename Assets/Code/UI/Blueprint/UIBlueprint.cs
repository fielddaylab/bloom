using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Economy;
using Zavala.Sim;

namespace Zavala.UI
{
    public class UIBlueprint : SharedPanel, IScenePreload
    {
        #region Types

        [Serializable] public class PopupPool : SerializablePool<UIPolicyBoxPopup> { } // The packages airships drop

        #endregion // Types

        #region Inspector

        [SerializeField] private ShopToggleButton m_ShopToggle;
        [SerializeField] private CanvasGroup m_BuildCommandLayout;
        [SerializeField] private RectTransform m_CommandRect;
        [SerializeField] private Button m_DestroyModeButton; // Button to enter Destroy Mode
        [SerializeField] private Button m_BuildUndoButton;   // Undo button when in Build Mode

        [Header("Top Bar")]
        [SerializeField] private Graphic m_BuildingModeText;
        [SerializeField] private TMP_Text m_RegionText;

        [SerializeField] private RectGraphic m_TopBarBG;
        [SerializeField] private Color m_TBDefault;
        [SerializeField] private Color[] m_TBColorPerRegion;
        // [SerializeField] private Color m_TBBlueprint;

        [SerializeField] private CanvasGroup m_PolicyBoxGroup;
        [SerializeField] private UIPolicyBox[] m_PolicyBoxes;
        [SerializeField] private UIPolicyBox m_MilkRevenueBox; // Not in m_PolicyBoxes as it is not associated with a PolicyType

        [Header("Receipt")]
        [SerializeField] private CanvasGroup m_ReceiptGroup;
        [SerializeField] private TMP_Text m_RunningCostText;
        [SerializeField] private TMP_Text m_FundsRemainingText;
        [SerializeField] private Button m_BuildButton;

        [Header("Destroy Mode")]
        [SerializeField] private CanvasGroup m_DestroyCommandLayout;
        [SerializeField] private Button m_DestroyConfirmButton;   // Confirms queued destructions
        [SerializeField] private Button m_DestroyUndoButton;   // Undo button when in Destroy Mode
        [SerializeField] private Button m_DestroyExitButton;   // Exit button when in Destroy Mode

        private int m_NumBuildCommits = 0;

        #endregion // Inspector

        private Routine m_TopBarRoutine;
        private Routine m_ReceiptRoutine;
        private Routine m_BuildButtonRoutine;
        private Routine m_PolicyBoxRoutine;
        private Routine m_BuildCommandLayoutRoutine;
        private Routine m_DestroyCommandLayoutRoutine;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            Game.Events.Register(GameEvents.BlueprintModeStarted, HandleStartBlueprintMode);
            Game.Events.Register(GameEvents.BlueprintModeEnded, HandleEndBlueprintMode);
            Game.Events.Register(GameEvents.RegionSwitched, HandleRegionSwitched);
            Game.Events.Register(GameEvents.PolicyTypeUnlocked, HandlePolicyTypeUnlocked);
            Game.Events.Register(GameEvents.GameLoaded, HandleGameLoaded);

            m_ReceiptGroup.alpha = 0;
            m_BuildingModeText.SetAlpha(0);
            m_BuildCommandLayout.alpha = 0;
            m_BuildCommandLayout.blocksRaycasts = false;
            m_DestroyCommandLayout.alpha = 0;
            m_DestroyCommandLayout.blocksRaycasts = false;

            m_DestroyCommandLayout.gameObject.SetActive(true);
            m_BuildUndoButton.gameObject.SetActive(false);

            m_BuildButton.onClick.AddListener(HandleBuildConfirmButtonClicked);
            m_BuildUndoButton.onClick.AddListener(HandleUndoBuildButtonClicked);
            m_DestroyUndoButton.onClick.AddListener(HandleUndoDestroyButtonClicked);
            m_DestroyModeButton.onClick.AddListener(HandleDestroyModeButtonClicked);
            m_DestroyConfirmButton.onClick.AddListener(HandleDestroyConfirmButtonClicked);
            m_DestroyExitButton.onClick.AddListener(HandleDestroyExitButtonClicked);

            foreach(var box in m_PolicyBoxes)
            {
                box.gameObject.SetActive(false);
                box.Popup.Group.alpha = 0;
            }
            m_MilkRevenueBox.gameObject.SetActive(true);
            m_MilkRevenueBox.Popup.Group.alpha = 0;

            return null;
        }

        private void OnEnable()
        {
            PolicyState policies = Game.SharedState.Get<PolicyState>();
            // policies.PolicyCardSelected.Register(HandlePolicyCardSelected);
            policies.OnPolicyUpdated.Register(HandlePolicyUpdated);

            UpdatePolicyBoxTexts();
            HandleRegionSwitched();
            m_BuildButton.gameObject.SetActive(false);
        }

        protected override void OnDestroy() {
            m_TopBarRoutine.Stop();
            base.OnDestroy();
        }

        public void UpdateTotalCost(int totalCost, int deltaCost, long playerFunds, int numCommits)
        {
            // Change total to new total
            if (totalCost == 0)
            {
                m_RunningCostText.text = "" + totalCost;
                m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(numCommits > 0));
                // Do not change receipt state (might still be 0 cost with destroy activated)
            }
            else if (totalCost > 0)
            {
                m_RunningCostText.text = "-" + totalCost;
                m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(true));
            }
            else
            {
                m_RunningCostText.text = "+" + Math.Abs(totalCost);
                m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(true));
            }


            // TODO: Flash delta cost animation

            // Update funds remaining
            m_FundsRemainingText.text = "" + (playerFunds - totalCost);
        }

        public void UpdateDefaultColor(int newRegion) {
            m_TBDefault = m_TBColorPerRegion[newRegion];
            m_TopBarRoutine.Replace(this, TopBarColorTransition());
        }

        #region UI Handlers

        private void HandleStartBlueprintMode()
        {
            var blueprintState = Game.SharedState.Get<BlueprintState>();
            blueprintState.StartBlueprintMode = true;
            ScriptUtility.Trigger(GameTriggers.BlueprintModeEntered);
        }

        private void HandleEndBlueprintMode()
        {
            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();
            bpState.ExitedBlueprintMode = true;
            ScriptUtility.Trigger(GameTriggers.BlueprintModeExited);
        }

        private void HandlePolicyTypeUnlocked()
        {
            BlueprintState bpState = Game.SharedState.Get<BlueprintState>();

            UpdatePolicyBoxTexts();

            if (!bpState.IsActive)
            {
                m_PolicyBoxRoutine.Replace(this, PolicyBoxAppearanceTransition(true));
            }
        }

        private void HandlePolicyCardSelected(CardData data)
        {
            UpdatePolicyBoxTexts();
        }

        private void HandlePolicyUpdated() {
            UpdatePolicyBoxTexts();
        }

        private void HandleRegionSwitched()
        {
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            UpdateDefaultColor(grid.CurrRegionIndex);
            UpdatePolicyBoxTexts(grid);
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

        public void OnStartBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(true));
            m_PolicyBoxRoutine.Replace(this, PolicyBoxAppearanceTransition(false));
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(true));
            Game.Gui.GetShared<GlobalAlertButton>().Hide();
        }

        public void OnExitedBlueprintMode()
        {
            m_TopBarRoutine.Replace(this, TopBarAppearanceTransition(false));
            m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(false));
            m_BuildButtonRoutine.Replace(this, BuildConfirmAppearanceTransition(true));
            m_PolicyBoxRoutine.Replace(this, PolicyBoxAppearanceTransition(true));
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(false));
            Game.Gui.GetShared<GlobalAlertButton>().Show();
        }

        public void OnBuildConfirmClicked()
        {
            // Exit blueprint mode
            m_ShopToggle.ManualAppear(false);
        }

        // Handle when number of build commits changes
        public void OnNumBuildCommitsChanged(int num)
        {
            m_BuildUndoButton.gameObject.SetActive(num > 0);
            m_ReceiptRoutine.Replace(this, ReceiptAppearanceTransition(num > 0));
            m_BuildButtonRoutine.Replace(this, BuildConfirmAppearanceTransition(num > 0));

            m_NumBuildCommits = num;
        }

        // Handle when number of destroy action commits changes
        public void OnNumDestroyActionsChanged(int num)
        {
            m_DestroyUndoButton.interactable = num > 0;
            m_DestroyConfirmButton.gameObject.SetActive(num > 0);
        }

        public void OnDestroyModeClicked()
        {
            m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(false));
            m_DestroyCommandLayoutRoutine.Replace(this, DestroyCommandAppearanceTransition(true));
            m_ShopToggle.gameObject.SetActive(false);
            m_BuildButtonRoutine.Replace(this, BuildConfirmAppearanceTransition(false));


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
            m_BuildButtonRoutine.Replace(this, BuildConfirmAppearanceTransition(m_NumBuildCommits > 0));

            Game.Events?.Dispatch(GameEvents.DestroyModeEnded);
        }

        public void OnBuildToolSelected()
        {
            //m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(false));
        }

        public void OnBuildToolDeselected()
        {
            //m_BuildCommandLayoutRoutine.Replace(this, BuildCommandAppearanceTransition(true));
        }

        public void OnMarketTickAdvanced(MarketData data, SimGridState grid) {

            int region = grid.CurrRegionIndex;
            if (data.SalesTaxHistory[region].Net.Count <= 0) return;

            // Show milk revenue popup (dummy policy box, not associated with a PolicyType)
            // POP instead of PEEK because this one is not needed for any graphs, and isn't synced with market ticks.
            if (data.MilkRevenueHistory[region].Net.TryPopFront(out int milkRev) && milkRev != 0) {
                PolicyBoxUtility.SetPopupAmt(m_MilkRevenueBox.Popup, milkRev);
                PolicyBoxUtility.PlayPopupRoutine(m_MilkRevenueBox);
            }  


            // Show new popups
            foreach (var box in m_PolicyBoxes)
            {
                int amt = 0;

                switch(box.PolicyType)
                {
                    case PolicyType.SalesTaxPolicy:
                        amt = data.SalesTaxHistory[region].LastChange();
                        PolicyBoxUtility.SetPopupAmt(box.Popup, amt);
                        break;
                    case PolicyType.ImportTaxPolicy:
                        amt = data.ImportTaxHistory[region].LastChange();
                        PolicyBoxUtility.SetPopupAmt(box.Popup, amt);
                        break;
                    case PolicyType.RunoffPolicy:
                        amt = data.PenaltiesHistory[region].LastChange();
                        // PolicyBoxUtility.SetPopupAmt(box.Popup, amt);
                        continue; // skip past playing animation, go to next policy
                    case PolicyType.SkimmingPolicy:
                        if (data.SkimmerCostHistory[region].Net.TryPopFront(out int skim)){
                            amt = skim;
                            PolicyBoxUtility.SetPopupAmt(box.Popup, amt);
                        }
                        break;
                    default:
                        break;
                }

                // Display animation
                if (amt != 0)
                {
                    PolicyBoxUtility.PlayPopupRoutine(box);
                }
            }
        }

        private void HandleGameLoaded() {
            CardsState cards = Game.SharedState.Get<CardsState>();

            foreach (var box in m_PolicyBoxes) {
                bool visible = CardsUtility.GetUnlockedOptions(cards, box.PolicyType).Count > 0;
                box.gameObject.SetActive(visible);
            }
            UpdatePolicyBoxTexts();
        }

        #endregion // System Handlers

        #region Routines

        private IEnumerator TopBarColorTransition() {
            yield return m_TopBarBG.ColorTo(m_TBDefault, 0.1f);
        }

        private IEnumerator TopBarAppearanceTransition(bool inBMode)
        {
            if (inBMode)
            {
                var shopState = Game.SharedState.Get<ShopState>();
                shopState.ManualUpdateRequested = true;

                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBDefault, 0.1f),
                    m_BuildingModeText.FadeTo(1, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(11, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(-7, .1f, Axis.Y, Space.Self),
                    m_PolicyBoxGroup.FadeTo(0, .1f)
                    );
            }
            else
            {
                yield return Routine.Combine(
                    m_TopBarBG.ColorTo(m_TBDefault, 0.1f),
                    m_BuildingModeText.FadeTo(0, .1f),
                    m_BuildingModeText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self),
                    m_RegionText.rectTransform.MoveTo(0, .1f, Axis.Y, Space.Self),
                    m_PolicyBoxGroup.FadeTo(1, .1f)
                    );
            }
        }

        private IEnumerator PolicyBoxAppearanceTransition(bool appearing)
        {
            CardsState state = Game.SharedState.Get<CardsState>();

            if (appearing)
            {
                // IEnumerator[] toFade = new IEnumerator[m_PolicyBoxes.Length];
                // toFade[0] = toFade[1] = toFade[2] = toFade[3] = null;

                // int enumerateIndex = 0;

                foreach (var box in m_PolicyBoxes)
                {
                    bool visible = CardsUtility.GetUnlockedOptions(state, box.PolicyType).Count > 0;

                    box.gameObject.SetActive(visible);
                    if (visible)
                    {
                        // toFade[enumerateIndex] = box.Group.FadeTo(1, 0.1f);
                        // enumerateIndex++;
                        yield return box.Group.FadeTo(1, 0.05f);
                    }

                    /*
                    yield return Routine.Combine(
                        toFade[0],
                        toFade[1],
                        toFade[2],
                        toFade[3]
                        );
                    */
                }
            }
            else
            {
                // IEnumerator[] toFade = new IEnumerator[m_PolicyBoxes.Length];
                // int enumerateIndex = 0;

                foreach (var box in m_PolicyBoxes)
                {
                    // toFade[enumerateIndex] = box.Group.FadeTo(0, 0.1f);
                    // enumerateIndex++;
                    yield return box.Group.FadeTo(0, 0.05f);
                }

                /*
                yield return Routine.Combine(
                    toFade[0],
                    toFade[1],
                    toFade[2],
                    toFade[3]
                    );
                */
            }
            UpdatePolicyBoxTexts();
        }

        private IEnumerator ReceiptAppearanceTransition(bool appearing)
        {
            if (appearing)
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

        private IEnumerator BuildConfirmAppearanceTransition(bool appearing)
        {
            m_BuildButton.gameObject.SetActive(appearing);
            yield return null;
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
            m_DestroyConfirmButton.gameObject.SetActive(false);
            if (appearing)
            {
                SetDestroyCommandLayoutInteractable(true);
                yield return m_DestroyCommandLayout.FadeTo(1, 0.1f);
            }
            else
            {
                SetDestroyCommandLayoutInteractable(false);
                yield return m_DestroyCommandLayout.FadeTo(0, 0.1f);
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

        private void UpdatePolicyBoxTexts(SimGridState grid = null)
        {
            PolicyState policies = Game.SharedState.Get<PolicyState>();
            if (grid == null) {
                grid = Game.SharedState.Get<SimGridState>();
            } 

            foreach (var box in m_PolicyBoxes)
            {
                PolicyBoxUtility.UpdateLevelText(policies, grid, box);
            }
        }

        #endregion // Helpers
    }
}