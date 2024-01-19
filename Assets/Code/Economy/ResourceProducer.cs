using System;
using BeauPools;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceStorage))]
    public sealed class ResourceProducer : BatchedComponent {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Produces;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Requires;

        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public ResourceRequester Request;

        [NonSerialized] public ResourcePriceNegotiator PriceNegotiator;

        [NonSerialized] public bool ProducedLastTick = false; // used by production runoff system
        [NonSerialized] public ResourceBlock ConsumedLastTick;   // used by production runoff: what did they use to produce

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);
            this.CacheComponent(ref PriceNegotiator);

            // Post inifinite requests when enabled
            if (Request && Request.InfiniteRequests) {
                MarketUtility.RegisterInfiniteProducer(this);
            }
        }

        protected override void OnDisable() {
            ProducedLastTick = false;

            if (Request && Request.InfiniteRequests) {
                MarketUtility.DeregisterInfiniteProducer(this);
            }

            base.OnDisable();
        }
    }
}