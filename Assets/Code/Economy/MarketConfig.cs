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
        public PurchaseCosts[] PurchasePerRegion = new PurchaseCosts[RegionInfo.MaxRegions];
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
    }

    public static class MarketParams {

        #region Tunable Parameters

        // begin growing algae when this P threshold is exceeded
        [ConfigVar("TruckSpeed", 0.2f, 5, 0.2f)] static public float TruckSpeed = 1;

        // begin growing algae when this P threshold is exceeded
        [ConfigVar("AirshipSpeed", 0.2f, 5, 0.2f)] static public float AirshipSpeed = 2;

        // begin growing algae when this P threshold is exceeded
        [ConfigVar("ParcelSpeed", 0.2f, 5, 0.2f)] static public float ParcelSpeed = 2.2f;

        #endregion
    }
}