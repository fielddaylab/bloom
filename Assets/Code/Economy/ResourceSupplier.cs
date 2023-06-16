using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceStorage), typeof(OccupiesTile))]
    public sealed class ResourceSupplier : BatchedComponent {
        [AutoEnum] public ResourceMask ShippingMask;
        
        [NonSerialized] public OccupiesTile Position;

        [NonSerialized] public ResourceStorage Storage;
        [NonSerialized] public MarketSupplierPriorityList Priorities;

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Position);

            Priorities.Create();
        }

        protected override void OnEnable() {
            base.OnEnable();

            MarketUtility.RegisterSupplier(this);
        }

        protected override void OnDisable() {
            MarketUtility.DeregisterSupplier(this);

            base.OnDisable();
        }
    }
}