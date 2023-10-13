using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Actors {
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class ActorPhosphorusGeneratorSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer, ResourceStorage, OccupiesTile> {
        public override void ProcessWork(float deltaTime) {
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();

            foreach(var componentGroup in m_Components) {
                if (componentGroup.ComponentA.HasAdvanced()) {
                    int index = componentGroup.ComponentC.TileIndex;

                    int manureMod = 3;
                    int mFertilizerMod = 2;
                    int dFertilizerMod = 1;

                    int totalToAdd = componentGroup.Primary.Amount * (
                        componentGroup.ComponentB.Current.Manure * manureMod +
                        componentGroup.ComponentB.Current.MFertilizer * mFertilizerMod +
                        componentGroup.ComponentB.Current.DFertilizer * dFertilizerMod
                        );

                    SimPhospohorusUtility.AddPhosphorus(phosphorus, index, totalToAdd);
                    componentGroup.Primary.AmountProducedLastTick = totalToAdd;
                }
            }
        }
    }
}