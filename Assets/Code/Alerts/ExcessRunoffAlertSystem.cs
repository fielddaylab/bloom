using BeauUtil.Variants;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Scripting;

namespace Zavala.Sim
{
    public class ExcessRunoffAlertSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ActorPhosphorusGenerator generator, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (EventActorUtility.AnyQueueContains(actor, GameAlerts.ExcessRunoff)) {
                // only add this trigger once
                return;
            }

            // check if runoff is excessive
            int excessThreshold = 5; // TODO: balance this number

            if (generator.AmountProducedLastTick >= excessThreshold) {
                // if so, create alert on this tile
                EventActorTrigger newTrigger = new EventActorTrigger();
                newTrigger.EventId = GameTriggers.AlertExamined;
                newTrigger.Argument = new NamedVariant("alertType", GameAlerts.ExcessRunoff);
                EventActorUtility.QueueTrigger(actor, newTrigger.EventId, newTrigger.Argument);
            }
        }
    }
}