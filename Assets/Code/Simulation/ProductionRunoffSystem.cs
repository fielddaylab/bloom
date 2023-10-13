using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Actors
{
    /// <summary>
    ///  Speciifcally, Grain Production
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class ProductionRunoffSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ResourceProducer, OccupiesTile>
    {
        public override void ProcessWork(float deltaTime) {
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();

            foreach (var componentGroup in m_Components) {
                if (componentGroup.ComponentA.ProducedLastTick) {
                    Debug.Log("[Sitting] producer " + componentGroup.ComponentA.name);
                    componentGroup.ComponentA.ProducedLastTick = false;

                    int index = componentGroup.ComponentB.TileIndex;
                    // TODO: doesn't work on grain farms
                    ResourceBlock lastProduced = componentGroup.ComponentA.Produces;
                    ResourceBlock lastRequired = componentGroup.ComponentA.Requires;
                    if (lastRequired.PhosphorusCount == 0) {
                        // Only apply to phosph application on grain farm
                        continue;
                    }
                    if (lastProduced.Grain == 0) {
                        continue;
                    }

                    int manureMod = 3;
                    int mFertilizerMod = 2;
                    int dFertilizerMod = 1;

                    int totalToAdd = componentGroup.Primary.Amount * (
                        lastRequired.Manure * manureMod +
                        lastRequired.MFertilizer * mFertilizerMod +
                        lastRequired.DFertilizer * dFertilizerMod
                        );

                    Debug.Log("[Sitting] Added " + totalToAdd + " via grain farm phosphorus application");
                    SimPhospohorusUtility.AddPhosphorus(phosphorus, index, totalToAdd);
                    componentGroup.Primary.AmountProducedLastTick = totalToAdd;
                }
            }
        }
    }
}