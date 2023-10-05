using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    public sealed class ResourceStorage : BatchedComponent {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Capacity = new ResourceBlock() {
            Manure = 8,
            DFertilizer = 8,
            MFertilizer = 8,
            Milk = 8,
            Grain = 8
        };

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock Current;
        public ResourceDisplay[] Displays;

        public void Start() {
             ResourceStorageUtility.RefreshStorageDisplays(this);
        }
    }

    public static class ResourceStorageUtility {
        public static void RefreshStorageDisplays(ResourceStorage storage) {
            if (storage.Displays.Length <= 0) return;
            foreach (ResourceDisplay display in storage.Displays) {
                display.SetCount(storage.Current[display.ResourceType]);
            }
        }
    }
}