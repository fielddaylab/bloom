using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Systems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zavala.Actors;
using Zavala.Advisor;
using Zavala.Economy;
using Zavala.Rendering;
using Zavala.World;

namespace Zavala.Sim {
    [SysUpdate(GameLoopPhase.Update, -48, ZavalaGame.SimulationUpdateMask)] // after algae system
    public class PhosphorusSkimmerSystem : SharedStateSystemBehaviour<PhosphorusSkimmerState, SimPhosphorusState, SimAlgaeState, PolicyState> {

        public override void ProcessWork(float deltaTime) {
            if (!m_StateA.SkimTimer.Advance(deltaTime, ZavalaGame.SimTime)) {
                return;
            }

            // TODO: iterate only through unlocked regions/regions where skimmer policy has been set
            for (int i = 0; i < RegionInfo.MaxRegions; i++) {
                ProcessSkimmerRegion(m_StateA.SkimmerLocsPerRegion[i], i);
            }
        }

        #region Helper Methods

        private void ProcessSkimmerRegion(List<SkimmerLocation> skimmerList, int regionIndex) {
            int regionCost = 0;
            foreach (SkimmerLocation loc in skimmerList) {
                if (loc.PlacedSkimmer == null) continue;
                regionCost += ProcessSkimmer(loc.PlacedSkimmer, loc.TileIndex, regionIndex);
            }
            if (regionCost == 0) return;
            TryPayForSkimmerRegion(regionCost, regionIndex);

        }

        private void TryPayForSkimmerRegion(int cost, int region) {
            MarketData market = Game.SharedState.Get<MarketData>();
            BudgetData budget = Game.SharedState.Get<BudgetData>();
            if (BudgetUtility.TrySpendBudget(budget, cost, (uint)region)) {
                MarketUtility.RecordSkimmerCostToHistory(market, -cost, region);
            } else {
                string actor = "region" + (region + 1) + "_city1";
                PolicyUtility.ForcePolicyToNone(PolicyType.SkimmingPolicy, actor, region);
            }
        }

        private int ProcessSkimmer(PhosphorusSkimmer skimmer, int tileIndex, int regionIndex) {
            int returnCost = 0;
            float removedAmt = 0;
            if (skimmer.Type == SkimmerType.Algae) {
                returnCost = SkimmerParams.AlgaeSkimCost;
                removedAmt = SimAlgaeUtility.RemoveAlgae(m_StateC, tileIndex, SkimmerParams.AlgaeSkimAmt, regionIndex);
                foreach (int idx in skimmer.NeighborIndices) {
                    if (idx < 0) continue;
                    removedAmt += SimAlgaeUtility.RemoveAlgae(m_StateC, idx, SkimmerParams.AlgaeSkimAmt / 2, regionIndex);
                }
                Debug.Log("[Skimmer] Skimmed " + removedAmt + "  units of Algae");
            } else if (skimmer.Type == SkimmerType.Dredge) {
                // Remove P from tile
                returnCost = SkimmerParams.PhosDredgeCost;
                removedAmt = SimPhospohorusUtility.RemovePhosphorus(m_StateB, tileIndex, SkimmerParams.PhosDredgeAmt);
                foreach (int idx  in skimmer.NeighborIndices) {
                    if (idx < 0) continue;
                    removedAmt += SimPhospohorusUtility.RemovePhosphorus(m_StateB, idx, SkimmerParams.PhosDredgeAmt / 2);
                }
                Debug.Log("[Skimmer] Dredged " + removedAmt + "  units of P");
            }
            // TODO:Currently uses Mary's skimmer particles, which have a new ParticleSystem on every Skimmer.
            //      This is less efficient than using the single ParticleSystem through VfxUtility,
            //      but I wasn't able to make the single ParticleSystem rotate properly to align with each Skimmer.
            //      I tried using EmitParams.rotation, EmitParams.rotation3D, and ParticleSystem.shape.rotation - none were quite right
            if (removedAmt != 0) {
                VfxUtility.PlayEffect(skimmer.transform.position, EffectType.Algae_Remove);
                skimmer.SkimParticles.Play();
                return returnCost;
            } else return 0;
        }

        #endregion

    }

    public static class SkimmerParams {
        [ConfigVar("Algae Skimmer Cost", 0, 5, 1)] static public int AlgaeSkimCost = 1;
        [ConfigVar("Phos Dredge Cost", 0, 10, 1)] static public int PhosDredgeCost = 2;
        [ConfigVar("Algae Skim Amount", 0f, 0.4f, 0.04f)] static public float AlgaeSkimAmt = 0.16f;
        [ConfigVar("Phos Dredge Amount", 0, 10, 2)] static public int PhosDredgeAmt = 8;

    }
}