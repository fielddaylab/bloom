using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Scripting;
using Zavala.UI;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class ExcessRunoffAlertSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(ActorPhosphorusGenerator generator, ActorTimer timer, EventActor actor, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            bool isFromGrainFarm = true;
            if (generator.transform.parent != null && generator.transform.parent.TryGetComponent(out EventActor parentActor)) {
                isFromGrainFarm = false;
                actor = parentActor;
            }
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.ExcessRunoff)) {
                if (generator.RunoffImproving) {
                    // Sold manure recently - cancel any existing alert!
                    // UIAlertUtility.ClearAlert(actor.DisplayingEvent);
                    EventActorUtility.ClearAndPopAlert(actor);
                } 
                // only add this trigger once
                return;
            }

            // check if runoff is excessive
            Log.Debug("[ExcessRunoffAlertSystem] Amount generated last tick by {0}: {1}", actor.name, generator.AmountProducedLastTick);
            if (generator.AmountProducedLastTick >= RunoffParams.ExcessRunoffThreshold) {
                // if so, create alert on this tile
                Log.Debug("-----> Sending runoff alert from {0}", actor.name);
                EventActorUtility.QueueAlert(actor, EventActorAlertType.ExcessRunoff, tile.TileIndex, tile.RegionIndex, 
                    new NamedVariant("isFromGrainFarm", isFromGrainFarm));
            }
        }
    }
}