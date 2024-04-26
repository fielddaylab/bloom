using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIPolicyBox : MonoBehaviour {
        private const float DISPLAY_TIME = 1.4f;

        #region Inspector

        public Button Button;
        public Graphic NotSetHighlight;

        public CanvasGroup Group;
        public TMP_Text LevelText;
        public UIPolicyBoxPopup Popup;

        #endregion // Inspector

        public PolicyType PolicyType;

        public Routine PopupRoutine;
        public Routine FlashRoutine;

        private void Start() {
            AdvisorType advisorType = AdvisorType.None;
            switch (PolicyType){ 
                case PolicyType.SalesTaxPolicy:
            case PolicyType.ImportTaxPolicy:
                advisorType = AdvisorType.Economy;
                break;
            case PolicyType.SkimmingPolicy:
            case PolicyType.RunoffPolicy:
                advisorType = AdvisorType.Ecology;
                break;
            default:
                break;
            }
            Button.onClick.AddListener(() => HandlePolicyBoxClicked(advisorType));
            
        }

        private void OnDestroy() {
            FlashRoutine.Stop();
            PopupRoutine.Stop();
        }

        #region Handlers

        private void HandlePolicyBoxClicked(AdvisorType type) {
            ScriptUtility.AutoOpenPolicyCards(PolicyType);

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("advisorType", type.ToString());
                ScriptUtility.Trigger(GameTriggers.AdvisorOpened, varTable);
            }

            // ZavalaGame.Events.Dispatch(GameEvents.AdvisorButtonClicked, type);
            ZavalaGame.Events.Dispatch(GameEvents.PolicyButtonClicked, PolicyType);
            //AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            //advisorState.AdvisorButtonClicked?.Invoke(type);

        }

        #endregion

        #region Routines

        public IEnumerator DisplayPopupRoutine(UIPolicyBoxPopup popup)
        {
            popup.Group.alpha = 0;

            yield return Routine.Combine(
                popup.Group.FadeTo(1, .1f)
                );

            yield return DISPLAY_TIME;

            yield return Routine.Combine(
                popup.Group.FadeTo(0, .1f)
                );
        }

        #endregion // Routines
    }

    static public class PolicyBoxUtility
    {
        static public void PlayPopupRoutine(UIPolicyBoxPopup popup)
        {
            popup.PopupRoutine.Replace(popup.DisplayPopupRoutine(popup)).ExecuteWhileDisabled();
        }

        static public void UpdateLevelText(PolicyState policyState, SimGridState grid, UIPolicyBox box)
        {
            if (!box.gameObject.activeSelf) return;

            if (policyState.Policies[grid.CurrRegionIndex].EverSet[(int) box.PolicyType]) {
                PolicyLevel level = policyState.Policies[grid.CurrRegionIndex].Map[(int) box.PolicyType];
                box.LevelText.text = Loc.Find("cards." + box.PolicyType.ToString() + "." + level.ToString().ToLower());
                box.FlashRoutine.Stop();
                box.NotSetHighlight.gameObject.SetActive(false);
            } else {
                box.LevelText.text = Loc.Find("cards.severity.notset");
                box.NotSetHighlight.gameObject.SetActive(true);
                box.FlashRoutine.Replace(FlashNotSetRoutine(box));
            }
        }

        static public void SetPopupAmt(UIPolicyBoxPopup popup, int amt)
        {
            if (amt == 0)
            {
                return;
            }

            if (amt > 0)
            {
                popup.AmountText.text = "+$" + amt;
                popup.AmountBG.color = ZavalaColors.TopBarPopupPlus;
            }
            else
            {
                popup.AmountText.text = "-$" + (-amt);
                popup.AmountBG.color = ZavalaColors.TopBarPopupMinus;
            }

            popup.Layout.ForceRebuild(true);
        }

        static public IEnumerator FlashNotSetRoutine(UIPolicyBox box) {
            while (box != null) {
                yield return box.NotSetHighlight.FadeTo(0.1f, 0.4f).Ease(Curve.CubeInOut);
                yield return box.NotSetHighlight.FadeTo(0.7f, 0.4f).Ease(Curve.CubeInOut);
            }
        }

        static public void SetInteractable(UIPolicyBox box, bool interactable)
        {
            box.Button.interactable = interactable;
        }
    }
}