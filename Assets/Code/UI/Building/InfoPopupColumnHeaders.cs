using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.UI.Info;

namespace Zavala.UI.Info
{
    public class InfoPopupColumnHeaders : MonoBehaviour
    {
        public RectTransform Root;
        public LayoutGroup Layout;
        public RectTransform LayoutRect;

        public InfoPopupColumnHeader BasePriceColHeader;
        public InfoPopupColumnHeader ShippingColHeader;
        public InfoPopupColumnHeader SalesTaxColHeader;
        public InfoPopupColumnHeader ImportTaxColHeader;
        public InfoPopupColumnHeader PenaltyColHeader;
        public InfoPopupColumnHeader TotalPriceColHeader;
        public InfoPopupColumnHeader TotalProfitColHeader;

        public InfoPopupColumnHeader[] PriceHeaders;
    }
}