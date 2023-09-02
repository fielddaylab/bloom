using FieldDay.Systems;
using Zavala.Actors;

namespace Zavala.Sim
{
    public class RunoffAlertSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ActorPhosphorusGenerator generator, ActorTimer timer, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            // TODO: check if runoff is excessive
                // if so, create alert on this tile
        }
    }
}