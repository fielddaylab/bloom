using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, 4, ZavalaGame.SimulationUpdateMask)] // Before MarketSystem, After ActorPhosphorusGeneratorSystem
    public sealed class StorageRecyclerSystem : ComponentSystemBehaviour<StorageRecycler, ResourceStorage, ActorTimer>
    {
        public override void ProcessWorkForComponent(StorageRecycler recycler, ResourceStorage storage, ActorTimer timer, float deltaTime) {
            if (timer != null && !timer.HasAdvanced()) {
                return;
            }

            if (storage.Current.IsZero) {
                return;
            }

            if (ResourceBlock.CanAddFull(storage.Current, recycler.ReturnTo.Current, recycler.ReturnTo.Capacity)) {
                // return the recyclable storage to the receiver
                recycler.ReturnTo.Current += storage.Current;

                // remove the recyclable storage
                storage.Current = default;
                ResourceStorageUtility.RefreshStorageDisplays(storage);
                ResourceStorageUtility.RefreshStorageDisplays(recycler.ReturnTo);
                // Log.Msg("[Sitting] Sitting storage returned! Left in SITTING: {0}", storage.Current.Manure);
            }
            else {
                // TODO: Handle too much letting run off to return
            }
        }
    }
}