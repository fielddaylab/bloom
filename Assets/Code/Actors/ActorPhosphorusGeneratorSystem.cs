using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Actors {
    [SysUpdate(GameLoopPhase.Update, 0)]
    public sealed class ActorPhosphorusGeneratorSystem : ComponentSystemBehaviour<ActorPhosphorusGenerator, ActorTimer> {
        public override void ProcessWork(float deltaTime) {
            SimGridState grid = ZavalaGame.SimGrid;
            SimWorldState world = ZavalaGame.SimWorld;
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