using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0)] // after SimAlgaeSystem
    // Set execution order to after SimAlgaeStateSystem
    public class BloomAlertSystem : ComponentSystemBehaviour<EventActor, OccupiesTile>
    {
        public override void ProcessWorkForComponent(EventActor actor, OccupiesTile tile, float deltaTime) {
            if (actor.QueuedTriggers.Count > 0) {
                return;
            }
            bool bloomPeaked = false;

            SimAlgaeState simAlgae = Game.SharedState.Get<SimAlgaeState>();
            bloomPeaked = simAlgae.Algae.PeakingTiles.Contains(tile.TileIndex);

            if (bloomPeaked) {
                EventActorTrigger newTrigger = new EventActorTrigger();
                newTrigger.EventId = GameTriggers.AlertExamined;
                newTrigger.Argument = new NamedVariant("alertType", GameAlerts.Bloom);
                EventActorUtility.QueueTrigger(actor, newTrigger.EventId, newTrigger.Argument);

                // TODO: seems like we should be able to clear this at the start of SimAlgaeSystem, since this system comes after.
                // But for some reason some tiles aren't processed before SimAlgaeSystem starts again.
                // So for now, this system is responsible for removing the PeakingTiles from the hash set.
                simAlgae.Algae.PeakingTiles.Remove(tile.TileIndex);
            }

        }
    }
}