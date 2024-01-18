using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.UI;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)] // after SimAlgaeSystem
    // Set execution order to after SimAlgaeStateSystem
    public class BloomGroupAlertSystem : ComponentSystemBehaviour<EventActor, ActorTimer, WaterGroupInstance>
    {
        public unsafe override void ProcessWorkForComponent(EventActor actor, ActorTimer timer, WaterGroupInstance group, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            SimAlgaeState simAlgae = Game.SharedState.Get<SimAlgaeState>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.Bloom)) {
                if (!AnyTilesBloomed(simAlgae, grid.WaterGroups[group.GroupIndex])) {
                    Log.Msg("[BloomGroupAlertSystem] Bloom receding, attempted to cancel alert");
                    UIAlertUtility.ClearAlert(actor.DisplayingEvent);
                    //EventActorUtility.CancelEventType(actor, EventActorAlertType.Bloom);
                }
                return;
            }
            // bool bloomPeaked = false;
            WaterGroupInfo groupInfo = grid.WaterGroups[group.GroupIndex];
            if (AnyTilesPeaked(simAlgae, groupInfo, out int firstPeaked)) {
                EventActorUtility.QueueAlert(actor, EventActorAlertType.Bloom, firstPeaked, groupInfo.RegionId);

                // TODO: seems like we should be able to clear this at the start of SimAlgaeSystem, since this system comes after.
                // But for some reason some tiles aren't processed before SimAlgaeSystem starts again.
                // So for now, this system is responsible for removing the PeakingTiles from the hash set.
                simAlgae.Algae.PeakingTiles.Remove(firstPeaked);
            }

        }

        private unsafe bool AnyTilesPeaked(SimAlgaeState algaeState, WaterGroupInfo groupInfo, out int firstPeaked) {
            bool anyPeaked = false;
            int tileIndex = -1;
            for (int i = 0; i < groupInfo.TileCount && !anyPeaked; i++) {
                tileIndex = groupInfo.TileIndices[i];
                anyPeaked = algaeState.Algae.PeakingTiles.Contains(tileIndex);
            }
            firstPeaked = tileIndex;
            return anyPeaked;
        }

        private unsafe bool AnyTilesBloomed(SimAlgaeState algaeState, WaterGroupInfo groupInfo) {
            bool anyBloomed = false;
            int tileIndex = -1;
            for (int i = 0; i < groupInfo.TileCount && !anyBloomed; i++) {
                tileIndex = groupInfo.TileIndices[i];
                anyBloomed = algaeState.Algae.BloomedTiles.Contains(tileIndex);
            }
            return anyBloomed;
        }
    }
}