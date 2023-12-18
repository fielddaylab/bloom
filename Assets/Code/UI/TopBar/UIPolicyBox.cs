using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIPolicyBox : MonoBehaviour {
        private const float DISPLAY_TIME = 1.4f;

        #region Inspector

        [SerializeField] private Button m_Button;
        public Graphic NotSetHighlight;

        public CanvasGroup Group;
        public TMP_Text LevelText;
        public UIPolicyBoxPopup Popup;

        #endregion // Inspector

        public PolicyType PolicyType;

        public Routine PopupRoutine;

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
            m_Button.onClick.AddListener(() => HandlePolicyBoxClicked(advisorType));
            
        }

        #region Handlers

        private void HandlePolicyBoxClicked(AdvisorType type) {
            ScriptUtility.AutoOpenPolicy(PolicyType);

            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("advisorType", type.ToString());
                ScriptUtility.Trigger(GameTriggers.AdvisorOpened, varTable);
            }

            AdvisorState advisorState = Game.SharedState.Get<AdvisorState>();
            advisorState.AdvisorButtonClicked?.Invoke(type);

        }

        #endregion

        #region Routines

        public IEnumerator DisplayPopupRoutine()
        {
            Popup.Group.alpha = 0;

            yield return Routine.Combine(
                Popup.Group.FadeTo(1, .1f)
                );

            yield return DISPLAY_TIME;

            yield return Routine.Combine(
                Popup.Group.FadeTo(0, .1f)
                );
        }

        #endregion // Routines
    }

    static public class PolicyBoxUtility
    {
        static public void PlayPopupRoutine(UIPolicyBox box)
        {
            box.PopupRoutine.Replace(box.DisplayPopupRoutine());
        }

        static public void UpdateLevelText(PolicyState policyState, SimGridState grid, UIPolicyBox box)
        {
            if (policyState.Policies[grid.CurrRegionIndex].EverSet[box.PolicyType]) {
                PolicyLevel level = policyState.Policies[grid.CurrRegionIndex].Map[box.PolicyType];
                box.LevelText.text = Loc.Find("cards." + box.PolicyType.ToString() + "." + level.ToString().ToLower());
                box.NotSetHighlight.gameObject.SetActive(false);
            } else {
                box.LevelText.text = Loc.Find("cards.severity.notset");
                box.NotSetHighlight.gameObject.SetActive(true);
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
    }
}