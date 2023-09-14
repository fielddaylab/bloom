using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEditor;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.Economy
{
    [SysUpdate(GameLoopPhase.Update, 5)]
    public sealed class MarketSystem : SharedStateSystemBehaviour<MarketData, MarketConfig, SimGridState>
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
                MarketRequestInfo? found = FindHighestPriorityBuyer(supplier, m_RequestWorkList, out int profit, out GeneratedTaxRevenue taxRevenue);
                
                if (found.HasValue) {
                    Log.Msg("[MarketSystem] Shipping {0} from '{1}' to '{2}'", found.Value.Requested, supplier.name, found.Value.Requester.name);
                    ResourceBlock.Consume(ref supplier.Storage.Current, found.Value.Requested);
                    MarketActiveRequestInfo activeRequest = new MarketActiveRequestInfo(supplier, found.Value);
                    if (profit < 0) { supplier.SoldAtALoss = true; }
                    m_StateA.FulfullQueue.PushBack(activeRequest); // picked up by fulfillment system
                    int regionPurchasedIn = ZavalaGame.SimGrid.Terrain.Regions[found.Value.Requester.Position.TileIndex];
                    MarketUtility.RecordPurchaseToHistory(marketData, found.Value.Requested, regionPurchasedIn);
                    MarketUtility.RecordRevenueToHistory(marketData, taxRevenue, regionPurchasedIn);
                }
            }

            // TODO: keep track of gathered sales taxes, import taxes, and penalties

            // for all remaining, increment their age
            for (int i = 0; i < m_RequestWorkList.Count; i++) {
                m_RequestWorkList[i].Age++;
            }
            m_RequestWorkList.CopyTo(m_StateA.RequestQueue);
            m_RequesterWorkList.Clear();

            MarketUtility.FinalizeCycleHistory(marketData);
            // Trigger market cycle tick completed for market graphs to update
            ZavalaGame.Events.Dispatch(GameEvents.MarketCycleTickCompleted);
        }

        private unsafe MarketRequestInfo? FindHighestPriorityBuyer(ResourceSupplier supplier, RingBuffer<MarketRequestInfo> requests, out int profit, out GeneratedTaxRevenue taxRevenue) {
            int highestPriorityIndex = int.MaxValue;
            int highestPriorityRequestIndex = -1;
            ResourceBlock current = supplier.Storage.Current;

            for (int i = 0; i < requests.Count; i++) {
                if (!ResourceBlock.Fulfills(current, requests[i].Requested)) {
                    continue;
                }

                int priorityIndex = supplier.Priorities.PrioritizedBuyers.FindIndex((i, b) => i.Target == b, requests[i].Requester);
                if (priorityIndex < 0) {
                    continue;
                }


                if (priorityIndex == 0) {
                    MarketRequestInfo request = requests[i];
                    profit = supplier.Priorities.PrioritizedBuyers[priorityIndex].Profit;
                    taxRevenue = supplier.Priorities.PrioritizedBuyers[priorityIndex].TaxRevenue;
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

                RoadPathSummary connectionSummary = RoadUtility.IsConnected(network, gridSize, supplier.Position.TileIndex, requester.Position.TileIndex);
                if (!connectionSummary.Connected) {
                    continue;
                }

                var adjustments = config.UserAdjustmentsPerRegion[supplier.Position.RegionIndex];

                ResourceId primary = ResourceUtility.FirstResource(overlap);
                // TODO: only apply import tax if shipping across regions. Then use import tax of purchaser
                // TODO: do purchase / import taxes need to be multiplied by the quantity of shipped goods?
                float shippingCost = connectionSummary.Distance * config.TransportCosts.CostPerTile[primary] + adjustments.PurchaseTax[primary] + adjustments.ImportTax[primary];
                float profit = config.PurchasePerRegion[supplier.Position.RegionIndex].Buy[primary];
                float score = profit - shippingCost;
                // TODO: implement Let it Sit option. Then if purchaser is letting it sit, find penalties
                GeneratedTaxRevenue taxRevenue = new GeneratedTaxRevenue(adjustments.PurchaseTax[primary], adjustments.ImportTax[primary], 0);

                m_PriorityWorkList.PushBack(new MarketSupplierPriorityInfo() {
                    Distance = (int)Math.Ceiling(connectionSummary.Distance),
                    Mask = overlap,
                    Target = requester,
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