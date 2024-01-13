using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Economy;
using Zavala.Sim;

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

        static public readonly Color InactiveNumberColor = Colors.Hex("#B58B4C");
        static public readonly Color ActiveNumberColor = Colors.Hex("#763813");

        static public readonly float InactiveAlpha = 0.65f;
        static public readonly int ActiveAlpha = 1;

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
        static private readonly Color RowPenaltyColor = Colors.Hex("#E62E2E");
        static private readonly Color[] RowDefaultColor = new Color[2]
        {
            Colors.Hex("#C4AC90"),
            Colors.Hex("#FFFBE3")
        };

        #region Load Columns

        static public void LoadLocationIntoCol(InfoPopupLocationColumn col, OccupiesTile location, OccupiesTile referenceLocation, bool forSale, bool runoffAffected, GameObject bestOptionBanner, int colGroupIndex)
        {
            bool evenCol = colGroupIndex % 2 == 0;
            LocationDescription desc = location.GetComponent<LocationDescription>();

            col.NameLabel.gameObject.SetActive(true);
            // col.Icon.gameObject.SetActive(true);
            col.Arrow.gameObject.SetActive(true);
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
                    // check if runoff penalty
                    if (runoffAffected)
                    {
                        col.Background.SetColor(RowPenaltyColor);
                    }
                    else
                    {
                        col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                    }
                }
            }
            else
            {
                // check if runoff penalty
                if (runoffAffected)
                {
                    col.Background.SetColor(RowPenaltyColor);
                }
                else
                {
                    col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                }
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

        static private void SetMoneyText(TMP_Text toSet, int val, string suffix = "")
        {
            string prefix = "";
            if (val < 0) {
                val *= -1;
                prefix += "-";
            }
            prefix += MoneyPrefix;
            toSet.SetText(prefix + val.ToStringLookup() + suffix);
        }

        /// <summary>
        /// Sets a text obejct to show shipping calculation
        /// </summary>
        /// <param name="toSet">Text to set</param>
        /// <param name="distance">Distance of this shipping cost</param>
        /// <param name="total">Total shipping cost</param>
        static private void SetShippingRateText(TMP_Text toSet, int distance, int total) {
            if (distance == 0) {
                Log.Warn("[InfoPopup] Error setting shipping rate text: distance of 0!");
                toSet.gameObject.SetActive(false);
                return;
            }
            int rate = total / distance;
            toSet.SetText(distance.ToStringLookup() + " × " + MoneyPrefix + rate.ToStringLookup());
            toSet.gameObject.SetActive(true);
        }

        static public void LoadEmptyCostsCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, GameObject bestOptionBanner, int colGroupIndex)
        {
            ActivateCostsHeaders(headers, policyState, grid);

            col.SalesTaxCol.gameObject.SetActive(true);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex);
        }

        static public void LoadEmptyProfitCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, GameObject bestOptionBanner, int colGroupIndex)
        {
            ActivateProfitHeaders(headers, policyState, grid);

            col.SalesTaxCol.gameObject.SetActive(false);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex);
        }

        static private void LoadEmptyCol(InfoPopupLocationColumn col, GameObject bestOptionBanner, int colGroupIndex)
        {
            bool evenCol = colGroupIndex % 2 == 0;

            col.PolicyIcon.gameObject.SetActive(false);
            col.Arrow.gameObject.SetActive(false);
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


        static public void LoadCostsIntoCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool forSale, bool isSecondary)
        {
             col.PriceGroup.SetActive(true);

            ActivateCostsHeaders(headers, policyState, grid);

            col.PolicyIcon.gameObject.SetActive(false);

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex];

            col.BasePriceCol.gameObject.SetActive(true);
            SetMoneyText(col.BasePriceCol.Number, basePrice);

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, shippingPrice);

            //int distance = info.Distance;
            //SetShippingRateText(col.ShippingCol.Detail, distance, shippingPrice);

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex && !info.Supplier.Position.IsExternal)
            {
                import = 0;
            }
            else {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource];
            }

            col.ImportTaxCol.Number.color = ActiveNumberColor;
            col.ImportTaxCol.gameObject.SetActive(true);
            if (import == 0)
            {
                bool importActive = policyState.Policies[grid.CurrRegionIndex].Map[Advisor.PolicyType.ImportTaxPolicy] != PolicyLevel.None;
                if (importActive)
                {
                    SetMoneyText(col.ImportTaxCol.Number, import);
                    col.ImportTaxCol.Number.color = InactiveNumberColor;
                }
                else
                {
                    col.ImportTaxCol.Number.SetText(EmptyEntry);
                }
            }
            else if (import < 0) {
                SetMoneyText(col.ImportTaxCol.Number, import, " " + Loc.Find("ui.popup.info.subsidyBlurb"));
            }
            else
            {
                SetMoneyText(col.ImportTaxCol.Number, import, " " + Loc.Find("ui.popup.info.taxBlurb"));
            }

            col.SalesTaxCol.Number.color = ActiveNumberColor;
            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource];
            col.SalesTaxCol.gameObject.SetActive(true);
            if (salesTax == 0) {
                bool salesActive = policyState.Policies[grid.CurrRegionIndex].Map[Advisor.PolicyType.SalesTaxPolicy] != PolicyLevel.None;
                if (salesActive)
                {
                    SetMoneyText(col.SalesTaxCol.Number, salesTax);
                    col.SalesTaxCol.Number.color = InactiveNumberColor;
                }
                else
                {
                    col.SalesTaxCol.Number.SetText(EmptyEntry);
                }
            }
            else if (salesTax < 0)
            {
                SetMoneyText(col.SalesTaxCol.Number, salesTax, " " + Loc.Find("ui.popup.info.subsidyBlurb"));
            }
            else
            {
                SetMoneyText(col.SalesTaxCol.Number, salesTax, " " + Loc.Find("ui.popup.info.taxBlurb"));
            }

            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(false);
            if (penalties == 0) { 
                col.PenaltyCol.Number.SetText(EmptyEntry);
            }
            else { 
                SetMoneyText(col.PenaltyCol.Number, penalties);
            }

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            col.TotalProfitCol.gameObject.SetActive(false);
            col.TotalPriceCol.gameObject.SetActive(true);
            SetMoneyText(col.TotalPriceCol.Number, totalCost);

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

        static public void LoadProfitIntoCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool forSale, bool isSecondary)
        {
            col.PriceGroup.SetActive(true);
            ActivateProfitHeaders(headers, policyState, grid);

            col.SalesTaxCol.Number.color = ActiveNumberColor;
            col.PolicyIcon.gameObject.SetActive(false);

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
            SetMoneyText(col.BasePriceCol.Number, basePrice);

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, -shippingPrice);

            //int distance = info.Distance;
            //SetShippingRateText(col.ShippingCol.Detail, distance, shippingPrice);

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex)
            {
                import = 0;
            }
            else
            {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource];
            }
            col.ImportTaxCol.gameObject.SetActive(false);
            if (import == 0) { col.ImportTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.ImportTaxCol.Number, (-import)); }

            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource];
            col.SalesTaxCol.gameObject.SetActive(false);
            if (salesTax == 0) { col.SalesTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.SalesTaxCol.Number, (-salesTax)); }

            col.PenaltyCol.Number.color = ActiveNumberColor;
            int penalties = info.TaxRevenue.Penalties;
            col.PenaltyCol.gameObject.SetActive(true);
            if (penalties == 0) { 
                col.PenaltyCol.Number.SetText(EmptyEntry);
                col.PenaltyCol.Number.color = InactiveNumberColor;
            }
            else {
                SetMoneyText(col.PenaltyCol.Number, (-penalties), " " + Loc.Find("ui.popup.info.penaltyFine"));
                //col.PolicyIcon.gameObject.SetActive(true);
            }

            int totalCost = info.Profit + (salesTax + import);
            col.TotalProfitCol.gameObject.SetActive(true);
            col.TotalPriceCol.gameObject.SetActive(false);
            SetMoneyText(col.TotalProfitCol.Number, totalCost);

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

        static private void ActivateProfitHeaders(InfoPopupColumnHeaders headers, PolicyState policyState, SimGridState grid)
        {
            headers.BasePriceColHeader.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            headers.ImportTaxColHeader.gameObject.SetActive(false);
            headers.SalesTaxColHeader.gameObject.SetActive(false);
            headers.PenaltyColHeader.gameObject.SetActive(true);
            headers.TotalProfitColHeader.gameObject.SetActive(true);
            headers.TotalPriceColHeader.gameObject.SetActive(false);

            bool penaltyActive = policyState.Policies[grid.CurrRegionIndex].Map[Advisor.PolicyType.RunoffPolicy] != PolicyLevel.None;
            if (penaltyActive) {
                headers.PenaltyColHeader.Icon.SetAlpha(ActiveAlpha);
                headers.PenaltyColHeader.Text.SetAlpha(ActiveAlpha);
            }
            else { 
                headers.PenaltyColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.PenaltyColHeader.Text.SetAlpha(InactiveAlpha);
            }
        }

        static private void ActivateCostsHeaders(InfoPopupColumnHeaders headers, PolicyState policyState, SimGridState grid)
        {
            headers.BasePriceColHeader.gameObject.SetActive(true);
            headers.ShippingColHeader.gameObject.SetActive(true);
            headers.ImportTaxColHeader.gameObject.SetActive(true);
            headers.SalesTaxColHeader.gameObject.SetActive(true);
            headers.PenaltyColHeader.gameObject.SetActive(false);
            headers.TotalProfitColHeader.gameObject.SetActive(false);
            headers.TotalPriceColHeader.gameObject.SetActive(true);

            bool importActive = policyState.Policies[grid.CurrRegionIndex].Map[Advisor.PolicyType.ImportTaxPolicy] != PolicyLevel.None;
            if (importActive) {
                headers.ImportTaxColHeader.Icon.SetAlpha(ActiveAlpha); 
                headers.ImportTaxColHeader.Text.SetAlpha(ActiveAlpha);
            }
            else { 
                headers.ImportTaxColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.ImportTaxColHeader.Text.SetAlpha(InactiveAlpha);
            }

            bool salesActive = policyState.Policies[grid.CurrRegionIndex].Map[Advisor.PolicyType.SalesTaxPolicy] != PolicyLevel.None;
            if (salesActive) { 
                headers.SalesTaxColHeader.Icon.SetAlpha(ActiveAlpha); 
                headers.SalesTaxColHeader.Text.SetAlpha(ActiveAlpha); 
            }
            else { 
                headers.SalesTaxColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.SalesTaxColHeader.Text.SetAlpha(InactiveAlpha);
            }
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

        

        static public void ClearPricesAtRow(InfoPopupLocationRow row)
        {
            row.PriceGroup.SetActive(false);
        }

        #endregion // Load Rows

    }
}