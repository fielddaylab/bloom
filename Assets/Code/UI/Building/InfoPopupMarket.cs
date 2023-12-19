using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
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

        static public readonly string EmptyEntry = "-";
        static public readonly string MoneyPrefix = "$";


        static private readonly Color[] ColumnColors = new Color[5]
{
            Colors.Hex("#FFE9BD"),
            Colors.Hex("#FFE2A7"),
            Colors.Hex("#F8D99B"),
            Colors.Hex("#F8D99B"),
            Colors.Hex("#ECCD90")
        }
;
        static private readonly Color RowHighlightColor = Colors.Hex("#9CE978");
        static private readonly Color[] RowDefaultColor = new Color[2]
        {
            Colors.Hex("#C4AC90"),
            Colors.Hex("#FFFBE3")
        };

        #region Load Columns

        static public void LoadLocationIntoCol(InfoPopupLocationColumn col, OccupiesTile location, OccupiesTile referenceLocation, bool forSale, GameObject bestOptionBanner, int colGroupIndex)
        {
            bool evenCol = colGroupIndex % 2 == 0;
            LocationDescription desc = location.GetComponent<LocationDescription>();

            col.NameLabel.gameObject.SetActive(true);
            col.Icon.gameObject.SetActive(true);

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

            if (colGroupIndex == 0)
            {
                bestOptionBanner.SetActive(forSale);
                if (forSale)
                {
                    col.Background.SetColor(RowHighlightColor);
                }
                else
                {
                    col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                }
            }
            else
            {
                col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
            }
        }

        static public void LoadStorageCapacity(InfoPopupStorageCapacity storageGroup, int full, int total)
        {
            Assert.Equals(storageGroup.Slots.Length, total);

            for (int i = 0; i < storageGroup.Slots.Length; i++)
            {
                if (i < full)
                {
                    storageGroup.Slots[i].sprite = storageGroup.FullSprite;
                }
                else
                {
                    storageGroup.Slots[i].sprite = storageGroup.EmptySprite;
                }
            }
        }

        static private void SetMoneyText(TMP_Text toSet, string newText)
        {
            toSet.SetText(MoneyPrefix + newText);
        }

        static public void LoadEmptyCostsCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, GameObject bestOptionBanner, int colGroupIndex)
        {
            ActivateCostsHeaders(headers);

            col.SalesTaxCol.gameObject.SetActive(true);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex);
        }

        static public void LoadEmptyProfitCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, GameObject bestOptionBanner, int colGroupIndex)
        {
            ActivateProfitHeaders(headers);

            col.SalesTaxCol.gameObject.SetActive(false);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex);
        }

        static private void LoadEmptyCol(InfoPopupLocationColumn col, GameObject bestOptionBanner, int colGroupIndex)
        {
            bool evenCol = colGroupIndex % 2 == 0;

            col.RegionLabel.gameObject.SetActive(false);
            col.NameLabel.gameObject.SetActive(false);
            col.Icon.gameObject.SetActive(false);
            for (int i = 0; i < col.PriceCols.Length; i++)
            {
                col.PriceCols[i].Number.SetText(EmptyEntry);
            }

            col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);

            if (colGroupIndex == 0)
            {
                bestOptionBanner.SetActive(false);
            }
        }


        static public void LoadCostsIntoCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool forSale, bool isSecondary)
        {
             col.PriceGroup.SetActive(true);

            ActivateCostsHeaders(headers);

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex];

            col.BasePriceCol.gameObject.SetActive(true);
            SetMoneyText(col.BasePriceCol.Number, basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, shippingPrice.ToStringLookup());

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex)
            {
                import = 0;
            }
            else {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource];
            }
            col.ImportTaxCol.gameObject.SetActive(true);
            if (import == 0) { col.ImportTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.ImportTaxCol.Number, import.ToStringLookup()); }

            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource];
            col.SalesTaxCol.gameObject.SetActive(true);
            if (salesTax == 0) { col.SalesTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.SalesTaxCol.Number, salesTax.ToStringLookup()); }

            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(false);
            if (penalties == 0) { col.PenaltyCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.PenaltyCol.Number, penalties.ToStringLookup()); }

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            col.TotalProfitCol.gameObject.SetActive(false);
            col.TotalPriceCol.gameObject.SetActive(true);
            SetMoneyText(col.TotalPriceCol.Number, totalCost.ToStringLookup());

            if (isSecondary)
            {
                col.TotalPriceCol.Number.color = NegativeColor;
            }
            else
            {
                col.TotalPriceCol.Number.color = PositiveColor;
                if (forSale)
                {
                    col.TotalPriceCol.Number.fontStyle = FontStyles.Underline;
                }
                else
                {
                    col.TotalPriceCol.Number.fontStyle = FontStyles.Normal;
                }
            }
        }

        static public void LoadProfitIntoCol(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool forSale, bool isSecondary)
        {
            col.PriceGroup.SetActive(true);
            ActivateProfitHeaders(headers);

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
            SetMoneyText(col.BasePriceCol.Number, basePrice.ToStringLookup());

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, (-shippingPrice).ToStringLookup());

            int import = info.TaxRevenue.Import;
            col.ImportTaxCol.gameObject.SetActive(false);
            if (import == 0) { col.ImportTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.ImportTaxCol.Number, (-import).ToStringLookup()); }

            int salesTax = info.TaxRevenue.Sales;
            col.SalesTaxCol.gameObject.SetActive(false);
            if (salesTax == 0) { col.SalesTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.SalesTaxCol.Number, (-salesTax).ToStringLookup()); }

            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(true);
            if (penalties == 0) { col.PenaltyCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.PenaltyCol.Number, (-penalties).ToStringLookup() + " " + Loc.Find("ui.popup.info.penaltyFine")); }

            int totalCost = info.Profit + (salesTax + import);
            col.TotalProfitCol.gameObject.SetActive(true);
            col.TotalPriceCol.gameObject.SetActive(false);
            SetMoneyText(col.TotalProfitCol.Number, totalCost.ToStringLookup());

            if (isSecondary)
            {
                col.TotalProfitCol.Number.color = NegativeColor;
            }
            else
            {
                col.TotalProfitCol.Number.color = PositiveColor;
                if (forSale)
                {
                    col.TotalProfitCol.Number.fontStyle = FontStyles.Underline;
                }
                else
                {
                    col.TotalProfitCol.Number.fontStyle = FontStyles.Normal;
                }
            }
        }

        static private void ActivateProfitHeaders(InfoPopupColumnHeaders headers)
        {
            headers.BasePriceColHeader.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            headers.ImportTaxColHeader.gameObject.SetActive(false);
            headers.SalesTaxColHeader.gameObject.SetActive(false);
            headers.PenaltyColHeader.gameObject.SetActive(true);
            headers.TotalProfitColHeader.gameObject.SetActive(true);
            headers.TotalPriceColHeader.gameObject.SetActive(false);
        }

        static private void ActivateCostsHeaders(InfoPopupColumnHeaders headers)
        {
            headers.BasePriceColHeader.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            headers.ImportTaxColHeader.gameObject.SetActive(true);
            headers.SalesTaxColHeader.gameObject.SetActive(true);
            headers.PenaltyColHeader.gameObject.SetActive(false);
            headers.TotalProfitColHeader.gameObject.SetActive(false);
            headers.TotalPriceColHeader.gameObject.SetActive(true);
        }

        static public void AssignColColors(InfoPopupColumnHeaders headers)
        {
            int colorIndex = 0;
            for (int i = 0; i < headers.PriceHeaders.Length; i++)
            {
                if (headers.PriceHeaders[i].gameObject.activeSelf)
                {
                    headers.PriceHeaders[i].Background.SetColor(ColumnColors[colorIndex]);
                    colorIndex++;
                }
            }
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