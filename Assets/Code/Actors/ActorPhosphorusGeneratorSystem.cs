using FieldDay;
using FieldDay.Systems;
using Zavala.Sim;

namespace Zavala.Actors {
    [SysUpdate(FieldDay.GameLoopPhase.Update, 0)]
    public sealed class ActorPhosphorusGeneratorSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer> {
        public override void ProcessWork(float deltaTime) {
            SimGridState grid = Game.SharedState.Get<SimGridState>();
            SimWorldState world = Game.SharedState.Get<SimWorldState>();
            SimPhosphorusState phosphorus = Game.SharedState.Get<SimPhosphorusState>();

            foreach(var componentPair in m_Components) {
                if (componentPair.Secondary.HasAdvanced()) {
                    HexVector vec = HexVector.FromWorld(componentPair.Primary.transform.position, world.WorldSpace);
                    if (grid.HexSize.IsValidPos(vec)) {
                        int index = grid.HexSize.FastPosToIndex(vec);
                        SimPhospohorusUtility.AddPhosphorus(phosphorus, index, componentPair.Primary.Amount);
                    }
                }
            }
        }
    }
}