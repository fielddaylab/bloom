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
    public sealed class RequesterFinancialStressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer, RequesterFinancialStressable, ResourceRequester>
    {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, RequesterFinancialStressable financeStress, ResourceRequester requester, float deltaTime)
        {
            MarketData market = Game.SharedState.Get<MarketData>();
            if (market.MarketTimer.HasAdvanced())
            {
                if (requester.MatchedThisTick)
                {
                    financeStress.MatchedSinceLast++;
                    // check for subsidy
                }

                if (requester.PurchasedAtStressedPrice)
                {
                    financeStress.PurchasedStressedSinceLast++;
                }
            }

            if (!timer.Timer.HasAdvanced())
            {
                return;
            }

            int purchasedUnstressed = financeStress.MatchedSinceLast - financeStress.PurchasedStressedSinceLast;

            // TODO: may need to shift this to AFTER market system?
            if (financeStress.MatchedSinceLast > 0 && financeStress.PurchasedStressedSinceLast >= purchasedUnstressed)
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
                // decrease stress every not loss sale
                financeStress.TriggerCounter++;
                if (financeStress.TriggerCounter >= financeStress.NumTriggersPerStressTick)
                {
                    StressUtility.DecrementStress(actor, StressCategory.Financial);
                    financeStress.TriggerCounter = 0;
                }
            }

            financeStress.MatchedSinceLast = 0;
            financeStress.PurchasedStressedSinceLast = 0;
        }
    }
}