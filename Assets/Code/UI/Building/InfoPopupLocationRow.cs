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
        public InfoPopupPriceRow[] PriceRows;
        public InfoPopupPriceRow TotalPriceRow;
    }
}