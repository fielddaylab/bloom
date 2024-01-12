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

namespace Zavala.Actors {
    [SysUpdate(GameLoopPhase.Update, 15, ZavalaGame.SimulationUpdateMask)] // After stress system subcategories
    public sealed class StressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer> {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }

            if (actor.ChangedOperationThisTick)
            {
                int timerDelta;
                switch (actor.Position.Type)
                {
                    case BuildingType.GrainFarm:
                        timerDelta = actor.PrevState < actor.OperationState ? -actor.StressDelta : actor.StressDelta;
                        timer.AdustTimer(timerDelta);
                        break;
                    case BuildingType.DairyFarm:
                        timerDelta = actor.PrevState < actor.OperationState ? -actor.StressDelta : actor.StressDelta;
                        timer.AdustTimer(timerDelta);
                        break;
                    case BuildingType.City:
                        ResourcePurchaser rp = actor.GetComponent<ResourcePurchaser>();
                        int demandDelta = actor.PrevState < actor.OperationState ? -actor.StressDelta : actor.StressDelta;
                        rp.ChangeDemandAmount(ResourceId.Milk, demandDelta);
                        break;
                    default:
                        break;
                }
            }

            actor.ChangedOperationThisTick = false;

            if (actor.Position.Type == BuildingType.City) {
                WinLossState winLossState = Game.SharedState.Get<WinLossState>();
                if (actor.OperationState == OperationState.Bad) {
                    winLossState.CityFallingTimersPerRegion[actor.Position.RegionIndex] += 1;
                    // if you have multiple cities, each will increment.
                } else {
                    winLossState.CityFallingTimersPerRegion[actor.Position.RegionIndex] = 0;
                    // BUT any of them healing will make it better
                }
            }
        }
    }
}