using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.UI.Info {
    public class InfoPopupMarket : MonoBehaviour {
        public LayoutGroup Layout;
        public InfoPopupLocationRow[] Locations;
        public GameObject[] Dividers;
    }

    static public class InfoPopupMarketUtility {
        static public void LoadLocationIntoRow(InfoPopupLocationRow row, OccupiesTile location, OccupiesTile referenceLocation) {
            // TODO: Locate icon
            // TODO: Locate name

            if (referenceLocation != null && location.RegionIndex == referenceLocation.RegionIndex) {
                row.RegionLabel.gameObject.SetActive(false);
                row.NameLabel.rectTransform.SetAnchorPos(0, Axis.Y);
            } else {
                row.RegionLabel.gameObject.SetActive(true);
                // TODO: Locate region name
                row.NameLabel.rectTransform.SetAnchorPos(row.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
        }
    }
}