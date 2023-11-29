using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;

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

        static public void LoadPricesIntoRow(InfoPopupLocationRow row, OccupiesTile location, MarketQueryResultInfo info, MarketConfig config) {
            row.PriceGroup.SetActive(false);

            int basePrice = config.PurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];
            //row.BasePriceRow.gameObject.SetActive(true);
            row.BasePriceRow.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = config.TransportCosts.CostPerTile[info.Resource] * info.Distance;
            row.ShippingRow.gameObject.SetActive(shippingPrice > 0);
            if (shippingPrice > 0) {
                row.ShippingRow.Number.SetText(shippingPrice.ToStringLookup());
            }

            int import = info.TaxRevenue.Import;
            row.ImportTaxRow.gameObject.SetActive(import > 0);
            row.SubsidyRow.gameObject.SetActive(import < 0);
            if (import > 0) {
                row.ImportTaxRow.Number.SetText((-import).ToStringLookup());
            } else if (import < 0) {
                row.SubsidyRow.Number.SetText((-import).ToStringLookup());
            }

            int salesTax = info.TaxRevenue.Sales;
            row.SalesTaxRow.gameObject.SetActive(salesTax > 0);
            if (salesTax > 0) {
                row.SalesTaxRow.Number.SetText((-salesTax).ToStringLookup());
            }

            int penalties = info.TaxRevenue.Penalties;
            row.PenaltyRow.gameObject.SetActive(penalties > 0);
            if (penalties > 0) {
                row.PenaltyRow.Number.SetText((-penalties).ToStringLookup());
            }

            row.TotalPriceRow.Number.SetText(info.Profit.ToStringLookup());
        }
    }
}