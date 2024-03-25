using System;
using System.Collections;
using System.Collections.Generic;
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
        static public readonly Color PositiveColorBG = Colors.Hex("#60F5AD");
        static public readonly Color PositiveColor = Colors.Hex("#078313");

        static public readonly Color NeutralColor = Colors.Hex("#806844");

        static public readonly Color NegativeColorBG = Colors.Hex("#F0B59F");
        static public readonly Color NegativeColor = Colors.Hex("#C23636");

        static public readonly Color InactiveNumberColor = Colors.Hex("#B58B4C");
        static public readonly Color ActiveNumberColor = Colors.Hex("#763813");

        static public readonly float InactiveAlpha = 0.65f;
        static public readonly int ActiveAlpha = 1;

        static public readonly string EmptyEntry = "-";
        static public readonly string MoneyPrefix = "$";

        static private readonly float ColWidth = 55f;


        static private readonly Color[] ColumnColors = new Color[5]
{
            Colors.Hex("#F4EECE"), // Colors.Hex("#FFE9BD"),
            Colors.Hex("#F4EECE"), //Colors.Hex("#FFE2A7"),
            Colors.Hex("#F4EECE"), //Colors.Hex("#F8D99B"),
            Colors.Hex("#F4EECE"), //Colors.Hex("#F8D99B"),
            Colors.Hex("#F4EECE") //Colors.Hex("#ECCD90")
        }
