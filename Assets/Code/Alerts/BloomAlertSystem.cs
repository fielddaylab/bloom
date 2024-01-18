using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.UI;
using Zavala.World;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)] // after SimAlgaeSystem
    // Set execution order to after SimAlgaeStateSystem
    public class BloomAlertSystem : ComponentSystemBehaviour<EventActor, ActorTimer, OccupiesTile>
    {
        public override void ProcessWorkForComponent(EventActor actor, ActorTimer timer, OccupiesTile tile, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) return;
            
            SimAlgaeState simAlgae = Game.SharedState.Get<SimAlgaeState>();
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.Bloom)) {
                if (!simAlgae.Algae.BloomedTiles.Contains(tile.TileIndex)) {
                    Log.Msg("[BloomAlertSystem] Bloom receding, attempted to cancel alert");
                    UIAlertUtility.ClearAlert(actor.DisplayingEvent);
                    //EventActorUtility.CancelEventType(actor, EventActorAlertType.Bloom);
                }
                return;
            }

            // SimAlgaeState simAlgae = Game.SharedState.Get<SimAlgaeState>();
            bool bloomPeaked = simAlgae.Algae.PeakingTiles.Contains(tile.TileIndex);

            if (bloomPeaked) {
                EventActorUtility.QueueAlert(actor, EventActorAlertType.Bloom, tile.TileIndex, tile.RegionIndex);

                // TODO: seems like we should be able to clear this at the start of SimAlgaeSystem, since this system comes after.
                // But for some reason some tiles aren't processed before SimAlgaeSystem starts again.
                // So for now, this system is responsible for removing the PeakingTiles from the hash set.
                simAlgae.Algae.PeakingTiles.Remove(tile.TileIndex);
            }

        }
    }
}