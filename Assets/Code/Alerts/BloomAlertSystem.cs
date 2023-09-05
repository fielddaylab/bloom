using BeauUtil.Variants;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    public class BloomAlertSystem : ComponentSystemBehaviour<EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(EventActor actor, OccupiesTile tile, float deltaTime) {
            if (actor.QueuedTriggers.Count > 0) {
                return;
            }

            EventActorTrigger newTrigger = new EventActorTrigger();
            newTrigger.EventId = GameTriggers.AlertExamined;
            newTrigger.Argument = new NamedVariant("alertType", GameAlerts.Bloom);
            EventActorUtility.QueueTrigger(actor, newTrigger.EventId, newTrigger.Argument);

            // TODO: check if bloom has occured
                // if so, create alert on this tile
        }
    }
}