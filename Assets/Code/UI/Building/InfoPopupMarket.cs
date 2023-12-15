using System;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Economy;
using Zavala.Sim;
using static FieldDay.Scenes.PreloadManifest;

namespace Zavala.UI.Info {
    public class InfoPopupMarket : MonoBehaviour {
        public LayoutGroup Layout;
        public InfoPopupLocationRow[] LocationRows;
        public InfoPopupLocationColumn[] LocationCols;
        public GameObject[] Dividers;
    }

    static public class InfoPopupMarketUtility {
        static public readonly Color PositiveColorBG = Colors.Hex("#C8E295");
        static public readonly Color PositiveColor = Colors.Hex("#078313");

        static public readonly Color NeutralColor = Colors.Hex("#806844");

        static public readonly Color NegativeColorBG = Colors.Hex("#FFBBBB");
        static public readonly Color NegativeColor = Colors.Hex("#C23636");

        #region Load Columns

        static public void LoadLocationIntoCol(InfoPopupLocationColumn col, OccupiesTile location, OccupiesTile referenceLocation)
        {
            LocationDescription desc = location.GetComponent<LocationDescription>();

            col.NameLabel.SetText(Loc.Find(desc.TitleLabel));
            col.Icon.sprite = desc.Icon;

            if (location.IsExternal)
            {
                col.RegionLabel.gameObject.SetActive(true);
                col.RegionLabel.SetText(Loc.Find("region.external.name"));
                col.NameLabel.rectTransform.SetAnchorPos(col.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
            else if (referenceLocation != null && location.RegionIndex == referenceLocation.RegionIndex)
            {
                col.RegionLabel.gameObject.SetActive(false);
                col.NameLabel.rectTransform.SetAnchorPos(0, Axis.Y);
            }
            else
            {
                col.RegionLabel.gameObject.SetActive(true);
                col.RegionLabel.SetText(Loc.Find(RegionUtility.GetNameLong(location.RegionIndex)));
                col.NameLabel.rectTransform.SetAnchorPos(col.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
        }

        static public void LoadCostsIntoCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool isSecondary)
        {
             col.PriceGroup.SetActive(true);

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex]; // config.DefaultPurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];

            col.BasePriceCol.gameObject.SetActive(true);
            headers.BasePriceColHeader.gameObject.SetActive(true);
            col.BasePriceCol.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            col.ShippingCol.Number.SetText(shippingPrice.ToStringLookup());

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex)
            {
                import = 0;
            }
            else {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource]; // info.TaxRevenue.Import;
            }
            col.ImportTaxCol.gameObject.SetActive(true);
            headers.ImportTaxColHeader.gameObject.SetActive(true);
            col.ImportTaxCol.Number.SetText((import).ToStringLookup());

            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource]; // info.TaxRevenue.Sales;
            col.SalesTaxCol.gameObject.SetActive(true);
            headers.SalesTaxColHeader.gameObject.SetActive(true);
            col.SalesTaxCol.Number.SetText((salesTax).ToStringLookup());

            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(false);
            headers.PenaltyColHeader.gameObject.SetActive(false);
            col.PenaltyCol.Number.SetText((-penalties).ToStringLookup());

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            col.TotalProfitCol.gameObject.SetActive(false);
            headers.TotalProfitColHeader.gameObject.SetActive(false);
            col.TotalPriceCol.gameObject.SetActive(true);
            headers.TotalPriceColHeader.gameObject.SetActive(true);
            col.TotalPriceCol.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary)
            {
                // col.TotalPriceCol.Background.color = NegativeColorBG;
                col.TotalPriceCol.Number.color = NegativeColor;
            }
            else
            {
                // col.TotalPriceCol.Background.color = PositiveColorBG;
                col.TotalPriceCol.Number.color = PositiveColor;
            }