;
        static private readonly Color RowHighlightBuyColor = Colors.Hex("#4ADEFF");
        static private readonly Color RowHighlightSellColor = Colors.Hex("#F6DA76");
        static private readonly Color RowPenaltyColor = Colors.Hex("#E62E2E");
        static private readonly Color[] RowDefaultColor = new Color[2]
        {
            Colors.Hex("#C4AC90"),
            Colors.Hex("#FFFBE3")
        };

        #region Load Columns

        static public List<InspectorLocationQuery> GatherLocationGroupsForResourceTab(InfoPopupLocationColumn[] cols, int maxRows, int numFilledRows, RingBuffer<MarketQueryResultInfo> queryResults, List<InspectorLocationQuery> locations, bool isShipping)
        {
            var newTabLocations = new List<InspectorLocationQuery>();

            for (int i = 0; i < maxRows; i++)
            {
                if (i < numFilledRows)
                {
                    var results = queryResults[i];
                    if (isShipping)
                    {
                        var query = InfoPopupMarketUtility.GatherLocationForQuery(results.Requester.Position, results.Supplier.Position, i);
                        query.SoldOutTo = results.SoldOutTo.Equals(results.Requester.GetComponent<LocationDescription>().TitleLabel) ? "" : results.SoldOutTo;
                        //if (results.Requester.PriceNegotiator.OfferedRecord[MarketUtility.ResourceIdToMarketIndex(currResource)]]) { }

                        newTabLocations.Add(query);
                    }
                    else
                    {
                        var query = InfoPopupMarketUtility.GatherLocationForQuery(results.Supplier.Position, results.Requester.Position, i);
                        query.SoldOutTo = results.SoldOutTo.Equals(results.Requester.GetComponent<LocationDescription>().TitleLabel) ? "" : results.SoldOutTo;

                        newTabLocations.Add(query);
                    }
                }
            }

            return newTabLocations;
        }

        static public InspectorLocationQuery GatherLocationForQuery(OccupiesTile location, OccupiesTile referenceLocation, int colGroupIndex)
        {
            InspectorLocationQuery newLocationQuery = new InspectorLocationQuery();

            bool evenCol = colGroupIndex % 2 == 0;
            LocationDescription desc = location.GetComponent<LocationDescription>();

            // TODO: isActive
            // newLocationQuery.IsActive = true

            newLocationQuery.FarmName = Loc.Find(desc.TitleLabel);

            if (location.IsExternal)
            {
                newLocationQuery.FarmCounty = Loc.Find("region.external.name");
            }
            else
            {
                newLocationQuery.FarmCounty = Loc.Find(RegionUtility.GetNameLong(location.RegionIndex));
            }

            return newLocationQuery;
        }

        static public bool LoadLocationIntoCol(InfoPopupLocationColumn col, OccupiesTile location, OccupiesTile referenceLocation, bool forSale, bool runoffAffected, BestLocationHeader bestOptionHeader, int colGroupIndex, InspectorLocationQuery toLoad, ref int nextBestIndex, bool isShipping, InfoPopupColumnHeaders headers, Color arrowColor, Color arrowTextColor, string arrowTextId)
        {
            bool evenCol = colGroupIndex % 2 == 0;
            LocationDescription desc = location.GetComponent<LocationDescription>();

            col.NameLabel.gameObject.SetActive(true);
            // force name label rect to be correct
            col.ElseArrow.gameObject.SetActive(toLoad.SoldOutTo.IsEmpty);
            col.BuyArrow.gameObject.SetActive(false);
            col.NameLabel.SetText(toLoad.FarmName);
            col.Icon.sprite = desc.Icon;

            if (location.IsExternal)
            {
                col.RegionLabel.gameObject.SetActive(true);
                col.RegionLabel.SetText(toLoad.FarmCounty);
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
                col.RegionLabel.SetText(toLoad.FarmCounty);
                col.NameLabel.rectTransform.SetAnchorPos(3.7f, Axis.Y);
            }

            col.BackgroundRect.position = new Vector3(
                headers.BasePriceColHeader.transform.position.x - ColWidth / 2.0f,
                col.BackgroundRect.anchoredPosition.y,
                col.BackgroundRect.position.z);

            col.BackgroundRect.offsetMax = new Vector2(col.BackgroundRect.offsetMax.x, 0);
            col.BackgroundRect.offsetMin = new Vector2(col.BackgroundRect.offsetMin.x, 0);

            col.SoldOutGroup.SetActive(!toLoad.SoldOutTo.IsEmpty);

            if (colGroupIndex == nextBestIndex)
            {
                if (!toLoad.SoldOutTo.IsEmpty)
                {
                    // sold out from this seller. Shift next best index down one
                    nextBestIndex++;

                    col.SoldToText.SetText(Loc.Find(toLoad.SoldOutTo));
                }
                else
                {
                    col.BuyArrow.gameObject.SetActive(true);
                    col.BuyArrowText.SetText(Loc.Find(arrowTextId));
                    col.BuyArrow.color = arrowColor;
                    col.BuyArrowText.color = arrowTextColor;

                    bestOptionHeader.gameObject.SetActive(forSale);
                    if (forSale)
                    {
                        col.Background.SetColor(isShipping ? RowHighlightSellColor : RowHighlightBuyColor);
                        return true;
                    }
                    else
                    {
                        // check if runoff penalty
                        if (runoffAffected)
                        {
                            // col.Background.SetColor(RowPenaltyColor);
                            col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                        }
                        else
                        {
                            col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                        }

                        nextBestIndex++;
                    }
                }
            }
            else
            {
                // check if runoff penalty
                if (runoffAffected)
                {
                    // col.Background.SetColor(RowPenaltyColor);
                    col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                }
                else
                {
                    col.Background.SetColor(evenCol ? RowDefaultColor[0] : RowDefaultColor[1]);
                }
            }

            return false;
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
            col.BasePriceCol.Background.enabled = false;
            col.ShippingCol.Background.enabled = false;
            col.SalesTaxCol.Background.enabled = false;
            col.ImportTaxCol.Background.enabled = false;
            col.PenaltyCol.Background.enabled = false;
            col.TotalPriceCol.Background.enabled = false;
            col.TotalProfitCol.Background.enabled = false;
            col.SoldOutGroup.SetActive(false);
            col.BuyArrow.gameObject.SetActive(false);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex, headers);
        }

        static public void LoadEmptyProfitCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, GameObject bestOptionBanner, int colGroupIndex, bool anyPrev)
        {
            ActivateProfitHeaders(headers, policyState, grid);

            col.SalesTaxCol.gameObject.SetActive(false);
            if (!anyPrev)
            {
                col.ImportTaxCol.gameObject.SetActive(false);
                col.PenaltyCol.gameObject.SetActive(false);
            }
            col.BasePriceCol.Background.enabled = false;
            col.ShippingCol.Background.enabled = false;
            col.SalesTaxCol.Background.enabled = false;
            col.ImportTaxCol.Background.enabled = false;
            col.PenaltyCol.Background.enabled = false;
            col.TotalPriceCol.Background.enabled = false;
            col.TotalProfitCol.Background.enabled = false;
            col.SoldOutGroup.SetActive(false);
            col.BuyArrow.gameObject.SetActive(false);
            LoadEmptyCol(col, bestOptionBanner, colGroupIndex, headers);
        }

        static private void LoadEmptyCol(InfoPopupLocationColumn col, GameObject bestOptionBanner, int colGroupIndex, InfoPopupColumnHeaders headers)
        {
            bool evenCol = colGroupIndex % 2 == 0;

            col.PolicyIcon.gameObject.SetActive(false);
            col.ElseArrow.gameObject.SetActive(false);
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

            Routine.Start(RepositionBG(col, headers));
        }

        static private IEnumerator RepositionBG(InfoPopupLocationColumn col, InfoPopupColumnHeaders headers)
        {
            yield return new WaitForEndOfFrame();

            col.BackgroundRect.position = new Vector3(
                headers.BasePriceColHeader.transform.position.x - ColWidth / 2.0f,
                col.BackgroundRect.anchoredPosition.y,
                col.BackgroundRect.position.z);

            col.BackgroundRect.offsetMax = new Vector2(col.BackgroundRect.offsetMax.x, 0);
            col.BackgroundRect.offsetMin = new Vector2(col.BackgroundRect.offsetMin.x, 0);
        }

        static public InspectorProfitQuery GatherCostForQuery(PolicyState policyState, SimGridState grid, MarketConfig config, MarketQueryResultInfo info, bool forSale, bool isSecondary, bool showRunoff)
        {
            InspectorProfitQuery newProfitQuery = new InspectorProfitQuery();

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = info.Supplier.PriceNegotiator.SellPriceBlock[marketIndex];
            newProfitQuery.BasePrice = basePrice;

            int shippingPrice = info.ShippingCost;
            newProfitQuery.ShippingCost = shippingPrice;

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex && !info.Supplier.Position.IsExternal)
            {
                import = 0;
            }
            else
            {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource];
            }
            newProfitQuery.ImportTax = import;

            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource];
            newProfitQuery.SalesTax = salesTax;

            int penalties = info.TaxRevenue.Penalties;
            newProfitQuery.Penalties = penalties;

            int totalCost = basePrice + shippingPrice + import + salesTax + penalties;
            newProfitQuery.TotalProfit = totalCost;

            return newProfitQuery;
        }

        static public void LoadCostsIntoCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, MarketConfig config, bool forSale, bool isSecondary, InspectorProfitQuery costsToLoad, bool bestBuyChoice)
        {
            col.PriceGroup.SetActive(true);

            ActivateCostsHeaders(headers, policyState, grid);

            col.PolicyIcon.gameObject.SetActive(false);

            int marketIndex = MarketUtility.ResourceIdToMarketIndex(info.Resource);
            int basePrice = costsToLoad.BasePrice;

            col.BasePriceCol.gameObject.SetActive(true);
            SetMoneyText(col.BasePriceCol.Number, basePrice);
            col.BasePriceCol.Number.color = ActiveNumberColor;
            SetDefaultCellHighlightColor(col.BasePriceCol, bestBuyChoice, false);

            int shippingPrice = costsToLoad.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, shippingPrice);
            col.ShippingCol.Number.color = NegativeColor;
            SetDefaultCellHighlightColor(col.ShippingCol, bestBuyChoice, false);

            //int distance = info.Distance;
            //SetShippingRateText(col.ShippingCol.Detail, distance, shippingPrice);

            int import = costsToLoad.ImportTax;
            col.ImportTaxCol.Number.color = ActiveNumberColor;
            col.ImportTaxCol.gameObject.SetActive(true);
            SetDefaultCellHighlightColor(col.ImportTaxCol, bestBuyChoice, false);
            if (import == 0)
            {
                bool importActive = policyState.Policies[grid.CurrRegionIndex].Map[(int) Advisor.PolicyType.ImportTaxPolicy] != PolicyLevel.None;
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
                col.ImportTaxCol.Number.color = PositiveColor;
                SetCellHighlightColor(col.ImportTaxCol, PositiveColorBG);
            }
            else
            {
                SetMoneyText(col.ImportTaxCol.Number, import, " " + Loc.Find("ui.popup.info.taxBlurb"));
                col.ImportTaxCol.Number.color = NegativeColor;
                SetCellHighlightColor(col.ImportTaxCol, NegativeColorBG);
            }

            col.SalesTaxCol.Number.color = ActiveNumberColor;
            int salesTax = costsToLoad.SalesTax;
            col.SalesTaxCol.gameObject.SetActive(true);
            SetDefaultCellHighlightColor(col.SalesTaxCol, bestBuyChoice, false);
            if (salesTax == 0) {
                bool salesActive = policyState.Policies[grid.CurrRegionIndex].Map[(int) Advisor.PolicyType.SalesTaxPolicy] != PolicyLevel.None;
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
                col.SalesTaxCol.Number.color = PositiveColor;
                SetCellHighlightColor(col.SalesTaxCol, PositiveColorBG);

            }
            else
            {
                SetMoneyText(col.SalesTaxCol.Number, salesTax, " " + Loc.Find("ui.popup.info.taxBlurb"));
                col.SalesTaxCol.Number.color = NegativeColor;
                SetCellHighlightColor(col.SalesTaxCol, NegativeColorBG);
            }

            int penalties = costsToLoad.Penalties;
            col.PenaltyCol.gameObject.SetActive(false);
            SetDefaultCellHighlightColor(col.PenaltyCol, bestBuyChoice, false);
            if (penalties == 0) { 
                col.PenaltyCol.Number.SetText(EmptyEntry);
            }
            else { 
                SetMoneyText(col.PenaltyCol.Number, penalties);
                col.PenaltyCol.Number.color = NegativeColor;
            }

            int totalCost = costsToLoad.TotalProfit;
            col.TotalProfitCol.gameObject.SetActive(false);
            col.TotalPriceCol.gameObject.SetActive(true);
            SetDefaultCellHighlightColor(col.TotalPriceCol, bestBuyChoice, false);
            SetMoneyText(col.TotalPriceCol.Number, totalCost);

            if (isSecondary)
            {
                col.TotalPriceCol.Number.color = ActiveNumberColor;
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

        static public List<InspectorProfitQuery> GatherProfitGroupsForResourceTab(PolicyState policyState, SimGridState grid, MarketConfig config, ResourceMask currResource, int maxRows, int numFilledRows, RingBuffer<MarketQueryResultInfo> queryResults, bool showRunoff)
        {
            var newTabProfits = new List<InspectorProfitQuery>();

            for (int i = 0; i < maxRows; i++)
            {
                if (i < numFilledRows)
                {
                    var results = queryResults[i];
                    bool forSale = true;
                    // bool runoffAffected = results.TaxRevenue.Penalties > 0;
                    newTabProfits.Add(InfoPopupMarketUtility.GatherProfitForQuery(policyState, grid, config, results, forSale, i > 0, showRunoff));
                }
            }

            return newTabProfits;
        }

        static public List<InspectorProfitQuery> GatherCostGroupsForResourceTab(PolicyState policyState, SimGridState grid, MarketConfig config, ResourceMask currResource, int maxRows, int numFilledRows, RingBuffer<MarketQueryResultInfo> queryResults, bool showRunoff)
        {
            var newTabProfits = new List<InspectorProfitQuery>();

            for (int i = 0; i < maxRows; i++)
            {
                if (i < numFilledRows)
                {
                    var results = queryResults[i];
                    bool forSale = true;
                    // bool runoffAffected = results.TaxRevenue.Penalties > 0;
                    newTabProfits.Add(InfoPopupMarketUtility.GatherCostForQuery(policyState, grid, config, results, forSale, i > 0, showRunoff));
                }
            }

            return newTabProfits;
        }

        static public InspectorProfitQuery GatherProfitForQuery(PolicyState policyState, SimGridState grid, MarketConfig config, MarketQueryResultInfo info, bool forSale, bool isSecondary, bool showRunoff)
        {
            InspectorProfitQuery newProfitQuery = new InspectorProfitQuery();

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
            newProfitQuery.BasePrice = basePrice;

            int shippingPrice = info.ShippingCost;
            newProfitQuery.ShippingCost = shippingPrice;

            int import;
            if (info.Supplier.Position.RegionIndex == info.Requester.Position.RegionIndex)
            {
                import = 0;
            }
            else
            {
                import = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].ImportTax[info.Resource];
            }
            newProfitQuery.ImportTax = import;

            int salesTax = config.UserAdjustmentsPerRegion[info.Requester.Position.RegionIndex].PurchaseTax[info.Resource];
            newProfitQuery.SalesTax = salesTax;

            if (showRunoff)
            {
                int penalties = info.TaxRevenue.Penalties;
                newProfitQuery.Penalties = penalties;
            }

            int totalProfit = info.Profit /* + (salesTax + import)*/; // Commenting this out: sales tax and import tax do not give sellers any profit.
            newProfitQuery.TotalProfit = totalProfit;

            return newProfitQuery;
        }

        static public void LoadProfitIntoCol(PolicyState policyState, SimGridState grid, InfoPopupLocationColumn col, InfoPopupColumnHeaders headers, MarketQueryResultInfo info, bool forSale, bool isSecondary, bool showRunoff, InspectorProfitQuery profitsToLoad, bool bestSellChoice)
        {
            col.PriceGroup.SetActive(true);
            ActivateProfitHeaders(headers, policyState, grid);

            col.SalesTaxCol.Number.color = ActiveNumberColor;
            SetDefaultCellHighlightColor(col.SalesTaxCol, bestSellChoice, true);
            col.PolicyIcon.gameObject.SetActive(false);

            col.BasePriceCol.gameObject.SetActive(true);
            SetMoneyText(col.BasePriceCol.Number, profitsToLoad.BasePrice);
            col.BasePriceCol.Number.color = ActiveNumberColor;
            SetDefaultCellHighlightColor(col.BasePriceCol, bestSellChoice, true);

            int shippingPrice = info.ShippingCost;
            col.ShippingCol.gameObject.SetActive(true);
            SetMoneyText(col.ShippingCol.Number, -profitsToLoad.ShippingCost);
            col.ShippingCol.Number.color = NegativeColor;
            SetDefaultCellHighlightColor(col.ShippingCol, bestSellChoice, true);

            col.ImportTaxCol.gameObject.SetActive(false);
            if (profitsToLoad.ImportTax == 0) { col.ImportTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.ImportTaxCol.Number, (-profitsToLoad.ImportTax)); }
            SetDefaultCellHighlightColor(col.ImportTaxCol, bestSellChoice, true);

            col.SalesTaxCol.gameObject.SetActive(false);
            if (profitsToLoad.SalesTax == 0) { col.SalesTaxCol.Number.SetText(EmptyEntry); }
            else { SetMoneyText(col.SalesTaxCol.Number, (-profitsToLoad.SalesTax)); }
            SetDefaultCellHighlightColor(col.SalesTaxCol, bestSellChoice, true);

            col.PenaltyCol.gameObject.SetActive(showRunoff);
            if (showRunoff) {
                SetDefaultCellHighlightColor(col.PenaltyCol, bestSellChoice, true);
                if (profitsToLoad.Penalties == 0) {
                    col.PenaltyCol.Number.SetText(EmptyEntry);
                    col.PenaltyCol.Number.color = InactiveNumberColor;
                } else {
                    SetMoneyText(col.PenaltyCol.Number, (-profitsToLoad.Penalties), " " + Loc.Find("ui.popup.info.penaltyFine"));
                    col.PenaltyCol.Number.color = NegativeColor;
                    SetCellHighlightColor(col.PenaltyCol, NegativeColorBG);
                }
            }

            col.TotalProfitCol.gameObject.SetActive(true);
            col.TotalPriceCol.gameObject.SetActive(false);
            SetMoneyText(col.TotalProfitCol.Number, profitsToLoad.TotalProfit);
            SetDefaultCellHighlightColor(col.TotalProfitCol, bestSellChoice, true);

            if (profitsToLoad.TotalProfit < 0) {
                col.TotalProfitCol.Number.color = NegativeColor;
            } else if (isSecondary) {
                col.TotalProfitCol.Number.color = ActiveNumberColor;
            } else {
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
            //headers.PenaltyColHeader.gameObject.SetActive(true);
            headers.TotalProfitColHeader.gameObject.SetActive(true);
            headers.TotalPriceColHeader.gameObject.SetActive(false);

            bool penaltyActive = policyState.Policies[grid.CurrRegionIndex].Map[(int) Advisor.PolicyType.RunoffPolicy] != PolicyLevel.None;
            if (penaltyActive) {
                headers.PenaltyColHeader.Icon.SetAlpha(ActiveAlpha);
                headers.PenaltyColHeader.Text.SetAlpha(ActiveAlpha);
            }
            else { 
                headers.PenaltyColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.PenaltyColHeader.Text.SetAlpha(InactiveAlpha);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(headers.LayoutRect);
            headers.Layout.ForceRebuild(true);
        }

        static private void SetDefaultCellHighlightColor(InfoPopupPriceRow cell, bool bestOption, bool isShipping)
        {
            if (bestOption)
            {
                cell.Background.enabled = true;
                cell.Background.color = isShipping ? RowHighlightSellColor : RowHighlightBuyColor;
            }
            else
            {
                cell.Background.enabled = false;
            }
        }

        static private void SetCellHighlightColor(InfoPopupPriceRow cell, Color setTo)
        {
            cell.Background.enabled = true;
            cell.Background.color = setTo;
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

            bool importActive = policyState.Policies[grid.CurrRegionIndex].Map[(int) Advisor.PolicyType.ImportTaxPolicy] != PolicyLevel.None;
            if (importActive) {
                headers.ImportTaxColHeader.Icon.SetAlpha(ActiveAlpha); 
                headers.ImportTaxColHeader.Text.SetAlpha(ActiveAlpha);
            }
            else { 
                headers.ImportTaxColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.ImportTaxColHeader.Text.SetAlpha(InactiveAlpha);
            }

            bool salesActive = policyState.Policies[grid.CurrRegionIndex].Map[(int) Advisor.PolicyType.SalesTaxPolicy] != PolicyLevel.None;
            if (salesActive) { 
                headers.SalesTaxColHeader.Icon.SetAlpha(ActiveAlpha); 
                headers.SalesTaxColHeader.Text.SetAlpha(ActiveAlpha); 
            }
            else { 
                headers.SalesTaxColHeader.Icon.SetAlpha(InactiveAlpha);
                headers.SalesTaxColHeader.Text.SetAlpha(InactiveAlpha);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(headers.LayoutRect);
            headers.Layout.ForceRebuild(true);
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