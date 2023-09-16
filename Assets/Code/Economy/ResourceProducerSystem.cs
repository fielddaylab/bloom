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
                producer.ProducedLastTick = true;

                ResourceRequester requester = producer.GetComponent<ResourceRequester>();
                Log.Msg("[ResourceProducerSystem] Producer '{0}' consumed {1} to produce {2}", producer.name, consumed, produced);
                DebugDraw.AddWorldText(producer.transform.position, "Produced!", Color.green, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);

                ResourceRequester requestComp = producer.GetComponent<ResourceRequester>();
                if (requester != null && requester.IsLocalOption) {
                    // Local option need to produce and request on the same tick
                    QueueRequestForProduction(producer);
                }
            }
            else if (producer.Requires.IsPositive) {
                QueueRequestForProduction(producer);
            }
        }

        private void QueueRequestForProduction(ResourceProducer producer) {
            Assert.NotNull(producer.Request, "ResourceProducers that require external resources must have a ResourceRequester");
            MarketUtility.QueueRequest(producer.Request, producer.Requires);
            DebugDraw.AddWorldText(producer.transform.position, "Requesting!", Color.yellow, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
        }
    }
}