            //col.PriceLayout.ForceRebuild(true);
        }

        static public void LoadProfitIntoCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool isSecondary)
        {
            col.PriceGroup.SetActive(true);

            int basePrice;
            if (info.Requester.IsLocalOption)
            {
                basePrice = 0;
            }
            else if (info.Requester.OverridesBuyPrice)
            {
                int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
                basePrice = info.Requester.OverrideBlock[marketIndex];
            }
            else
            {
                int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
                basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex];
            }
            col.BasePriceCol.gameObject.SetActive(true);
            headers.BasePriceColHeader.gameObject.SetActive(true);
            col.BasePriceCol.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            col.ShippingCol.Number.SetText((-shippingPrice).ToStringLookup());

            int import = info.TaxRevenue.Import;
            col.ImportTaxCol.gameObject.SetActive(false);
            headers.ImportTaxColHeader.gameObject.SetActive(false);
            col.ImportTaxCol.Number.SetText((-import).ToStringLookup());

            int salesTax = info.TaxRevenue.Sales;
            col.SalesTaxCol.gameObject.SetActive(false);
            headers.SalesTaxColHeader.gameObject.SetActive(false);
            col.SalesTaxCol.Number.SetText((-salesTax).ToStringLookup());

            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(true);
            headers.PenaltyColHeader.gameObject.SetActive(true);
            col.PenaltyCol.Number.SetText((-penalties).ToStringLookup());

            int totalCost = info.Profit + (salesTax + import);
            col.TotalProfitCol.gameObject.SetActive(true);
            headers.TotalProfitColHeader.gameObject.SetActive(true);
            col.TotalPriceCol.gameObject.SetActive(false);
            headers.TotalPriceColHeader.gameObject.SetActive(false);
            col.TotalProfitCol.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary)
            {
                // col.TotalProfitCol.Background.color = NegativeColorBG;
                col.TotalProfitCol.Number.color = NegativeColor;
            }
            else
            {
                // col.TotalProfitCol.Background.color = PositiveColorBG;
                col.TotalProfitCol.Number.color = PositiveColor;
            }

            //col.PriceLayout.ForceRebuild(true);
        }

        static public void ClearPricesAtCol(InfoPopupLocationColumn col)
        {
            col.PriceGroup.SetActive(false);
        }

        #endregion // Load Columns

        #region Load Rows

        static public void LoadLocationIntoRow(InfoPopupLocationRow row, OccupiesTile location, OccupiesTile referenceLocation)
        {
            LocationDescription desc = location.GetComponent<LocationDescription>();

            row.NameLabel.SetText(Loc.Find(desc.TitleLabel));
            row.Icon.sprite = desc.Icon;

            if (location.IsExternal)
            {
                row.RegionLabel.gameObject.SetActive(true);
                row.RegionLabel.SetText(Loc.Find("region.external.name"));
                row.NameLabel.rectTransform.SetAnchorPos(row.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
            else if (referenceLocation != null && location.RegionIndex == referenceLocation.RegionIndex)
            {
                row.RegionLabel.gameObject.SetActive(false);
                row.NameLabel.rectTransform.SetAnchorPos(0, Axis.Y);
            }
            else
            {
                row.RegionLabel.gameObject.SetActive(true);
                row.RegionLabel.SetText(Loc.Find(RegionUtility.GetNameLong(location.RegionIndex)));
                row.NameLabel.rectTransform.SetAnchorPos(row.RegionLabel.rectTransform.sizeDelta.y / 2, Axis.Y);
            }
        }

        static public void LoadCostsIntoRow(InfoPopupLocationRow row, MarketQueryResultInfo info, MarketConfig config, bool isSecondary)
        {
            row.PriceGroup.SetActive(true);

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex]; // config.DefaultPurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];

            row.BasePriceRow.gameObject.SetActive(true);
            row.BasePriceRow.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            row.ShippingRow.gameObject.SetActive(shippingPrice > 0);
            if (shippingPrice > 0)
            {
                row.ShippingRow.Number.SetText(shippingPrice.ToStringLookup());
            }

            int import = info.TaxRevenue.Import;
            row.ImportTaxRow.gameObject.SetActive(import > 0);
            row.SubsidyRow.gameObject.SetActive(import < 0);
            if (import > 0)
            {
                row.ImportTaxRow.Number.SetText((-import).ToStringLookup());
            }
            else if (import < 0)
            {
                row.SubsidyRow.Number.SetText((-import).ToStringLookup());
            }

            int salesTax = info.TaxRevenue.Sales;
            row.SalesTaxRow.gameObject.SetActive(salesTax > 0);
            if (salesTax > 0)
            {
                row.SalesTaxRow.Number.SetText((-salesTax).ToStringLookup());
            }

            int penalties = info.TaxRevenue.Penalties;
            row.PenaltyRow.gameObject.SetActive(penalties > 0);
            if (penalties > 0)
            {
                row.PenaltyRow.Number.SetText((-penalties).ToStringLookup());
            }

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            row.TotalProfitRow.gameObject.SetActive(false);
            row.TotalPriceRow.gameObject.SetActive(true);
            row.TotalPriceRow.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary)
            {
                row.TotalPriceRow.Background.color = NegativeColorBG;
                row.TotalPriceRow.Number.color = NegativeColor;
            }
            else
            {
                row.TotalPriceRow.Background.color = PositiveColorBG;
                row.TotalPriceRow.Number.color = PositiveColor;
            }

            //row.PriceLayout.ForceRebuild(true);
        }

        static public void LoadProfitIntoRow(InfoPopupLocationRow row, MarketQueryResultInfo info, MarketConfig config, bool isSecondary)
        {
            row.PriceGroup.SetActive(true);

            int basePrice;
            if (info.Requester.IsLocalOption)
            {
                basePrice = 0;
            }
            else
            {
                int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
                basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex]; // config.DefaultPurchasePerRegion[info.Supplier.Position.RegionIndex].Buy[info.Resource];
            }
            row.BasePriceRow.gameObject.SetActive(basePrice > 0);
            row.BasePriceRow.Number.SetText(basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            row.ShippingRow.gameObject.SetActive(shippingPrice > 0);
            if (shippingPrice > 0)
            {
                row.ShippingRow.Number.SetText((-shippingPrice).ToStringLookup());
            }

            int import = info.TaxRevenue.Import;
            row.ImportTaxRow.gameObject.SetActive(import > 0);
            row.SubsidyRow.gameObject.SetActive(import < 0);
            if (import > 0)
            {
                row.ImportTaxRow.Number.SetText((-import).ToStringLookup());
            }
            else if (import < 0)
            {
                row.SubsidyRow.Number.SetText((-import).ToStringLookup());
            }

            // Commenting out for now - sales tax is applied to the buyer
            /*            
            int salesTax = info.TaxRevenue.Sales;
            row.SalesTaxRow.gameObject.SetActive(salesTax > 0);
            if (salesTax > 0) {
                row.SalesTaxRow.Number.SetText((-salesTax).ToStringLookup());
            }
            */

            int penalties = info.TaxRevenue.Penalties;
            row.PenaltyRow.gameObject.SetActive(penalties > 0);
            if (penalties > 0)
            {
                row.PenaltyRow.Number.SetText((-penalties).ToStringLookup());
            }

            int totalCost = info.Profit;
            row.TotalProfitRow.gameObject.SetActive(true);
            row.TotalPriceRow.gameObject.SetActive(false);
            row.TotalProfitRow.Number.SetText(totalCost.ToStringLookup());

            if (isSecondary)
            {
                row.TotalProfitRow.Background.color = NegativeColorBG;
                row.TotalProfitRow.Number.color = NegativeColor;
            }
            else
            {
                row.TotalProfitRow.Background.color = PositiveColorBG;
                row.TotalProfitRow.Number.color = PositiveColor;
            }

            //row.PriceLayout.ForceRebuild(true);
        }

        static public void ClearPricesAtRow(InfoPopupLocationRow row)
        {
            row.PriceGroup.SetActive(false);
        }

        #endregion // Load Rows

    }
}