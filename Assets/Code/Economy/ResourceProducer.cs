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

        [NonSerialized] public bool ProducedLastTick = false; // used by production runoff system

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);

            // Post inifinite requests when enabled
            if (Request && Request.InfiniteRequests) {
                MarketUtility.RegisterInifiniteProducer(this);
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