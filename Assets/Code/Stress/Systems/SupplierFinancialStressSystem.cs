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
                if (supplier.MatchedThisTick && !supplier.MatchedThisTickWasMilk)
                {
                    financeStress.NonMilkSoldSinceLast++;
                    // EXCLUDE MILK
                    // MatchedSinceLast should be equal to the number of finalized that were were not milk
                }
                
                if (supplier.SoldAtALossExcludingMilk)
                {
                    financeStress.SoldAtLossSinceLast++;
                    // EXCLUDE MILK
                    // SoldAtLossSinceLast Doesn't include milk sales
                }
            }

            if (!timer.Timer.HasAdvanced())
            {
                return;
            }

            int soldUnstressed = financeStress.NonMilkSoldSinceLast - financeStress.SoldAtLossSinceLast;
            Log.Debug("[SupplierFinancialStressSystem] NonMilkSold: {0}, SoldAtLoss: {1}", financeStress.NonMilkSoldSinceLast, financeStress.SoldAtLossSinceLast);

            // TODO: may need to shift this to AFTER market system?
            if (financeStress.NonMilkSoldSinceLast > 0 && financeStress.SoldAtLossSinceLast >= soldUnstressed)
            { // if we've sold something (not milk), but sold more things at a loss than not:
                financeStress.TriggerCounter++;
                if (financeStress.TriggerCounter >= financeStress.NumTriggersPerStressTick)
                {
                    StressUtility.IncrementStress(actor, StressCategory.Financial);
                    financeStress.TriggerCounter = 0;
                }
            }
            else if (soldUnstressed > 0)
            {
                // decrease stress
                financeStress.TriggerCounter++;
                if (financeStress.TriggerCounter >= financeStress.NumTriggersPerStressTick)
                {
                    StressUtility.DecrementStress(actor, StressCategory.Financial);
                    financeStress.TriggerCounter = 0;
                }
            }

            financeStress.NonMilkSoldSinceLast = 0;
            financeStress.SoldAtLossSinceLast = 0;
        }
    }
}