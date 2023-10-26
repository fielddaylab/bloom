using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
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

        [NonSerialized] public ResourceBlock Requested;
        [NonSerialized] public int RequestCount;
        [NonSerialized] public ResourceBlock Received;


        // Map resource to ticks since fulfilled


        protected override void OnEnable() {
            base.OnEnable();

            this.CacheComponent(ref Position);
            this.CacheComponent(ref Storage);

            MarketUtility.RegisterBuyer(this);
            RoadUtility.RegisterRoadAnchor(Position);
            if (!IsLocalOption) {
                RoadUtility.RegisterIntakeMask(Position, RequestMask);
            }
        }

        protected override void OnDisable() {
            MarketUtility.DeregisterBuyer(this);
            RoadUtility.DeregisterRoadAnchor(Position);
            if (!IsLocalOption) {
                RoadUtility.DeregisterIntakeMask(Position);
            }

            Requested.SetAll(0);
            RequestCount = 0;
            Received.SetAll(0);

            base.OnDisable();
        }
    }
}