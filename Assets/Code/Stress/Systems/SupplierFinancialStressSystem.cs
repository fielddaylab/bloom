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
    public sealed class SupplierFinancialStressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer, SupplierFinancialStressable, ResourceSupplier>
    {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, SupplierFinancialStressable financeStress, ResourceSupplier supplier, float deltaTime)
        {
            MarketData market = Game.SharedState.Get<MarketData>();
            if (market.MarketTimer.HasAdvanced())
            {
                if (supplier.MatchedThisTick)
                {
                    financeStress.MatchedSinceLast++;
                }

                if (supplier.SoldAtALoss)
                {
                    financeStress.SoldAtLossSinceLast++;
                }
            }

            if (!timer.Timer.HasAdvanced())
            {
                return;
            }

            int soldUnstressed = financeStress.MatchedSinceLast - financeStress.SoldAtLossSinceLast;

            // TODO: may need to shift this to AFTER market system?
            if (financeStress.MatchedSinceLast > 0 && financeStress.SoldAtLossSinceLast >= soldUnstressed)
            {
                financeStress.TriggerCounter++;
                if (financeStress.TriggerCounter >= financeStress.NumTriggersPerStressTick)
                {
                    StressUtility.IncrementStress(actor, StressCategory.Financial);
                    financeStress.TriggerCounter = 0;
                }
            }
            else
            {
                // decrease stress
                financeStress.TriggerCounter++;
                if (financeStress.TriggerCounter >= financeStress.NumTriggersPerStressTick)
                {
                    StressUtility.DecrementStress(actor, StressCategory.Financial);
                    financeStress.TriggerCounter = 0;
                }
            }

            financeStress.MatchedSinceLast = 0;
            financeStress.SoldAtLossSinceLast = 0;
        }
    }
}