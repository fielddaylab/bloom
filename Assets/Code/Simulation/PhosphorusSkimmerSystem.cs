using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
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
                float removedAmt = SimAlgaeUtility.RemoveAlgae(aState, position.TileIndex, SkimmerParams.AlgaeSkimAmt, position.RegionIndex);
                Debug.Log("[Skimmer] Skimmed " + removedAmt + "  units of Algae");
                foreach (int idx in skimmer.NeighborIndices) {
                    if (idx < 0) continue;
                    float bonusAmt = SimAlgaeUtility.RemoveAlgae(aState, idx, SkimmerParams.AlgaeSkimAmt/2, position.RegionIndex);
                    Debug.Log("[Skimmer] Skimmed " + bonusAmt + "  units of Algae");

                }
            } else if (skimmer.Type == SkimmerType.Dredge) {
                // Remove P from tile
                SimPhosphorusState pState = Game.SharedState.Get<SimPhosphorusState>();
                int removedAmt = SimPhospohorusUtility.RemovePhosphorus(pState, position.TileIndex, SkimmerParams.PhosDredgeAmt);
                foreach (int idx in skimmer.NeighborIndices) {
                    if (idx < 0) continue;
                    removedAmt += SimPhospohorusUtility.RemovePhosphorus(pState, idx, SkimmerParams.PhosDredgeAmt/2);
                }
                Debug.Log("[Skimmer] Dredged " + removedAmt + "  units of P");
            }
        }
    }

    public static class SkimmerParams {
        [ConfigVar("Algae Skim Amount", 0f, 0.4f, 0.04f)] static public float AlgaeSkimAmt = 0.16f;
        [ConfigVar("Phos Dredge Amount", 0, 10, 2)] static public int PhosDredgeAmt = 8;

    }
}