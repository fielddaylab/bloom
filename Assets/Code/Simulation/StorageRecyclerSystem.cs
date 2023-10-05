using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, 4)] // Before MarketSystem, After ActorPhosphorusGeneratorSystem
    public sealed class StorageRecyclerSystem : ComponentSystemBehaviour<StorageRecycler, ResourceStorage, ActorTimer>
    {
        public override void ProcessWorkForComponent(StorageRecycler recycler, ResourceStorage storage, ActorTimer timer, float deltaTime) {
            if (!timer.HasAdvanced()) {
                return;
            }
            Debug.Log("[Sitting] Time to recycle storage... ");

            if (storage.Current.IsZero) {
                Debug.Log("[Sitting] recycle storage empty.");
                return;
            }

            if (ResourceBlock.CanAddFull(storage.Current, recycler.ReturnTo.Current, recycler.ReturnTo.Capacity)) {
                // return the rcyclable storage to the receiver
                recycler.ReturnTo.Current += storage.Current;

                // remove the recyclable storage
                storage.Current = default;
                ResourceStorageUtility.RefreshStorageDisplays(storage);
                Debug.Log("[Sitting] Sitting storage returned!");
            }
            else {
                // TODO: Handle too much letting run off to return
            }
        }
    }
}