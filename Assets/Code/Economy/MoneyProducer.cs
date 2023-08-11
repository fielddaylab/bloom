using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.UI;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceStorage))]
    public sealed class MoneyProducer : BatchedComponent {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public int ProducesAmt;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Requires;

        [NonSerialized] public ResourceStorage Storage;

        private void Awake() {
            this.CacheComponent(ref Storage);
        }
    }
}