using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;

namespace Zavala.Economy {
    public sealed class ResourceProducerSystem : ComponentSystemBehaviour<ResourceProducer, ActorTimer> {
        public override void ProcessWorkForComponent(ResourceProducer producer, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            if (MarketUtility.CanProduceNow(producer, out ResourceBlock produced)) {
                ResourceBlock consumed = producer.Requires;
                ResourceBlock.Consume(ref producer.Storage.Current, ref consumed);
                producer.Storage.Current += produced;
                Log.Msg("[ResourceProducerSystem] Producer '{0}' consumed {1} to produce {2}", producer.name, consumed, produced);
                // TODO: events?
                // TODO: apply runoff
                DebugDraw.AddWorldText(producer.transform.position, "Produced!", Color.green, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
            } else if (producer.Requires.IsPositive) {
                Assert.NotNull(producer.Request, "ResourceProducers that require external resources must have a ResourceRequester");
                MarketUtility.QueueRequest(producer.Request, producer.Requires);
                DebugDraw.AddWorldText(producer.transform.position, "Requesting!", Color.yellow, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
            }
        }
    }
}