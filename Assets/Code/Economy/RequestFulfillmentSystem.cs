using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Actors;
using Zavala.Sim;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 10, ZavalaGame.SimulationUpdateMask)]
    public sealed class RequestFulfillmentSystem : ComponentSystemBehaviour<RequestFulfiller> {
        public override bool HasWork() {
            return isActiveAndEnabled;
        }

        public override void ProcessWork(float deltaTime) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            MarketPools pools = Game.SharedState.Get<MarketPools>();
            SimTimeState timeState = ZavalaGame.SimTime;
            RequestVisualState visualState = Game.SharedState.Get<RequestVisualState>();

            ProcessFulfillmentQueue(marketData, pools);
            deltaTime = SimTimeUtility.AdjustedDeltaTime(deltaTime, timeState);

            foreach (var component in m_Components) {
                ProcessFulfiller(marketData, pools, component, visualState, deltaTime);
            }
        }

        private void ProcessFulfillmentQueue(MarketData marketData, MarketPools pools) {
            while (marketData.FulfillQueue.TryPopFront(out MarketActiveRequestInfo request)) {
                if (!request.Requester.IsLocalOption) {
                    if (request.Supplier.Position.IsExternal) {
                        // TODO: Straight to blimp
                    }
                    else {
                        // TODO: divvy route between trucks and blimps
                        if (request.ProxyIdx != -1) {
                            // when using export Depot, final destination is the target. ProxyIdx is the export depot.
                            // Move toward export depot as a truck. Then move toward final destination as blimp.
                            // intitialize fulfiller with override?
                        }
                        request.Fulfiller = pools.Trucks.Alloc();
                        FulfillerUtility.InitializeFulfiller(request.Fulfiller, request);
                        request.Fulfiller.transform.position = request.Fulfiller.SourceWorldPos;
                        marketData.ActiveRequests.PushBack(request);
                    }
                }
                else {
                    // Skip fulfillment system, deliver directly to local tile
                    request.Requester.Received += request.Requested;
                    request.Requester.RequestCount--;
                    ResourceStorageUtility.RefreshStorageDisplays(request.Supplier.Storage);

                    // Add generated revenue
                    BudgetData budgetData = Game.SharedState.Get<BudgetData>();
                    int revenueAmt = request.Revenue.Sales + request.Revenue.Import + request.Revenue.Penalties;
                    BudgetUtility.AddToBudget(budgetData, revenueAmt, request.Requester.Position.RegionIndex);
                }
            }
        }

        private void ProcessFulfiller(MarketData marketData, MarketPools pools, RequestFulfiller component, RequestVisualState visualState, float deltaTime) {
            Vector3 newPos = Vector3.MoveTowards(component.transform.position, component.TargetWorldPos, 3 * deltaTime);
            if (Mathf.Approximately(Vector3.Distance(newPos, component.TargetWorldPos), 0)) {
                component.Target.Received += component.Carrying;
                component.Target.RequestCount--;
                Log.Msg("[RequestFulfillmentSystem] Shipment of {0} received by '{1}'", component.Carrying, component.Target.name);
                DebugDraw.AddWorldText(component.Target.transform.position, "Received!", Color.black, 2, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundLightOpaque);
                ResourceStorageUtility.RefreshStorageDisplays(component.Target.Storage);

                // Add generated revenue
                BudgetData budgetData = Game.SharedState.Get<BudgetData>();
                int revenueAmt = component.Revenue.Sales + component.Revenue.Import + component.Revenue.Penalties;
                BudgetUtility.AddToBudget(budgetData, revenueAmt, component.Target.Position.RegionIndex);

                int index = marketData.ActiveRequests.FindIndex(FindRequestForFulfiller, component);
                if (index >= 0) {
                    MarketActiveRequestInfo fulfilling = marketData.ActiveRequests[index];
                    visualState.FulfilledQueue.PushBack(fulfilling);
                    marketData.ActiveRequests.FastRemoveAt(index);                    
                }
                pools.Trucks.Free(component);
            } else {
                component.transform.position = newPos;
            }
        }

        static private Predicate<MarketActiveRequestInfo, RequestFulfiller> FindRequestForFulfiller = (a, b) => a.Fulfiller == b;
    }
}