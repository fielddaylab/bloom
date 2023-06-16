using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceRequester))]
    public sealed class ResourcePurchaser : BatchedComponent {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock RequestAmount;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock PurchasePrice;

        [NonSerialized] public ResourceRequester Request;
        [NonSerialized] public ResourceStorage Storage;

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);
        }
    }
}