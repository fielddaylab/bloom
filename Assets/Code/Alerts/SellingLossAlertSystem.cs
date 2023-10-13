using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class SellingLossAlertSystem : ComponentSystemBehaviour<ResourceSupplier, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ResourceSupplier supplier, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.SellingLoss)) {
                // only add this trigger once
                return;
            }

            // Check if a supplier sold at a loss
            if (supplier.SoldAtALoss) {
                // if so, create alert on this tile
                EventActorUtility.QueueAlert(actor, EventActorAlertType.SellingLoss, tile.TileIndex);
            }
        }
    }
}