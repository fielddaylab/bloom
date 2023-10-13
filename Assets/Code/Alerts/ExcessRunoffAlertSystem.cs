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
        public override void ProcessWorkForComponent(ActorPhosphorusGenerator generator, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.ExcessRunoff)) {
                // only add this trigger once
                return;
            }

            // check if runoff is excessive
            int excessThreshold = 8; // TODO: balance this number
            Debug.Log("[Phosphorus] Amount generated last tick: " + generator.AmountProducedLastTick);
            if (generator.AmountProducedLastTick >= excessThreshold) {
                // if so, create alert on this tile
                EventActorUtility.QueueAlert(actor, EventActorAlertType.ExcessRunoff, tile.TileIndex);
            }
        }
    }
}