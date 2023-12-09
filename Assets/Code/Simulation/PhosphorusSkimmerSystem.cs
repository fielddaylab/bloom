using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public class PhosphorusSkimmerSystem : ComponentSystemBehaviour<PhosphorusSkimmer, ActorTimer, OccupiesTile>
    {
        public override void ProcessWorkForComponent(PhosphorusSkimmer skimmer, ActorTimer timer, OccupiesTile position, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            if (skimmer.Type == SkimmerType.Algae) {
                SimAlgaeState aState = Game.SharedState.Get<SimAlgaeState>();
                float removedAmt = SimAlgaeUtility.RemoveAlgae(aState, position.TileIndex, skimmer.AlgaeSkimAmt);
                Debug.Log("[Skimmer] Skimmed " + removedAmt + "  units of Algae");
            } else if (skimmer.Type == SkimmerType.Dredge) {
                // Remove P from tile
                SimPhosphorusState pState = Game.SharedState.Get<SimPhosphorusState>();
                int removedAmt = SimPhospohorusUtility.RemovePhosphorus(pState, position.TileIndex, skimmer.PhosSkimAmt);
                Debug.Log("[Skimmer] Dredged " + removedAmt + "  units of P");
            }
        }
    }
}