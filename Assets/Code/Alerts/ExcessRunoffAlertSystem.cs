using BeauUtil.Debugger;
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
            Log.Msg("[ExcessRunoffAlertSystem] Amount generated last tick: " + generator.AmountProducedLastTick);
            if (generator.AmountProducedLastTick >= RunoffParams.ExcessRunoffThreshold) {
                // if so, create alert on this tile
                if (generator.transform.parent != null && generator.transform.parent.TryGetComponent(out EventActor parentActor)) {
                    EventActorUtility.QueueAlert(parentActor, EventActorAlertType.ExcessRunoff, tile.TileIndex, tile.RegionIndex, 
                        new NamedVariant("isFromGrainFarm", false));
                } else {
                    Log.Msg("[ExcessRunoffAlertSystem] Attempting to create grain alert");
                    EventActorUtility.QueueAlert(actor, EventActorAlertType.ExcessRunoff, tile.TileIndex, tile.RegionIndex, 
                        new NamedVariant("isFromGrainFarm", true));
                }
            }
        }
    }
}