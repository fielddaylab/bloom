using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Info
{
    public class InfoPopupLocationColumn : MonoBehaviour
    {
        public Image Icon;
        public TMP_Text NameLabel;
        public TMP_Text RegionLabel;

        [Header("Prices")]
        public GameObject PriceGroup;
        public LayoutGroup PriceLayout;
        public InfoPopupPriceRow BasePriceCol;
        public InfoPopupPriceRow ShippingCol;
        public InfoPopupPriceRow SalesTaxCol;
        public InfoPopupPriceRow ImportTaxCol;
        public InfoPopupPriceRow SubsidyCol;
        public InfoPopupPriceRow PenaltyCol;
        public InfoPopupPriceRow TotalPriceCol;
        public InfoPopupPriceRow TotalProfitCol;
    }
}