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
        public RectTransform BackgroundRect;
        public TMP_Text NameLabel;
        public RectTransform NameLabelRect;
        public TMP_Text RegionLabel;
        public Image BuyArrow; // or Sell Arrow
        public TMP_Text BuyArrowText; // or Sell Arrow Text
        public Image ElseArrow;
        public TMP_Text ElseArrowText;
        public GameObject SoldOutGroup;
        public TMP_Text SoldToText;


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