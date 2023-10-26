using System;
using System.Collections.Generic;
using System.Linq;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Roads;
using Zavala.Sim;

namespace Zavala.Economy
{
    [SysUpdate(GameLoopPhase.Update, 5)]
    public sealed class MarketSystem : SharedStateSystemBehaviour<MarketData, MarketConfig, SimGridState, RequestVisualState>
    {

        // NOT persistent state - work lists for various updates
        private readonly RingBuffer<MarketRequestInfo> m_RequestWorkList = new RingBuffer<MarketRequestInfo>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ResourceSupplier> m_SupplierWorkList = new RingBuffer<ResourceSupplier>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ResourceRequester> m_RequesterWorkList = new RingBuffer<ResourceRequester>(8, RingBufferMode.Expand);
        private readonly RingBuffer<MarketSupplierPriorityInfo> m_PriorityWorkList = new RingBuffer<MarketSupplierPriorityInfo>(8, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime) {
            SimTimeState time = ZavalaGame.SimTime;

            MoveReceivedToStorage();

            bool marketCycle = m_StateA.MarketTimer.Advance(deltaTime, time);
            if (marketCycle) {
                // TODO: Only update when necessary
                UpdateAllSupplierPriorities();
                ProcessMarketCycle();
            }
        }

        private void ProcessMarketCycle() {
            m_StateA.RequestQueue.CopyTo(m_RequestWorkList);
            m_StateA.RequestQueue.Clear();

            m_StateA.Suppliers.CopyTo(m_SupplierWorkList);

            SimGridState grid = ZavalaGame.SimGrid;
            grid.Random.Shuffle(m_SupplierWorkList);

            MarketData marketData = Game.SharedState.Get<MarketData>();

            foreach (var supplier in m_SupplierWorkList) {
                // reset sold at a loss
                supplier.SoldAtALoss = false;
                MarketRequestInfo? found = FindHighestPriorityBuyer(supplier, m_RequestWorkList, out int baseProfit, out GeneratedTaxRevenue baseTaxRevenue, out int proxyIdx);

                if (found.HasValue) {
                    ResourceBlock adjustedValueRequested = found.Value.Requested;
                    MarketRequestInfo? adjustedFound = found;

                    if (found.Value.Requester.InfiniteRequests) {
                        // Set requested value equal to this suppliers stock
                        // TODO: probably a simpler way to do this
                        var array = Enum.GetValues(typeof(ResourceId)).Cast<ResourceId>();
                        int length = array.Count();
                        for (int i = 0; i < length - 2; i++) {
                            if ((supplier.Storage.Current[(ResourceId)i] != 0) && (found.Value.Requested[(ResourceId)i] != 0)) {
                                Debug.Log("[Sitting] Set request to max " + supplier.Storage.Current[(ResourceId)i] + " for resource " + ((ResourceId)i).ToString());
                                adjustedValueRequested[(ResourceId)i] = supplier.Storage.Current[(ResourceId)i];
                            }
                        }

                        if (adjustedFound.Value.Requester.IsLocalOption) {
                            // Remove sales / import taxes (since essentially sold to itself)
                            baseTaxRevenue.Sales = 0;
                            baseTaxRevenue.Import = 0;
                        }
                        // TODO: Determine how storage should work with sales taxes

                        adjustedFound = new MarketRequestInfo(found.Value.Requester, adjustedValueRequested);
                    }

                    Log.Msg("[MarketSystem] Shipping {0} from '{1}' to '{2}'", adjustedValueRequested, supplier.name, adjustedFound.Value.Requester.name);

                    int regionPurchasedIn = ZavalaGame.SimGrid.Terrain.Regions[adjustedFound.Value.Requester.Position.TileIndex];
                    int quantity = adjustedValueRequested.Count; // TODO: may be buggy if we ever have requests that cover multiple resources
                    GeneratedTaxRevenue netTaxRevenue = new GeneratedTaxRevenue(baseTaxRevenue.Sales * quantity, baseTaxRevenue.Import * quantity, baseTaxRevenue.Penalties * quantity);
                    MarketUtility.RecordRevenueToHistory(marketData, netTaxRevenue, regionPurchasedIn);

                    MarketActiveRequestInfo activeRequest;
                    if (supplier.Storage.InfiniteSupply) {
                        activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, adjustedValueRequested, netTaxRevenue, proxyIdx);
                    }
                    else {
                        activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, ResourceBlock.Consume(ref supplier.Storage.Current, adjustedValueRequested), netTaxRevenue, proxyIdx);
                    }
                    ResourceStorageUtility.RefreshStorageDisplays(supplier.Storage);
                    if (baseProfit < 0) { supplier.SoldAtALoss = true; }

                    m_StateA.FulfillQueue.PushBack(activeRequest); // picked up by fulfillment system

                    if (!adjustedFound.Value.Requester.IsLocalOption) {
                        MarketUtility.RecordPurchaseToHistory(marketData, adjustedValueRequested, regionPurchasedIn);
                    }
                    else {
                        ScriptUtility.Trigger(GameTriggers.LetSat);
                    }
                }
            }

