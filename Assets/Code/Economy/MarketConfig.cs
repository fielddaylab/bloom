using System;
using BeauUtil;
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
    }

    [Serializable]
    public struct PurchaseCosts {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Buy;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Import;
    }

    [Serializable]
    public struct PurchaseCostAdjustments {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock ExportTax;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock PurchaseTax;
    }
}