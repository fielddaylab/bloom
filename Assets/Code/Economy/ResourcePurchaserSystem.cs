using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    [SysUpdate(FieldDay.GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class ResourcePurchaserSystem : ComponentSystemBehaviour<ResourcePurchaser, ActorTimer> {
        public override void ProcessWorkForComponent(ResourcePurchaser purchaser, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (Game.SharedState.Get<TutorialState>().CurrState < TutorialState.State.ActiveSim) {
                return;
            }

            purchaser.RequestAmountHistory.PushBack(purchaser.RequestAmount);

            ref ResourceBlock total = ref purchaser.Request.Received;
            ResourceBlock requestAmt = purchaser.RequestAmount;
            

            if (ResourceBlock.Consume(ref total, ref requestAmt)) {
                ResourceBlock cash = requestAmt * purchaser.PurchasePrice;
                ResourceStorageUtility.RefreshStorageDisplays(purchaser.Storage);
                Log.Msg("[ResourcePurchaserSystem] Purchaser '{0}' consumed {1} for ${2}", purchaser.name, requestAmt, cash.Count);
                // TODO: cash
                // Dispatch purchase event
                SimWorldState world = ZavalaGame.SimWorld;
                SimGridState grid = ZavalaGame.SimGrid;
                HexVector vec = HexVector.FromWorld(purchaser.transform.position, world.WorldSpace);
                ZavalaGame.Events.Dispatch(ResourcePurchaser.Event_PurchaseMade, grid.HexSize.FastPosToIndex(vec));
            } else {
                MarketUtility.QueueRequest(purchaser.Request, purchaser.RequestAmount);
                DebugDraw.AddWorldText(purchaser.transform.position, "Requesting!", Color.yellow, 2);
            }
        }



    }
}