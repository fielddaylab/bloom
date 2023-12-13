using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile), typeof(ResourcePriceNegotiator))]
    public sealed class ResourceRequester : BatchedComponent {
        [AutoEnum] public ResourceMask RequestMask;
        public int MaxRequests = 3;
        public int AgeOfUrgency = 20; // Min age at which requests begin to be marked as urgent

        public bool IsLocalOption = false; // used by Let It Sit option on Dairy Farms
        public bool InfiniteRequests = false;  // used by Let It Sit option on Dairy Farms

        public bool OverridesBuyPrice = false;
        public ResourceBlock OverrideBlock;

        [NonSerialized] public OccupiesTile Position;
        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public MarketRequesterPriorityList Priorities;
        [NonSerialized] public int BestPriorityIndex;
        [NonSerialized] public bool MatchedThisTick;

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

            PriceNegotiatorUtility.InitializeRequesterNegotiator(PriceNegotiator, RequestMask, Position.RegionIndex);

            Priorities.Create();

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
            MarketUtility.DeregisterBuyer(this);
            if (!IsLocalOption) {
                RoadUtility.DeregisterDestination(Position);
            }

            Requested.SetAll(0);
            RequestCount = 0;
            Received.SetAll(0);

            base.OnDisable();
        }
    }
}