using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Scripting;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class ExcessRunoffAlertSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, EventActor, OccupiesTile>
    {
        private static int EXCESS_THRESHOLD = 12; // CAFO generator amount * 3 * manure unites

        public override void ProcessWorkForComponent(ActorPhosphorusGenerator generator, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.ExcessRunoff)) {
                // only add this trigger once
                return;
            }

            // check if runoff is excessive
            Debug.Log("[Phosphorus] Amount generated last tick: " + generator.AmountProducedLastTick);
            if (generator.AmountProducedLastTick >= EXCESS_THRESHOLD) {
                // if so, create alert on this tile
                EventActorUtility.QueueAlert(actor, EventActorAlertType.ExcessRunoff, tile.TileIndex);
            }
        }
    }
}