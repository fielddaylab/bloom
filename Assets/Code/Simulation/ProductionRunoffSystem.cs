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
    ///  Speciifcally, phosphorus application during Grain production
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class ProductionRunoffSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ResourceProducer, OccupiesTile>
    {
        public override bool HasWork() {
            if (base.HasWork()) {
                // disable phosphorus generation for tutorial
                return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
            }

            return false;
        }

        public override void ProcessWork(float deltaTime) {
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();

            foreach (var componentGroup in m_Components) {
                if (componentGroup.ComponentA.ProducedLastTick) {
                    Debug.Log("[Sitting] producer " + componentGroup.ComponentA.name);
                    componentGroup.ComponentA.ProducedLastTick = false;

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

                    int index = componentGroup.ComponentB.TileIndex;

                    SimPhospohorusUtility.GenerateProportionalPhosphorus(phosphorus, index, componentGroup.Primary, lastRequired);
                }
            }
        }
    }
}