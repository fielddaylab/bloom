using System;
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

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);
        }
    }
}