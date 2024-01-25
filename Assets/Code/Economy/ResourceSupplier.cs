using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceStorage), typeof(OccupiesTile), typeof(ResourcePriceNegotiator))]
    public sealed class ResourceSupplier : BatchedComponent {
        [AutoEnum] public ResourceMask ShippingMask;

        public int SupplierPriority = 0; // determines the batch this supplier is processed in during market cycle
        
        [NonSerialized] public OccupiesTile Position;

        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public bool SoldAtALossExcludingMilk = false;
        [NonSerialized] public bool MatchedThisTick = false;
        [NonSerialized] public bool MatchedThisTickWasMilk = false;
        // [NonSerialized] public bool SubsidyAppliedThisTick = false;

        [NonSerialized] public MarketSupplierPriorityList Priorities;
        [NonSerialized] public int[] BestPriorityIndex;

        [NonSerialized] public ResourcePriceNegotiator PriceNegotiator;
        [NonSerialized] public ResourceBlock PreSaleSnapshot;
        [NonSerialized] public ResourceBlock PostSaleSnapshot;

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Position);
            this.CacheComponent(ref PriceNegotiator);

            PriceNegotiatorUtility.InitializeSupplierNegotiator(PriceNegotiator, ShippingMask, Position.RegionIndex);

            Priorities.Create();

            // One index for each market (Grain, Milk, and Phosphorus)
            BestPriorityIndex = new int[3];
        }

        protected override void OnEnable() {
            base.OnEnable();

            MarketUtility.RegisterSupplier(this);
            RoadUtility.RegisterSource(Position, (RoadDestinationMask) ShippingMask | RoadDestinationMask.Tollbooth | RoadDestinationMask.Export);
        }

        protected override void OnDisable() {
            if (Frame.IsLoadingOrLoaded(this)) {
                MarketUtility.DeregisterSupplier(this);
                RoadUtility.DeregisterSource(Position);

                SoldAtALossExcludingMilk = false;
                Priorities.PrioritizedBuyers.Clear();
            }

            base.OnDisable();
        }
    }
}