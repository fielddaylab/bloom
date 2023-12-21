using System;
using System.Collections.Generic;
using System.Numerics;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.Actors
{
    [SysUpdate(GameLoopPhase.Update, 10, ZavalaGame.SimulationUpdateMask)]
    public sealed class BloomStressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer, BloomStressable>
    {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, BloomStressable bloomStress, float deltaTime)
        {
            if (!timer.Timer.HasAdvanced())
            {
                return;
            }

            SimAlgaeState algaeState = Game.SharedState.Get<SimAlgaeState>();

            bool anyAdjacent = false;
            foreach(int index in algaeState.Algae.GrowingTiles)
            {
                if (ZavalaGame.SimGrid.HexSize.FastIsNeighbor(actor.Position.TileIndex, index, out var _))
                {
                    anyAdjacent = true;
                    bloomStress.TriggerCounter++;
                    if (bloomStress.TriggerCounter >= bloomStress.NumTriggersPerStressTick)
                    {
                        StressUtility.IncrementStress(actor, StressCategory.Bloom);
                        bloomStress.TriggerCounter = 0;
                    }
                }
            }

            if (!anyAdjacent)
            {
                // decrease stress
                bloomStress.TriggerCounter++;
                if (bloomStress.TriggerCounter >= bloomStress.NumTriggersPerStressTick)
                {
                    StressUtility.DecrementStress(actor, StressCategory.Bloom);
                    bloomStress.TriggerCounter = 0;
                }
            }

            // TODO: If number of shrinking > number of GrowingTiles, start decreasing

        }
    }
}