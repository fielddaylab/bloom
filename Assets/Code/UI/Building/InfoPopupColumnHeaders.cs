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
        public InfoPopupPriceRow BasePriceColHeader;
        public InfoPopupPriceRow ShippingColHeader;
        public InfoPopupPriceRow SalesTaxColHeader;
        public InfoPopupPriceRow ImportTaxColHeader;
        public InfoPopupPriceRow PenaltyColHeader;
        public InfoPopupPriceRow TotalPriceColHeader;
        public InfoPopupPriceRow TotalProfitColHeader;
    }
}