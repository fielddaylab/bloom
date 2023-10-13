using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;
using Zavala.Scripting;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)] // after SimAlgaeSystem
    // Set execution order to after SimAlgaeStateSystem
    public class BloomGroupAlertSystem : ComponentSystemBehaviour<EventActor, WaterGroupInstance>
    {
        public unsafe override void ProcessWorkForComponent(EventActor actor, WaterGroupInstance group, float deltaTime) {
            if (EventActorUtility.IsAlertQueued(actor, EventActorAlertType.Bloom)) {
                // only add this trigger once
                return;
            }
            bool bloomPeaked = false;

            SimAlgaeState simAlgae = Game.SharedState.Get<SimAlgaeState>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            WaterGroupInfo groupInfo = grid.WaterGroups[group.GroupIndex];
            int tileIndex = -1;
            for(int i = 0; i < groupInfo.TileCount && !bloomPeaked; i++) {
                tileIndex = groupInfo.TileIndices[i];
                bloomPeaked = simAlgae.Algae.PeakingTiles.Contains(tileIndex);
            }

            if (bloomPeaked) {
                EventActorUtility.QueueAlert(actor, EventActorAlertType.Bloom, tileIndex);

                // TODO: seems like we should be able to clear this at the start of SimAlgaeSystem, since this system comes after.
                // But for some reason some tiles aren't processed before SimAlgaeSystem starts again.
                // So for now, this system is responsible for removing the PeakingTiles from the hash set.
                simAlgae.Algae.PeakingTiles.Remove(tileIndex);
            }

        }
    }
}