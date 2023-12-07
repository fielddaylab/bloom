using BeauRoutine;
using BeauUtil;
using FieldDay;
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
    public class UIPolicyBox : MonoBehaviour
    {
        private const float DISPLAY_TIME = 1.4f;

        #region Inspector

        public CanvasGroup Group;
        public TMP_Text LevelText;
        public UIPolicyBoxPopup Popup;

        #endregion // Inspector

        public PolicyType PolicyType;

        public Routine PopupRoutine;


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
            PolicyLevel level = policyState.Policies[grid.CurrRegionIndex].Map[box.PolicyType];

            string newString;
            if (level == PolicyLevel.Alt) {
                newString = Loc.Find("cards." + box.PolicyType.ToString() + "." + level.ToString().ToLower());

            } else {
                newString = Loc.Find("cards.severity." + level.ToString().ToLower());
            }

            box.LevelText.text = newString;
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