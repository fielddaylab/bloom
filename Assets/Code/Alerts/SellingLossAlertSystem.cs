using BeauUtil.Variants;
using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    public class SellingLossAlertSystem : ComponentSystemBehaviour<ResourceSupplier, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ResourceSupplier supplier, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (EventActorUtility.AnyQueueContains(actor, GameAlerts.SellingLoss)) {
                // only add this trigger once
                return;
            }

            // Check if a supplier sold at a loss
            if (supplier.SoldAtALoss) {
                // if so, create alert on this tile
                EventActorTrigger newTrigger = new EventActorTrigger();
                newTrigger.EventId = GameTriggers.AlertExamined;
                newTrigger.Argument = new NamedVariant("alertType", GameAlerts.SellingLoss);
                EventActorUtility.QueueTrigger(actor, newTrigger.EventId, tile.TileIndex, newTrigger.Argument);
            }
        }
    }
}