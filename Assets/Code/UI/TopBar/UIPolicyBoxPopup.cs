using BeauUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI {
    public class UIPolicyBoxPopup : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_Text AmountText;
        [SerializeField] private Graphic AmountBG;
        [SerializeField] private LayoutGroup Layout;

        public CanvasGroup Group;

        #endregion // Inspector


        public void SetPopupAmt(int amt)
        {
            if (amt == 0)
            {
                return;
            }

            if (amt > 0)
            {
                AmountText.text = "+$" + amt;
                AmountBG.color = ZavalaColors.TopBarPopupPlus;
            }
            else
            {
                AmountText.text = "-$" + (-amt);
                AmountBG.color = ZavalaColors.TopBarPopupMinus;
            }

            Layout.ForceRebuild(true);
        }
    }
}