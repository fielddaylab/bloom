using FieldDay.Systems;
using Zavala.Actors;
using Zavala.Economy;

namespace Zavala.Sim
{
    public class SellingLossAlertSystem : ComponentSystemBehaviour<ResourceSupplier, ActorTimer, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ResourceSupplier supplier, ActorTimer timer, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            // TODO: check if runoff is excessive
            // if so, create alert on this tile
        }
    }
}