            // for all remaining, increment their age
            for (int i = 0; i < m_RequestWorkList.Count; i++) {
                m_RequestWorkList[i].Age++;
                if (m_RequestWorkList[i].Age >= m_RequestWorkList[i].Requester.AgeOfUrgency && m_RequestWorkList[i].Requester.AgeOfUrgency > 0) {
                    m_StateD.NewUrgents.Add(m_RequestWorkList[i]);
                }
            }
            m_RequestWorkList.CopyTo(m_StateA.RequestQueue);
            m_RequesterWorkList.Clear();

            MarketUtility.FinalizeCycleHistory(marketData);
            // Trigger market cycle tick completed for market graphs to update
            ZavalaGame.Events.Dispatch(GameEvents.MarketCycleTickCompleted);
        }

        private unsafe MarketRequestInfo? FindHighestPriorityBuyer(ResourceSupplier supplier, RingBuffer<MarketRequestInfo> requests, out int profit, out GeneratedTaxRevenue taxRevenue, out int proxyIdx) {
            int highestPriorityIndex = int.MaxValue;
            int highestPriorityRequestIndex = -1;
            ResourceBlock current = supplier.Storage.Current;
            proxyIdx = -1;

            for (int i = 0; i < requests.Count; i++) {
                if (!supplier.Storage.InfiniteSupply) {
                    if (!ResourceBlock.Fulfills(current, requests[i].Requested)) {
                        continue;
                    }
                }

                int priorityIndex = supplier.Priorities.PrioritizedBuyers.FindIndex((i, b) => i.Target == b, requests[i].Requester);
                if (priorityIndex < 0) {
                    continue;
                }

                if (priorityIndex == 0) {
                    MarketRequestInfo request = requests[i];
                    profit = supplier.Priorities.PrioritizedBuyers[priorityIndex].Profit;
                    taxRevenue = supplier.Priorities.PrioritizedBuyers[priorityIndex].TaxRevenue;
                    proxyIdx = supplier.Priorities.PrioritizedBuyers[priorityIndex].ProxyIdx;
                    requests.FastRemoveAt(i);
                    return request;
                }

                if (priorityIndex < highestPriorityIndex) {
                    highestPriorityIndex = priorityIndex;
                    highestPriorityRequestIndex = i;
                }
            }

            if (highestPriorityRequestIndex >= 0) {
                MarketRequestInfo request = requests[highestPriorityRequestIndex];
                profit = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].Profit;
                taxRevenue = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].TaxRevenue;
                proxyIdx = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].ProxyIdx;
                requests.FastRemoveAt(highestPriorityRequestIndex);
                return request;
            }

            profit = 0;
            taxRevenue = new GeneratedTaxRevenue(0, 0, 0);
            return null;
        }

        private void MoveReceivedToStorage() {
            foreach (var requester in m_StateA.Buyers) {
                if (requester.Storage && !requester.Received.IsZero) {
                    requester.Storage.Current += requester.Received;
                    ResourceStorageUtility.RefreshStorageDisplays(requester.Storage);
                    requester.Received = default;
                }
            }
        }

        private void UpdateAllSupplierPriorities() {
            m_StateA.Buyers.CopyTo(m_RequesterWorkList);

            RoadNetwork roadState = Game.SharedState.Get<RoadNetwork>();
            HexGridSize gridSize = Game.SharedState.Get<SimGridState>().HexSize;

            foreach (var supplier in m_StateA.Suppliers) {
                UpdateSupplierPriority(supplier, m_StateA, m_StateB, roadState, gridSize);
            }

            m_RequesterWorkList.Clear();
            m_PriorityWorkList.Clear();
        }

        private void UpdateSupplierPriority(ResourceSupplier supplier, MarketData data, MarketConfig config, RoadNetwork network, HexGridSize gridSize) {
            m_PriorityWorkList.Clear();
            supplier.Priorities.PrioritizedBuyers.Clear();

            ResourceMask shippingMask = supplier.ShippingMask;

            foreach (var requester in m_RequesterWorkList) {
                // ignore buyers that don't overlap with shipping mask
                ResourceMask overlap = requester.RequestMask & supplier.ShippingMask;
                if (overlap == 0) {
                    continue;
                }

                RoadPathSummary connectionSummary;
                if (supplier.Position.IsExternal) {
                    connectionSummary = new RoadPathSummary();
                    connectionSummary.TileIndx = requester.Position.TileIndex;
                    connectionSummary.Connected = true;
                    connectionSummary.Distance = 0;
                }
                else {
                    connectionSummary = RoadUtility.IsConnected(network, gridSize, supplier.Position.TileIndex, requester.Position.TileIndex);
                }

                if (!connectionSummary.Connected) {
                    continue;
                }
                if (connectionSummary.Distance != 0 && requester.IsLocalOption) {
                    // Exclude local options from lists of non-local tiles
                    continue;
                }

                bool proxyMatch = false;

                //External case
                if (supplier.Position.IsExternal) {
                    /* Only need to check for overlap if we start having various external suppliers selling different types of products through different depots.
                    ExternalState externalState = Game.SharedState.Get<ExternalState>();

                    ResourceMask proxyOverlap = supplier.ShippingMask & externalState.ExternalDepot.ProxyMask;

                    if (proxyOverlap == 0) {
                        // no match
                        continue;
                    }
                    */
                }
                // Regular case
                else {
                    // Adjust for if is a proxy connection
                    if (connectionSummary.ProxyConnectionIdx != -1) {
                        // If proxy connection, ensure proxy is for the right type of resource
                        uint region = ZavalaGame.SimGrid.Terrain.Regions[connectionSummary.ProxyConnectionIdx];
                        if (network.ExportDepotMap.ContainsKey(region)) {
                            List<ResourceSupplierProxy> proxies = network.ExportDepotMap[region];
                            foreach (var proxy in proxies) {
                                // For each export depot, check if supplier is connected to it.
                                if (connectionSummary.ProxyConnectionIdx == proxy.Position.TileIndex) {
                                    ResourceMask proxyOverlap = supplier.ShippingMask & proxy.ProxyMask;

                                    if (proxyOverlap != 0) {
                                        proxyMatch = true;
                                    }
                                }
                            }
                        }

                        if (!proxyMatch) {
                            // no match
                            continue;
                        }
                    }
                }

                var adjustments = config.UserAdjustmentsPerRegion[requester.Position.RegionIndex];

                ResourceId primary = ResourceUtility.FirstResource(overlap);
                // Only apply import tax if shipping across regions. Then use import tax of purchaser
                // NOTE: the profit and tax revenues calculated below are on a per-unit basis. Needs to be multiplied by quantity when the actually sale takes place.
                int importCost = 0;
                if (supplier.Position.IsExternal || (supplier.Position.RegionIndex != requester.Position.RegionIndex)) {
                    importCost = adjustments.ImportTax[primary];
                }
                float shippingCost = connectionSummary.Distance * config.TransportCosts.CostPerTile[primary] + adjustments.PurchaseTax[primary] + importCost;

                if (connectionSummary.ProxyConnectionIdx != -1) {
                    // add flat rate export depot shipping fee
                    shippingCost += config.TransportCosts.ExportDepotFlatRate[primary];
                }

                float profit = 0;
                if (requester.IsLocalOption) {
                    profit -= adjustments.RunoffPenalty[primary];
                }
                else {
                    if (requester.OverridesBuyPrice) {
                        profit = requester.OverrideBlock[primary];
                    }
                    else {
                        profit = config.PurchasePerRegion[supplier.Position.RegionIndex].Buy[primary];
                    }
                }

                float score = profit - shippingCost;

                GeneratedTaxRevenue taxRevenue = new GeneratedTaxRevenue();
                taxRevenue.Sales = adjustments.PurchaseTax[primary];
                taxRevenue.Import = importCost;
                taxRevenue.Penalties = requester.IsLocalOption ? adjustments.RunoffPenalty[primary] : 0;

                m_PriorityWorkList.PushBack(new MarketSupplierPriorityInfo() {
                    Distance = (int)Math.Ceiling(connectionSummary.Distance),
                    Mask = overlap,
                    Target = requester,
                    ProxyIdx = connectionSummary.ProxyConnectionIdx,
                    Profit = (int)Math.Ceiling(score),
                    TaxRevenue = taxRevenue
                });
            }

            m_PriorityWorkList.Sort((a, b) => {
                return b.Profit - a.Profit;
            });

            m_PriorityWorkList.CopyTo(supplier.Priorities.PrioritizedBuyers);
        }
    }
}