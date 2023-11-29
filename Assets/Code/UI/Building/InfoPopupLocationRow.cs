using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Info {
    public class InfoPopupLocationRow : MonoBehaviour {
        public Image Icon;
        public TMP_Text NameLabel;
        public TMP_Text RegionLabel;

        [Header("Prices")]
        public GameObject PriceGroup;
        public LayoutGroup PriceLayout;
        public InfoPopupPriceRow BasePriceRow;
        public InfoPopupPriceRow ShippingRow;
        public InfoPopupPriceRow SalesTaxRow;
        public InfoPopupPriceRow ImportTaxRow;
        public InfoPopupPriceRow SubsidyRow;
        public InfoPopupPriceRow PenaltyRow;
        public InfoPopupPriceRow TotalPriceRow;
    }
}