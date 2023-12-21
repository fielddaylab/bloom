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

namespace Zavala.Actors
{
    [SysUpdate(GameLoopPhase.Update, 10, ZavalaGame.SimulationUpdateMask)]
    public sealed class ResourceStressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer, ResourceStressable, ResourceRequester>
    {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, ResourceStressable resourceStress, ResourceRequester requester, float deltaTime)
        {
            if (!timer.Timer.HasAdvanced())
            {
                return;
            }

            RequestVisualState visualState = Game.SharedState.Get<RequestVisualState>();
            if (visualState.UrgentMap.ContainsKey(requester) && visualState.UrgentMap[requester] != 0)
            {
                resourceStress.TriggerCounter++;
                if (resourceStress.TriggerCounter >= resourceStress.NumTriggersPerStressTick)
                {
                    StressUtility.IncrementStress(actor, StressCategory.Resource);
                    resourceStress.TriggerCounter = 0;
                }
            }
            else
            {
                // decrease stress
                resourceStress.TriggerCounter++;
                if (resourceStress.TriggerCounter >= resourceStress.NumTriggersPerStressTick)
                {
                    StressUtility.DecrementStress(actor, StressCategory.Resource);
                    resourceStress.TriggerCounter = 0;
                }
            }
        }
    }
}