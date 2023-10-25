using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Economy {
    [SysUpdate(GameLoopPhase.Update, 10, ZavalaGame.SimulationUpdateMask)]
    public sealed class RequestFulfillmentSystem : ComponentSystemBehaviour<RequestFulfiller> {
        private static int AIRSHIP_SPAWN_DIST = 10;
        private static float AIRSHIP_HOVER_HEIGHT = 3f; // TODO: calculate heighest height, or handle about to run into a higher tile

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
                        // Straight to blimp
                        // Spawn blimp from nebulous external supplier
                        ExternalState externalState = Game.SharedState.Get<ExternalState>();

                        // blimps start a certain distance from the requester, in the direction of the supplier
                        Vector3 externalSrcPos = request.Requester.transform.position + externalState.ExternalDepot.transform.position.normalized * AIRSHIP_SPAWN_DIST;
                        externalSrcPos.y = request.Requester.transform.position.y + AIRSHIP_HOVER_HEIGHT;

                        // differentiate between external and internal blimp prefabs
                        request.Fulfiller = pools.ExternalAirships.Alloc();
                        FulfillerUtility.InitializeFulfiller(request.Fulfiller, request, externalSrcPos);

                        OrientAirship(ref request.Fulfiller);

                        marketData.ActiveRequests.PushBack(request);
                    }
                    else {
                        // create a truck
                        request.Fulfiller = pools.Trucks.Alloc();
                        FulfillerUtility.InitializeFulfiller(request.Fulfiller, request);

                        // divvy route between trucks and blimps through proxy, if applicable
                        if (request.ProxyIdx != -1) {
                            // truck will be conferring to blimp
                            request.Fulfiller.IsIntermediary = true;

                            // Override the target position to the export depot, not the final target
                            request.Fulfiller.TargetTileIndex = request.ProxyIdx;
                            request.Fulfiller.TargetWorldPos = SimWorldUtility.GetTileCenter(ZavalaGame.SimGrid.HexSize.FastIndexToPos(request.ProxyIdx));
                        }

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
            switch (component.FulfillerType) {
                case FulfillerType.Truck:
                    ProcessFulfillerTruck(marketData, pools, component, visualState, deltaTime);
                    break;
                case FulfillerType.Airship:
                    ProcessFulfillerAirship(marketData, pools, component, visualState, deltaTime);
                    break;
                case FulfillerType.Parcel:
                    ProcessFulfillerParcel(marketData, pools, component, visualState, deltaTime);
                    break;
                default:
                    break;
            }
           
        }

        private void ProcessFulfillerTruck(MarketData marketData, MarketPools pools, RequestFulfiller component, RequestVisualState visualState, float deltaTime) {
            Vector3 newPos = Vector3.MoveTowards(component.transform.position, component.TargetWorldPos, 3 * deltaTime);
            if (Mathf.Approximately(Vector3.Distance(newPos, component.TargetWorldPos), 0)) {
                if (component.IsIntermediary) {
                    // create a new blimp w/ src export depot + yOffset, target is request destination; confer fulfiller role
                    int index = marketData.ActiveRequests.FindIndex(FindRequestForFulfiller, component);
                    if (index < 0) {
                        // A request that has a fulfiller was fulfilled by other means. Shouldn't be able to happen.
                        Debug.LogError("[RequestFulfillmentSystem] En-route fulfiller has no corresponding request to fulfill. Cannot confer fulfillment role.");
                        return;
                    }

                    // differentiate between external and internal blimp prefabs
                    RequestFulfiller newFulfiller = pools.InternalAirships.Alloc();
                    FulfillerUtility.InitializeFulfiller(newFulfiller, marketData.ActiveRequests[index], component.transform.position);

                    OrientAirship(ref newFulfiller);

                    marketData.ActiveRequests[index].Fulfiller = newFulfiller;
                }
                else {
                    DeliverFulfillment(marketData, component, visualState);
                }
                pools.Trucks.Free(component);
            }
            else {
                component.transform.position = newPos;
            }
        }

        private void ProcessFulfillerAirship(MarketData marketData, MarketPools pools, RequestFulfiller component, RequestVisualState visualState, float deltaTime) {
            // when same position, drop parcel
            if (component.AtTransitionPoint) {
                int index = marketData.ActiveRequests.FindIndex(FindRequestForFulfiller, component);
                if (index < 0) {
                    // A request that has a fulfiller was fulfilled by other means. Shouldn't be able to happen.
                    Debug.LogError("[RequestFulfillmentSystem] En-route fulfiller has no corresponding request to fulfill. Cannot confer fulfillment role.");
                    return;
                }

                if (component.IsIntermediary) {
                    // create a new parcel w/ src airship position, target tile below; pass on fulfiller role
                    RequestFulfiller newFulfiller = pools.Parcels.Alloc();
                    FulfillerUtility.InitializeFulfiller(newFulfiller, marketData.ActiveRequests[index], component.transform.position);
                    marketData.ActiveRequests[index].Fulfiller = newFulfiller;
                }
            }
        }

        private void ProcessFulfillerParcel(MarketData marketData, MarketPools pools, RequestFulfiller component, RequestVisualState visualState, float deltaTime) {
            // move parcel from src to target
            Vector3 newPos = Vector3.MoveTowards(component.transform.position, component.TargetWorldPos, 3 * deltaTime);

            // when same position, deliver resource
            if (Mathf.Approximately(Vector3.Distance(newPos, component.TargetWorldPos), 0)) {
                DeliverFulfillment(marketData, component, visualState);
                pools.Parcels.Free(component);
            }
            else {
                component.transform.position = newPos;
            }
        }

        private void OrientAirship(ref RequestFulfiller fulfiller) {
            // blimp will be conferring to parcel
            fulfiller.IsIntermediary = true;

            // override target position to include hover
            fulfiller.TargetWorldPos = new Vector3(fulfiller.TargetWorldPos.x, fulfiller.TargetWorldPos.y + AIRSHIP_HOVER_HEIGHT, fulfiller.TargetWorldPos.z);

            // orient the airship, since it doesn't follow roads
            fulfiller.transform.LookAt(fulfiller.TargetWorldPos);
        }

        private void DeliverFulfillment(MarketData marketData, RequestFulfiller component, RequestVisualState visualState) {
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
        }

        static private Predicate<MarketActiveRequestInfo, RequestFulfiller> FindRequestForFulfiller = (a, b) => a.Fulfiller == b;
    }
}