using System;
using BeauUtil;
using FieldDay.Data;
using FieldDay.SharedState;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Economy {
    public sealed class MarketConfig : SharedStateComponent {
        [Header("Global")]
        public TransportCosts TransportCosts;

        [Header("Per-Region")]
        // starting point for individual prices
        public PurchaseCosts[] DefaultPurchasePerRegion = new PurchaseCosts[RegionInfo.MaxRegions];
        [NonSerialized]
        public PurchaseCostAdjustments[] UserAdjustmentsPerRegion = new PurchaseCostAdjustments[RegionInfo.MaxRegions];
    }

    [Serializable]
    public struct TransportCosts {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock CostPerTile;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock ExportDepotFlatRate;
    }

    [Serializable]
    public struct PurchaseCosts {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Buy;

        // [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        // public ResourceBlock Sell;

        // [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        // public ResourceBlock Import;
    }

    [Serializable]
    public struct PurchaseCostAdjustments {
        /*
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock ExportTax;
        */

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock ImportTax;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock PurchaseTax;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock RunoffPenalty;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public int SkimmerCost;
    }

    public static class MarketParams {

        #region Tunable Parameters

        [ConfigVar("TruckSpeed", 0.2f, 5, 0.2f)] static public float TruckSpeed = 1;

        [ConfigVar("AirshipSpeed", 0.2f, 5, 0.2f)] static public float AirshipSpeed = 2;

        [ConfigVar("ParcelSpeed", 0.2f, 5, 0.2f)] static public float ParcelSpeed = 2.2f;

        [ConfigVar("TicksPerNegotiation", 1, 10, 1)] static public float TicksPerNegotiation = 3; // How many market ticks before price step is applied

        [ConfigVar("NegotiationStep", 1, 10, 1)] static public int NegotiationStep = 1; // the price step applied during negotation phase

        #endregion
    }
}