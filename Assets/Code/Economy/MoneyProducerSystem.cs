using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;

namespace Zavala.Economy {
    public sealed class MoneyProducerSystem : ComponentSystemBehaviour<MoneyProducer, ActorTimer> {
        public override void ProcessWorkForComponent(MoneyProducer producer, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            if (MarketUtility.CanProduceNow(producer, out int producedAmt)) {
                ResourceBlock consumed = producer.Requires;
                ResourceBlock.Consume(ref producer.Storage.Current, ref consumed);
                // TODO: add producedAmt to regional budget
                
                //
                Log.Msg("[MoneyProducerSystem] Producer '{0}' consumed {1} to produce {2} money units", producer.name, consumed, producedAmt);
                // TODO: events?
                DebugDraw.AddWorldText(producer.transform.position, "Produced $!", Color.green, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
            } /*else if (producer.Requires.IsPositive) {
                Assert.NotNull(producer.Request, "MoneyProducers that require external resources must have a ResourceRequester");
                MarketUtility.QueueRequest(producer.Request, producer.Requires);
                DebugDraw.AddWorldText(producer.transform.position, "Requesting!", Color.yellow, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
            }*/
        }
    }
}