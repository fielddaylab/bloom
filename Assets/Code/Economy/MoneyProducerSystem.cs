using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 0, ZavalaGame.SimulationUpdateMask)]
    public sealed class MoneyProducerSystem : ComponentSystemBehaviour<MoneyProducer, ActorTimer, OccupiesTile> {
        public override void ProcessWorkForComponent(MoneyProducer producer, ActorTimer timer, OccupiesTile position, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            MarketData marketData = Game.SharedState.Get<MarketData>();

            if (MarketUtility.CanProduceNow(producer, out int producedAmt)) {
                ResourceBlock consumed = producer.Requires;
                ResourceBlock.Consume(ref producer.Storage.Current, ref consumed);
                
                BudgetData budgetData = Game.SharedState.Get<BudgetData>();
                BudgetUtility.AddToBudget(budgetData, producedAmt, position.RegionIndex);
                
                MarketUtility.RecordMilkRevenueToHistory(marketData, producedAmt, position.RegionIndex);

                Log.Debug("[MoneyProducerSystem] Producer '{0}' consumed {1} to produce {2} money units", producer.name, consumed, producedAmt);
                // TODO: events?
                DebugDraw.AddWorldText(producer.transform.position, "Produced $!", Color.green, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
                ResourceStorageUtility.RefreshStorageDisplays(producer.Storage);

            } else {
                MarketUtility.RecordMilkRevenueToHistory(marketData, 0, position.RegionIndex);
            }
        }
    }
}