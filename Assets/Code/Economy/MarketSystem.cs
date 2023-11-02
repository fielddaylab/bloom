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
            TutorialState tutorial = Game.SharedState.Get<TutorialState>();

            // TODO: get Zavala's team's sophisticated algorithm for matchmaking (cost optimization and bill reduction, I think)
            // Right now, it is NOT guaranteed that if a player makes local manure market competitive, the grain farms will buy it. It's random.
            // It only guarantees that if selling to the grain farm is cheaper for the dairy farm than letting it sit, then IF the dairy farm wins the lottery it will sell to the grain farm.
            // So the player can improve the amount of runoff on average, just without complete control over every transaction.

            // For now: Set external suppliers to a lower priority. This way external suppliers will only be queried if there is no local option offering to sell.
            m_SupplierWorkList.Sort((a, b) => {
                return a.SupplierPriority - b.SupplierPriority;
            });

            foreach (var supplier in m_SupplierWorkList) {
                // reset sold at a loss
                supplier.SoldAtALoss = false;
                MarketRequestInfo? found = FindHighestPriorityBuyer(supplier, m_RequestWorkList, out int baseProfit, out int relativeGain, out GeneratedTaxRevenue baseTaxRevenue, out ushort proxyIdx, out RoadPathSummary summary);

                if (found.HasValue) {
                    ResourceBlock adjustedValueRequested = found.Value.Requested;
                    MarketRequestInfo? adjustedFound = found;

                    if (found.Value.Requester.InfiniteRequests) {
                        // Set requested value equal to this suppliers stock
                        // TODO: probably a simpler way to do this
                        var array = Enum.GetValues(typeof(ResourceId)).Cast<ResourceId>();
                        int length = array.Count();
                        for (int i = 0; i < length - 2; i++) {
                            ResourceId resource = (ResourceId)i;
                            if ((supplier.Storage.Current[resource] != 0) && (found.Value.Requested[resource] != 0)) {
                                int extensionCount = 0;
                                if (supplier.Storage.StorageExtensionStore != null && !adjustedFound.Value.Requester.IsLocalOption) {
                                    extensionCount += supplier.Storage.StorageExtensionStore.Current[resource];
                                }

                                // Debug.Log("[Sitting] Set request to max " + (supplier.Storage.Current[resource] + extensionCount) + " for resource " + resource.ToString());
                                adjustedValueRequested[resource] = supplier.Storage.Current[resource] + extensionCount;
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

                    if (!ResourceBlock.Fulfills(supplier.Storage.Current, adjustedValueRequested)) {
                        if (supplier.Storage.StorageExtensionReq == adjustedFound.Value.Requester) {
                            // local option was optimal buyer, so didn't sell to alternatives. But no change to be made to the storage or extended storage.
                            // requeue request
                            m_RequestWorkList.PushBack(found.Value);
                            continue;
                        }
                    }

                    Log.Msg("[MarketSystem] Shipping {0} from '{1}' to '{2}'", adjustedValueRequested, supplier.name, adjustedFound.Value.Requester.name);

                    int regionPurchasedIn = ZavalaGame.SimGrid.Terrain.Regions[adjustedFound.Value.Requester.Position.TileIndex];
                    int quantity = adjustedValueRequested.Count; // TODO: may be buggy if we ever have requests that cover multiple resources
                    GeneratedTaxRevenue netTaxRevenue = new GeneratedTaxRevenue(baseTaxRevenue.Sales * quantity, baseTaxRevenue.Import * quantity, baseTaxRevenue.Penalties * quantity);
                    MarketUtility.RecordRevenueToHistory(marketData, netTaxRevenue, regionPurchasedIn);

                    MarketActiveRequestInfo activeRequest;
                    if (supplier.Storage.InfiniteSupply) {
                        activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, adjustedValueRequested, netTaxRevenue, proxyIdx, summary);
                    }
                    else {
                        ResourceBlock mainStorageBlock;
                        ResourceBlock extensionBlock = new ResourceBlock();

                        if (!ResourceBlock.Fulfills(supplier.Storage.Current, adjustedValueRequested)) {
                            // subtract main storage from whole value
                            mainStorageBlock = ResourceBlock.Consume(ref adjustedValueRequested, supplier.Storage.Current);

                            // subtract remaining value from extension storage
                            extensionBlock = ResourceBlock.Consume(ref supplier.Storage.StorageExtensionStore.Current, adjustedValueRequested);
                        }
                        else {
                            mainStorageBlock = ResourceBlock.Consume(ref supplier.Storage.Current, adjustedValueRequested);
                        }

                        activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, mainStorageBlock + extensionBlock, netTaxRevenue, proxyIdx, summary);
                    }
                    ResourceStorageUtility.RefreshStorageDisplays(supplier.Storage);
                    if (baseProfit - relativeGain < 0) {
                        supplier.SoldAtALoss = true; 
                    }

                    m_StateA.FulfillQueue.PushBack(activeRequest); // picked up by fulfillment system

                    if (!adjustedFound.Value.Requester.IsLocalOption) {
                        MarketUtility.RecordPurchaseToHistory(marketData, adjustedValueRequested, regionPurchasedIn);
                    }
                    else {
                        ScriptUtility.Trigger(GameTriggers.LetSat);
                    }
                }
            }

            if (tutorial.CurrState >= TutorialState.State.ActiveSim) {
                // for all remaining, increment their age
                for (int i = 0; i < m_RequestWorkList.Count; i++) {
                    m_RequestWorkList[i].Age++;
                    if (m_RequestWorkList[i].Age >= m_RequestWorkList[i].Requester.AgeOfUrgency && m_RequestWorkList[i].Requester.AgeOfUrgency > 0) {
                        m_StateD.NewUrgents.Add(m_RequestWorkList[i]);
                    }
                }
            }

            m_RequestWorkList.CopyTo(m_StateA.RequestQueue);
            m_RequesterWorkList.Clear();

            MarketUtility.FinalizeCycleHistory(marketData);
            // Trigger market cycle tick completed for market graphs to update
            ZavalaGame.Events.Dispatch(GameEvents.MarketCycleTickCompleted);
        }

        private unsafe MarketRequestInfo? FindHighestPriorityBuyer(ResourceSupplier supplier, RingBuffer<MarketRequestInfo> requests, out int profit, out int relativeGain, out GeneratedTaxRevenue taxRevenue, out ushort proxyIdx, out RoadPathSummary path) {
            int highestPriorityIndex = int.MaxValue;
            int highestPriorityRequestIndex = -1;
            ResourceBlock current;

            proxyIdx = Tile.InvalidIndex16;

            for (int i = 0; i < requests.Count; i++) {
                if (!supplier.Storage.InfiniteSupply) {

                    if (supplier.Storage.StorageExtensionStore == null) {
                        current = supplier.Storage.Current;
                    }
                    else if (requests[i].Requester.Position.TileIndex == supplier.Storage.StorageExtensionReq.Position.TileIndex) {
                        current = supplier.Storage.Current;
                    }
                    else {
                        current = supplier.Storage.Current + supplier.Storage.StorageExtensionStore.Current;
                    }

                    if (!ResourceBlock.Fulfills(current, requests[i].Requested) && !requests[i].Requester.InfiniteRequests) {
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
                    relativeGain = supplier.Priorities.PrioritizedBuyers[priorityIndex].RelativeGain;
                    taxRevenue = supplier.Priorities.PrioritizedBuyers[priorityIndex].TaxRevenue;
                    proxyIdx = supplier.Priorities.PrioritizedBuyers[priorityIndex].ProxyIdx;
                    path = supplier.Priorities.PrioritizedBuyers[priorityIndex].Path;
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
                relativeGain = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].RelativeGain;
                taxRevenue = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].TaxRevenue;
                proxyIdx = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].ProxyIdx;
                path = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].Path;
                requests.FastRemoveAt(highestPriorityRequestIndex);
                return request;
            }

            profit = 0;
            relativeGain = 0;
            taxRevenue = new GeneratedTaxRevenue(0, 0, 0);
            path = default;
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
            TutorialState tutorialState = Game.SharedState.Get<TutorialState>();

            foreach (var supplier in m_StateA.Suppliers) {
                UpdateSupplierPriority(supplier, m_StateA, m_StateB, roadState, gridSize, tutorialState);
            }

            m_RequesterWorkList.Clear();
            m_PriorityWorkList.Clear();
        }

        private void UpdateSupplierPriority(ResourceSupplier supplier, MarketData data, MarketConfig config, RoadNetwork network, HexGridSize gridSize, TutorialState tutorialState) {
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
                    connectionSummary.DestinationIdx = (ushort) requester.Position.TileIndex;
                    connectionSummary.Connected = true;
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
                if (supplier.Position.TileIndex == requester.Position.TileIndex && !requester.IsLocalOption) {
                    // Exclude a tile from supplying itself -- storage, I'm looking at you :(
                    continue;
                }

                bool proxyMatch = false;

                //External case
                if (supplier.Position.IsExternal) {
                    // Don't summon blimps until basic tutorial completes
                    if (tutorialState.CurrState <= TutorialState.State.InactiveSim) {
                        continue;
                    }
                }
                // Regular case
                else {
                    // Adjust for if is a proxy connection
                    if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16) {
                        // If proxy connection, ensure proxy is for the right type of resource
                        uint region = ZavalaGame.SimGrid.Terrain.Regions[connectionSummary.ProxyConnectionIdx];
                        if (network.ExportDepotMap.ContainsKey(region)) {
                            List<ResourceSupplierProxy> proxies = network.ExportDepotMap[region];
                            foreach (var proxy in proxies) {
                                // For each export depot, check if supplier is connected to it.
                                if (connectionSummary.ProxyConnectionIdx == proxy.Position.TileIndex) {
                                    ResourceMask proxyOverlap = overlap & proxy.ProxyMask;

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

                if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16) {
                    // add flat rate export depot shipping fee
                    shippingCost += config.TransportCosts.ExportDepotFlatRate[primary];
                }

                float profit = 0;
                float relativeGain = 0; // How much this supplier stands to gain by NOT incurring penalties from not selling
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

                    if (supplier.Storage.StorageExtensionReq != null) {
                        // since this has a local option that would penalize runoff, that means the farm would sell it to this alternative for that much less (so the score for the purchaser should be higher)
                        relativeGain += adjustments.RunoffPenalty[primary];
                    }
                }

                float score = profit - shippingCost;

                GeneratedTaxRevenue taxRevenue = new GeneratedTaxRevenue();
                taxRevenue.Sales = adjustments.PurchaseTax[primary];
                taxRevenue.Import = importCost;
                taxRevenue.Penalties = requester.IsLocalOption ? adjustments.RunoffPenalty[primary] : 0;

                m_PriorityWorkList.PushBack(new MarketSupplierPriorityInfo() {
                    Distance = connectionSummary.Distance,
                    Mask = overlap,
                    Target = requester,
                    ProxyIdx = connectionSummary.ProxyConnectionIdx,
                    Path = connectionSummary,
                    Profit = (int)Math.Ceiling(score),
                    RelativeGain = (int)Math.Ceiling(relativeGain),
                    TaxRevenue = taxRevenue
                });;
            }

            m_PriorityWorkList.Sort((a, b) => {
                return b.Profit - a.Profit;
            });

            m_PriorityWorkList.CopyTo(supplier.Priorities.PrioritizedBuyers);
        }
    }
}