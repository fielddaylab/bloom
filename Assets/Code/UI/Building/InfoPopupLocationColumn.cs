using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Info
{
    public class InfoPopupLocationColumn : MonoBehaviour
    {
        public Image Icon;
        public Image PolicyIcon;
        public Graphic Background;
        public TMP_Text NameLabel;
        public TMP_Text RegionLabel;

        [Header("Prices")]
        public GameObject PriceGroup;
        public LayoutGroup PriceLayout;
        public InfoPopupPriceRow BasePriceCol;
        public InfoPopupPriceRow ShippingCol;
        public InfoPopupPriceRow SalesTaxCol;
        public InfoPopupPriceRow ImportTaxCol;
        public InfoPopupPriceRow PenaltyCol;
        public InfoPopupPriceRow TotalPriceCol;
        public InfoPopupPriceRow TotalProfitCol;

        public InfoPopupPriceRow[] PriceCols;
    }
}