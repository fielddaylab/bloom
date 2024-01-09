using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
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
        public ResourceRequester StorageExtensionReq; // mainly for let it sit option
        public ResourceStorage StorageExtensionStore; // mainly for let it sit option

        public bool InfiniteSupply = false;  // true if external commercial fertilizer seller

        public void Start() {
             ResourceStorageUtility.RefreshStorageDisplays(this);
        }

        protected override void OnDisable() {
            Current.SetAll(0);

            base.OnDisable();
        }
    }

    public static class ResourceStorageUtility {
        public static void RefreshStorageDisplays(ResourceStorage storage) {
            if (storage == null || storage.Displays.Length <= 0) return;
            if (ResourceBlock.TryClamp(ref storage.Current, storage.Capacity)) {
                Log.Warn("[ResourceStorage] Storage {0} went over capacity! Clamping...", storage.name);
            }
            foreach (ResourceDisplay display in storage.Displays) {
                int extensionAdd = 0;
                if (storage.StorageExtensionReq != null) {
                    extensionAdd += storage.StorageExtensionReq.Received[display.ResourceType];
                }
                if (storage.StorageExtensionStore != null) {
                    extensionAdd += storage.StorageExtensionStore.Current[display.ResourceType];
                }
                display?.SetCount(storage.Current[display.ResourceType] + extensionAdd);
            }
        }
    }
}