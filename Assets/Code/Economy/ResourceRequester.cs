using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile), typeof(ResourcePriceNegotiator))]
    public sealed class ResourceRequester : BatchedComponent {
        [AutoEnum] public ResourceMask RequestMask;
        public int MaxRequests = 3;
        public int AgeOfUrgency = 30; // Min age at which requests begin to be marked as urgent

        public bool IsLocalOption = false; // used by Let It Sit option on Dairy Farms
        public bool InfiniteRequests = false;  // used by Let It Sit option on Dairy Farms
        public bool RefusesSameBuildingType = false;  // storage refuses to purchase from other storage

        public bool OverridesBuyPrice = false;
        public MarketPriceBlock OverrideBlock;

        [NonSerialized] public OccupiesTile Position;
        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public MarketRequesterPriorityList Priorities;
        [NonSerialized] public int[] BestPriorityIndex;
        [NonSerialized] public bool MatchedThisTick = false;
        [NonSerialized] public bool SubsidyAppliedThisTick = false;
        [NonSerialized] public bool PurchasedAtStressedPrice = false;

        [NonSerialized] public ResourceBlock Requested;
        [NonSerialized] public int RequestCount;
        [NonSerialized] public ResourceBlock Received;

        [NonSerialized] public ResourcePriceNegotiator PriceNegotiator;

        public Flagstaff Flagstaff;


        // Map resource to ticks since fulfilled


        protected override void OnEnable() {
            base.OnEnable();

            this.CacheComponent(ref Position);
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref PriceNegotiator);

            PriceNegotiatorUtility.InitializeRequesterNegotiator(this, PriceNegotiator, RequestMask, Position.RegionIndex);

            Priorities.Create();

            // One index for each market (Grain, Milk, and Phosphorus)
            BestPriorityIndex = new int[3];

            MarketUtility.RegisterBuyer(this);
            if (!IsLocalOption) {
                RoadUtility.RegisterDestination(Position, (RoadDestinationMask) RequestMask);
            }

            if (Flagstaff)
            {
                Flagstaff.FlagVisuals.gameObject.SetActive(false);
            }
        }

        protected override void OnDisable() {
            if (Frame.IsLoadingOrLoaded(this)) {
                MarketUtility.DeregisterBuyer(this);
                if (!IsLocalOption) {
                    RoadUtility.DeregisterDestination(Position);
                }

                Requested.SetAll(0);
                RequestCount = 0;
                Received.SetAll(0);
            }

            base.OnDisable();
        }
    }
}