using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.World;

namespace Zavala.Economy {
    public sealed class ResourcePurchaserSystem : ComponentSystemBehaviour<ResourcePurchaser, ActorTimer> {
        public override void ProcessWorkForComponent(ResourcePurchaser purchaser, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            ref ResourceBlock total = ref purchaser.Request.Received;
            ResourceBlock requestAmt = purchaser.RequestAmount;

            if (ResourceBlock.Consume(ref total, ref requestAmt)) {
                ResourceBlock cash = requestAmt * purchaser.PurchasePrice;
                Log.Msg("[ResourcePurchaserSystem] Purchaser '{0}' consumed {1} for ${2}", purchaser.name, requestAmt, cash.Count);
                // TODO: cash
                // Dispatch purchase event
                ZavalaGame.Events.Dispatch(ResourcePurchaser.Event_PurchaseMade, purchaser);
            } else {
                MarketUtility.QueueRequest(purchaser.Request, purchaser.RequestAmount);
                DebugDraw.AddWorldText(purchaser.transform.position, "Requesting!", Color.yellow, 2);
                // Dispatch unfulfilled purchase event
                ZavalaGame.Events.Dispatch(ResourcePurchaser.Event_PurchaseUnfulfilled, purchaser);

            }
        }



    }
}