using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using Zavala.Advisor;
using Zavala.Roads;
using Zavala.Sim;

namespace Zavala.Economy
{
    [SysUpdate(GameLoopPhase.Update, 5, ZavalaGame.SimulationUpdateMask)]
    public sealed class MarketSystem : SharedStateSystemBehaviour<MarketData, MarketConfig, SimGridState, RequestVisualState>
    {
        // NOT persistent state - work lists for various updates
        private readonly RingBuffer<MarketRequestInfo> m_RequestWorkList = new RingBuffer<MarketRequestInfo>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ResourceSupplier> m_SupplierWorkList = new RingBuffer<ResourceSupplier>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ResourceRequester> m_RequesterWorkList = new RingBuffer<ResourceRequester>(8, RingBufferMode.Expand);
        private readonly RingBuffer<MarketRequesterPriorityInfo> m_SellerPriorityWorkList = new RingBuffer<MarketRequesterPriorityInfo>(8, RingBufferMode.Expand);
        private readonly RingBuffer<MarketSupplierPriorityInfo> m_BuyerPriorityWorkList = new RingBuffer<MarketSupplierPriorityInfo>(8, RingBufferMode.Expand);
        private Dictionary<ResourceRequester, RingBuffer<MarketSupplierOffer>> m_SupplierOfferMap = new Dictionary<ResourceRequester, RingBuffer<MarketSupplierOffer>>();
        private readonly RingBuffer<MarketSupplierOffer> m_SupplierOfferWorkList = new RingBuffer<MarketSupplierOffer>(4, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime)
        {
            SimTimeState time = ZavalaGame.SimTime;

            MoveReceivedToStorage();

            bool marketCycle = m_StateA.MarketTimer.Advance(deltaTime, time);

            if (m_StateA.UpdatePrioritiesNow || marketCycle)
            {
                UpdateAllSupplierPriorities();
                UpdateAllRequesterPriorities();
                m_StateA.UpdatePrioritiesNow = false;
                ZavalaGame.Events.Dispatch(GameEvents.MarketPrioritiesRebuilt);
            }

            if (marketCycle)
            {
                ProcessMarketCycle();
            }
        }

        private void ProcessMarketCycle()
        {

            #region MarketCycle_InitialSetup
            // INITIAL SETUP

            // Prior: PRIORITIZE BUYERS
            // Prior: PRIORITIZE SELLERS

            m_StateA.RequestQueue.CopyTo(m_RequestWorkList);
            m_StateA.RequestQueue.Clear();
            m_StateA.NegotiationQueue.Clear();
            m_SupplierWorkList.Clear();
            m_RequesterWorkList.Clear();
            m_StateA.Suppliers.CopyTo(m_SupplierWorkList);
            m_StateA.Buyers.CopyTo(m_RequesterWorkList);
            m_SupplierOfferMap.Clear();
            m_SupplierOfferWorkList.Clear();

            SimGridState grid = ZavalaGame.SimGrid;
            MarketData marketData = m_StateA;
            TutorialState tutorial = Game.SharedState.Get<TutorialState>();
            BudgetData budget = Game.SharedState.Get<BudgetData>();
            MarketConfig config = Game.SharedState.Get<MarketConfig>();

            foreach (var supplier in m_SupplierWorkList)
            {
                // Take a snapshot before sells. Compare with post snapshot to see if a specific resource was sold
                supplier.PreSaleSnapshot = MarketUtility.GetCompleteStorage(supplier);

                // reset matched flag
                supplier.MatchedThisTick = false;

                // reset sold at a loss
                supplier.SoldAtALossExcludingMilk = false;

                // sold milk this tick
                supplier.MatchedThisTickWasMilk = false;

                for (int marketIndex = 0; marketIndex < MarketUtility.NumMarkets; marketIndex++)
                {
                    supplier.BestPriorityIndex[marketIndex] = 0;
                }
            }

            foreach (var requester in m_RequesterWorkList)
            {
                // reset matched flag
                requester.MatchedThisTick = false;

                // reset stress purchase
                requester.PurchasedAtStressedPrice = false;

                // subsidy applied this tick, relieves stress
                requester.SubsidyAppliedThisTick = false;

                for (int marketIndex = 0; marketIndex < MarketUtility.NumMarkets; marketIndex++)
                {
                    requester.BestPriorityIndex[marketIndex] = 0;
                }
            }

            m_RequesterWorkList.Clear();

            // Prior: CREATE A Buyer->SellerOptions MAPPING (m_SupplierOfferMap)
            // Prior: CREATE A RingBuffer<Buyers> TO PROCESS BUYERS IN PRIORITY ORDER (m_RequesterWorkList)

            #endregion // MarketCycle_InitialSetup

            for (int marketIndex = 0; marketIndex < MarketUtility.NumMarkets; marketIndex++)
            {
                ResourceMask marketMask = MarketUtility.MarketIndexToResourceMask(marketIndex);

                #region MarketCycle_SupplierFirstChoice

                // FIND EACH SUPPLIER'S FIRST CHOICE OF BUYER FOR THIS MARKET
                foreach (var supplier in m_SupplierWorkList)
                {
                      ReassignSellerHighestPriorityBuyer(supplier, marketMask);
                }

                #endregion // MarketCycle_SupplierFirstChoice

                #region MarketCycle_BuyerSupplierIteration

                // ITERATE THROUGH THE BUYERS, ONE PRIORITY LEVEL AT A TIME.
                // FIND THE MOST OPTIMAL CHOICE STILL AVAILABLE FOR BOTH BUYERS AND SELLERS.
                while (m_RequesterWorkList.Count > 0)
                {
                    var requester = m_RequesterWorkList.PopFront();

                    // LOAD SUPPLIER OFFERS FOR PROCESSING
                    m_SupplierOfferWorkList.Clear();
                    m_SupplierOfferMap[requester].CopyTo(m_SupplierOfferWorkList);

                    // SORT SELLER LISTS BY FAMILIARITY
                    m_SupplierOfferWorkList.Sort((a, b) =>
                    {
                        return b.FamiliarityScore - a.FamiliarityScore;
                    });

                    // IF ANY SELLER MATCHES THE BEST PRIORITY INDEX...
                    bool matchFound = false;

                    // bool offerInMarket = false;
                    foreach (var supplierOffer in m_SupplierOfferWorkList)
                    {
                        if (supplierOffer.Supplier == requester.Priorities.PrioritizedSuppliers[requester.BestPriorityIndex[marketIndex]].Target)
                        {
                            matchFound = true;

                            // ...FINALIZE SALE AND FIND NEW HIGHEST PRIORITY BUYERS FOR OTHER SELLERS
                            FinalizeSale(marketData, tutorial, config, supplierOffer.Supplier, supplierOffer.FoundRequest, supplierOffer, marketIndex);
                            m_RequestWorkList.Remove(supplierOffer.FoundRequest);
                            m_SupplierOfferWorkList.Remove(supplierOffer);

                            break;
                        }
                    }
                    if (matchFound)
                    {
                        foreach (var supplierOffer in m_SupplierOfferWorkList)
                        {
                            // REASSIGN REMAINING SUPPLIERS TO THEIR NEXT HIGHEST PRIORITY INDEX
                            ReassignSellerHighestPriorityBuyer(supplierOffer.Supplier, marketMask);
                        }
                    }
                    // ELSE MOVE TO BUYER'S NEXT BEST PRIORITY AND CONTINUE
                    else
                    {
                        requester.BestPriorityIndex[marketIndex]++;
                        m_RequesterWorkList.PushBack(requester);
                    }
                    m_SupplierOfferWorkList.CopyTo(m_SupplierOfferMap[requester]);
                }

                #endregion // MarketCycle_BuyerSupplierIteration
            }

            #region MarketCycle_Negotiations

            // NEGOTIATION PHASE
            // ProcessNegotiations(tutorial);

            #endregion // MarketCycle_Negotations

            #region MarketCycle_RecurringPolicyCosts

            // ProcessSkimmerCosts(grid, marketData, budget);

            #endregion // MarketCycle_RecurringPolicyCosts

            #region MarketCycle_History

            m_StateA.TickIndex++;
            if ((m_StateA.TickIndex % 3) == 0)
            {
                MarketUtility.FinalizeCycleHistory(marketData);
            }

            #endregion // MarketCycle_History

            #region MarketCycle_Cleanup

            m_RequestWorkList.CopyTo(m_StateA.RequestQueue);
            m_RequestWorkList.Clear();
            m_RequesterWorkList.Clear();

            #endregion // MarketCycle_Cleanup

            #region PostMarketCycle_Dispatches

            // Trigger market cycle tick completed for market graphs to update
            ZavalaGame.Events.Dispatch(GameEvents.MarketCycleTickCompleted);

            #endregion // PostMarketCycle_Dispatches
        }

        #region IRegistrationCallbacks

        public void OnEnable()
        {
            Game.Events?.Register(GameEvents.ForceMarketPrioritiesRebuild, HandleForcePrioritiesRebuild);
        }

        public void OnDisable()
        {
            Game.Events?.Deregister(GameEvents.ForceMarketPrioritiesRebuild, HandleForcePrioritiesRebuild);
        }

        #endregion // IRegistrationCallbacks

        #region Handlers

        private void HandleForcePrioritiesRebuild()
        {
            if (m_StateA == null) { return; }
            UpdateAllSupplierPriorities();
            UpdateAllRequesterPriorities();
            ZavalaGame.Events.Dispatch(GameEvents.MarketPrioritiesRebuilt);
        }

        #endregion // Handlers

        #region Market Processing

        private unsafe MarketRequestInfo? FindHighestPriorityBuyer(ResourceSupplier supplier, RingBuffer<MarketRequestInfo> requests, ResourceMask resourceMask, out int profit, out int relativeGain, out GeneratedTaxRevenue taxRevenue, out ushort proxyIdx, out RoadPathSummary path, out int costToBuyer)
        {
            int highestPriorityIndex = int.MaxValue;
            int highestPriorityRequestIndex = -1;
            ResourceBlock current;

            proxyIdx = Tile.InvalidIndex16;

            int marketIndex = MarketUtility.ResourceMaskToMarketIndex(resourceMask);
            for (int i = 0; i < requests.Count; i++)
            {
                // only consider requests of the specified type
                if ((requests[i].Requested & resourceMask).IsZero)
                {
                    continue;
                }
                // Skip this requester if they couldn't hold what they are requesting.
                if (requests[i].Requester.InfiniteRequests && !MarketUtility.CanHoldRequest(requests[i].Requester))
                {
                    continue;
                }
                if (!supplier.Storage.InfiniteSupply)
                {
                    if (supplier.Storage.StorageExtensionStore == null)
                    {
                        current = supplier.Storage.Current;
                    }
                    else if (requests[i].Requester.Position.TileIndex == supplier.Storage.StorageExtensionReq.Position.TileIndex)
                    {
                        current = supplier.Storage.Current;
                    }
                    else
                    {
                        current = supplier.Storage.Current + supplier.Storage.StorageExtensionStore.Current;
                    }
                    current = current & supplier.ShippingMask;

                    // current resources must fulfill the request unless the request is infinite AND a local option
                    if (!ResourceBlock.Fulfills(current, requests[i].Requested))
                    {
                        if (requests[i].Requester.InfiniteRequests && requests[i].Requester.IsLocalOption)
                        {
                            // no need to fulfill the request to consider it
                        }
                        else
                        {
                            // not a valid buyer, since this supplier cannot fulfill the request
                            continue;
                        }
                    }
                }

                int priorityIndex = supplier.Priorities.PrioritizedBuyers.FindIndex((i, b) => i.Target == b, requests[i].Requester);
                if (priorityIndex < supplier.BestPriorityIndex[marketIndex])
                {
                    continue;
                }
                /*
                if (supplier.Priorities.PrioritizedBuyers[priorityIndex].Deprioritized)
                {
                    continue;
                }
                */

                if (priorityIndex == supplier.BestPriorityIndex[marketIndex])
                {
                    MarketRequestInfo request = requests[i];
                    profit = supplier.Priorities.PrioritizedBuyers[priorityIndex].Profit;
                    relativeGain = supplier.Priorities.PrioritizedBuyers[priorityIndex].RelativeGain;
                    taxRevenue = supplier.Priorities.PrioritizedBuyers[priorityIndex].TaxRevenue;
                    proxyIdx = supplier.Priorities.PrioritizedBuyers[priorityIndex].ProxyIdx;
                    path = supplier.Priorities.PrioritizedBuyers[priorityIndex].Path;
                    costToBuyer = supplier.Priorities.PrioritizedBuyers[priorityIndex].CostToBuyer;
                    // requests.FastRemoveAt(i);
                    supplier.BestPriorityIndex[marketIndex] = priorityIndex + 1;
                    return request;
                }

                if (priorityIndex < highestPriorityIndex)
                {
                    highestPriorityIndex = priorityIndex;
                    highestPriorityRequestIndex = i;
                }
            }

            if (highestPriorityRequestIndex >= supplier.BestPriorityIndex[marketIndex])
            {
                MarketRequestInfo request = requests[highestPriorityRequestIndex];
                profit = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].Profit;
                relativeGain = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].RelativeGain;
                taxRevenue = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].TaxRevenue;
                proxyIdx = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].ProxyIdx;
                path = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].Path;
                costToBuyer = supplier.Priorities.PrioritizedBuyers[highestPriorityIndex].CostToBuyer;
                // requests.FastRemoveAt(highestPriorityRequestIndex);
                supplier.BestPriorityIndex[marketIndex] = highestPriorityRequestIndex + 1;
                return request;
            }

            profit = 0;
            relativeGain = 0;
            taxRevenue = new GeneratedTaxRevenue(0, 0, 0);
            path = default;
            costToBuyer = 0;
            return null;
        }

        private void UpdateAllSupplierPriorities()
        {
            m_StateA.Buyers.CopyTo(m_RequesterWorkList);

            RoadNetwork roadState = Game.SharedState.Get<RoadNetwork>();
            HexGridSize gridSize = Game.SharedState.Get<SimGridState>().HexSize;
            TutorialState tutorialState = Game.SharedState.Get<TutorialState>();

            foreach (var requester in m_RequesterWorkList)
            {
                requester.PriceNegotiator.OfferedRecord.SetAll((int)NegotiableCode.NO_OFFER);
            }

            foreach (var supplier in m_StateA.Suppliers)
            {
                supplier.PriceNegotiator.OfferedRecord.SetAll((int)NegotiableCode.NO_OFFER);
                UpdateSupplierPriority(supplier, m_StateA, m_StateB, roadState, gridSize, tutorialState);
            }

            m_RequesterWorkList.Clear();
            m_BuyerPriorityWorkList.Clear();

            Log.Msg("[MarketSystem] Updated supplier priorities");
        }

        private void UpdateSupplierPriority(ResourceSupplier supplier, MarketData data, MarketConfig config, RoadNetwork network, HexGridSize gridSize, TutorialState tutorialState)
        {
            m_BuyerPriorityWorkList.Clear();
            supplier.Priorities.PrioritizedBuyers.Clear();

            ResourceMask shippingMask = supplier.ShippingMask;

            foreach (var requester in m_RequesterWorkList)
            {
                // ignore buyers that don't overlap with shipping mask
                ResourceMask overlap = requester.RequestMask & supplier.ShippingMask;
                if (overlap == 0)
                {
                    continue;
                }
                if (requester.RefusesSameBuildingType && requester.Position.Type == supplier.Position.Type)
                {
                    continue;
                }

                RoadPathSummary connectionSummary;
                if (supplier.Position.IsExternal)
                {
                    connectionSummary = new RoadPathSummary();
                    connectionSummary.DestinationIdx = (ushort)requester.Position.TileIndex;
                    connectionSummary.Connected = true;
                }
                else
                {
                    connectionSummary = RoadUtility.IsConnected(network, gridSize, supplier.Position.TileIndex, requester.Position.TileIndex);
                }

                if (!connectionSummary.Connected)
                {
                    continue;
                }
                if (connectionSummary.Distance != 0 && requester.IsLocalOption)
                {
                    // Exclude local options from lists of non-local tiles
                    continue;
                }
                if (supplier.Position.TileIndex == requester.Position.TileIndex && !requester.IsLocalOption)
                {
                    // Exclude a tile from supplying itself -- storage, I'm looking at you :(
                    continue;
                }

                bool proxyMatch = false;

                //External case
                if (supplier.Position.IsExternal)
                {
                    // Don't summon blimps until basic tutorial completes
                    if (tutorialState.CurrState <= TutorialState.State.InactiveSim)
                    {
                        continue;
                    }
                }
                // Regular case
                else
                {
                    // Adjust for if is a proxy connection
                    if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16)
                    {
                        // If proxy connection, ensure proxy is for the right type of resource
                        uint region = ZavalaGame.SimGrid.Terrain.Regions[connectionSummary.ProxyConnectionIdx];
                        if (network.ExportDepotMap.ContainsKey(region))
                        {
                            List<ResourceSupplierProxy> proxies = network.ExportDepotMap[region];
                            foreach (var proxy in proxies)
                            {
                                // For each export depot, check if supplier is connected to it.
                                if (connectionSummary.ProxyConnectionIdx == proxy.Position.TileIndex)
                                {
                                    ResourceMask proxyOverlap = overlap & proxy.ProxyMask;

                                    if (proxyOverlap != 0)
                                    {
                                        proxyMatch = true;
                                    }
                                }
                            }
                        }

                        if (!proxyMatch)
                        {
                            // no match
                            continue;
                        }
                    }
                }

                var adjustments = config.UserAdjustmentsPerRegion[requester.Position.RegionIndex];

                ResourceId primary = ResourceUtility.FirstResource(overlap);
                List<ResourceId> allResources = ResourceUtility.AllResources(overlap);

                // Only apply import tax if shipping across regions. Then use import tax of purchaser
                // NOTE: the profit and tax revenues calculated below are on a per-unit basis. Needs to be multiplied by quantity when the actually sale takes place.
                int importCost = 0;
                if (supplier.Position.IsExternal || (supplier.Position.RegionIndex != requester.Position.RegionIndex))
                {
                    importCost = adjustments.ImportTax[primary];
                }
                float salesTax = adjustments.PurchaseTax[primary];
                float shippingCost = connectionSummary.Distance * config.TransportCosts.CostPerTile[primary];
                // Log.Msg("[MarketSystem] Shipping cost: {0} to {1}, {2} * {3} = {4}", supplier.name, requester.name, connectionSummary.Distance, config.TransportCosts.CostPerTile[primary], shippingCost);
                if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16)
                {
                    // add flat rate export depot shipping fee
                    shippingCost += config.TransportCosts.ExportDepotFlatRate[primary];
                }

                float basePrice = 0;
                float relativeGain = 0; // How much this supplier stands to gain by NOT incurring penalties from not selling
                if (requester.IsLocalOption)
                {
                    basePrice -= adjustments.RunoffPenalty[primary];
                }
                else if (requester.OverridesBuyPrice)
                {
                    basePrice = requester.OverrideBlock[MarketUtility.ResourceIdToMarketIndex(primary)];
                }
                else
                {
                    // Err towards buyer purchase cost (TODO: unless they accept anything?)
                    // profit = requester.PriceNegotiator.PriceBlock[primary];

                    basePrice = supplier.PriceNegotiator.SellPriceBlock[MarketUtility.ResourceIdToMarketIndex(primary)];
                }

                //if (supplier.Storage.StorageExtensionReq != null) {
                //    // since this has a local option that would penalize runoff, that means the farm would sell it to this alternative for that much less (so the score for the purchaser should be higher)
                //    // relativeGain += adjustments.RunoffPenalty[primary];
                //}

                float costToBuyer = basePrice + (shippingCost + salesTax + importCost);
                // float score = profit + relativeGain - (shippingCost + salesTax + importCost); // supplier wants to maximize score
                // TODO: why is "importCost"
                float score = basePrice - shippingCost; // the seller/supplier wants to maximize score

                GeneratedTaxRevenue taxRevenue = new GeneratedTaxRevenue();
                taxRevenue.Sales = requester.IsLocalOption ? 0 : adjustments.PurchaseTax[primary];
                taxRevenue.Import = importCost;
                taxRevenue.Penalties = requester.IsLocalOption ? adjustments.RunoffPenalty[primary] : 0;


                // NEGOTIATION PASS
                bool deprioritized = false; // appears in medium level feedback, but not considered as valid buyer to sell to
                // TODO: price block should reflect market prices, not individual resource prices
                if (!requester.PriceNegotiator.AcceptsAnyPrice && (requester.PriceNegotiator.BuyPriceBlock[MarketUtility.ResourceIdToMarketIndex(primary)] < costToBuyer))
                {
                    foreach (var currResource in allResources)
                    {
                        // Mark that the sellers/buyers would have had a match here if there prices were more reasonable
                        requester.PriceNegotiator.OfferedRecord[MarketUtility.ResourceIdToMarketIndex(currResource)] = supplier.PriceNegotiator.FixedSellOffer ? (int)NegotiableCode.NON_NEGOTIABLE : (int)NegotiableCode.NEGOTIABLE;

                        // Check if requester is actively requesting
                        if (requester.Requested[currResource] >= 0)
                        {
                            supplier.PriceNegotiator.OfferedRecord[MarketUtility.ResourceIdToMarketIndex(currResource)] = requester.PriceNegotiator.FixedBuyOffer ? (int)NegotiableCode.NON_NEGOTIABLE : (int)NegotiableCode.NEGOTIABLE;
                        }
                    }


                    // If price points don't overlap, not a valid buyer/seller pair.
                    deprioritized = true;
                }

                m_BuyerPriorityWorkList.PushBack(new MarketSupplierPriorityInfo()
                {
                    Distance = connectionSummary.Distance,
                    ShippingCost = (int)Math.Ceiling(shippingCost),
                    Mask = overlap,
                    Target = requester,
                    ProxyIdx = connectionSummary.ProxyConnectionIdx,
                    Path = connectionSummary,
                    Profit = (int)Math.Ceiling(score),
                    CostToBuyer = (int)costToBuyer,
                    RelativeGain = (int)Math.Ceiling(relativeGain),
                    TaxRevenue = taxRevenue,
                    Deprioritized = deprioritized
                });
            }

            m_BuyerPriorityWorkList.Sort((a, b) =>
            {
                int dif = b.Profit - a.Profit;
                if (dif == 0)
                {
                    // tie break
                    // favor closest option
                    return b.Distance - a.Distance;
                }
                else
                {
                    return dif;
                }
            });

            m_BuyerPriorityWorkList.CopyTo(supplier.Priorities.PrioritizedBuyers);
        }

        private void UpdateAllRequesterPriorities()
        {
            m_StateA.Suppliers.CopyTo(m_SupplierWorkList);

            RoadNetwork roadState = Game.SharedState.Get<RoadNetwork>();
            HexGridSize gridSize = Game.SharedState.Get<SimGridState>().HexSize;
            TutorialState tutorialState = Game.SharedState.Get<TutorialState>();

            foreach (var requester in m_StateA.Buyers)
            {
                UpdateRequesterPriority(requester, m_StateA, m_StateB, roadState, gridSize, tutorialState);
            }

            m_RequesterWorkList.Clear();
            m_SellerPriorityWorkList.Clear();

            Log.Msg("[MarketSystem] Updated buyer priorities");
        }


        private void UpdateRequesterPriority(ResourceRequester requester, MarketData data, MarketConfig config, RoadNetwork network, HexGridSize gridSize, TutorialState tutorialState)
        {
            m_SellerPriorityWorkList.Clear();
            requester.Priorities.PrioritizedSuppliers.Clear();

            ResourceMask requestMask = requester.RequestMask;

            foreach (var supplier in m_SupplierWorkList)
            {
                // ignore buyers that don't overlap with shipping mask
                ResourceMask overlap = supplier.ShippingMask & requester.RequestMask;
                if (overlap == 0)
                {
                    continue;
                }

                RoadPathSummary connectionSummary;
                if (supplier.Position.IsExternal) // NOTE: not 1:1 conversion from UpdateSellerPriority
                {
                    connectionSummary = new RoadPathSummary();
                    connectionSummary.DestinationIdx = (ushort)requester.Position.TileIndex; // NOTE: not 1:1 conversion from UpdateSellerPriority
                    connectionSummary.Connected = true;
                }
                else
                {
                    connectionSummary = RoadUtility.IsConnected(network, gridSize, supplier.Position.TileIndex, requester.Position.TileIndex); // NOTE: not 1:1 conversion from UpdateSellerPriority
                }

                if (!connectionSummary.Connected)
                {
                    continue;
                }
                if (connectionSummary.Distance != 0 && requester.IsLocalOption) // NOTE: not 1:1 conversion from UpdateSellerPriority
                {
                    // Exclude local options from lists of non-local tiles
                    continue;
                }
                if (requester.Position.TileIndex == supplier.Position.TileIndex && !requester.IsLocalOption) // NOTE: not 1:1 conversion from UpdateSellerPriority
                {
                    // Exclude a tile from supplying itself -- storage, I'm looking at you :(
                    continue;
                }

                bool proxyMatch = false;

                //External case
                if (supplier.Position.IsExternal) // NOTE: not 1:1 conversion from UpdateSellerPriority
                {
                    // Don't summon blimps until basic tutorial completes
                    if (tutorialState.CurrState <= TutorialState.State.InactiveSim)
                    {
                        continue;
                    }
                }
                // Regular case
                else
                {
                    // Adjust for if is a proxy connection
                    if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16)
                    {
                        // If proxy connection, ensure proxy is for the right type of resource
                        uint region = ZavalaGame.SimGrid.Terrain.Regions[connectionSummary.ProxyConnectionIdx];
                        if (network.ExportDepotMap.ContainsKey(region))
                        {
                            List<ResourceSupplierProxy> proxies = network.ExportDepotMap[region];
                            foreach (var proxy in proxies)
                            {
                                // For each export depot, check if supplier is connected to it.
                                if (connectionSummary.ProxyConnectionIdx == proxy.Position.TileIndex)
                                {
                                    ResourceMask proxyOverlap = overlap & proxy.ProxyMask;

                                    if (proxyOverlap != 0)
                                    {
                                        proxyMatch = true;
                                    }
                                }
                            }
                        }

                        if (!proxyMatch)
                        {
                            // no match
                            continue;
                        }
                    }
                }

                var adjustments = config.UserAdjustmentsPerRegion[requester.Position.RegionIndex]; // NOTE: not 1:1 conversion from UpdateSellerPriority

                ResourceId primary = ResourceUtility.FirstResource(overlap);
                List<ResourceId> allResources = ResourceUtility.AllResources(overlap);

                // Only apply import tax if shipping across regions. Then use import tax of purchaser
                // NOTE: the profit and tax revenues calculated below are on a per-unit basis. Needs to be multiplied by quantity when the actually sale takes place.
                int importCost = 0;
                if (supplier.Position.IsExternal || (supplier.Position.RegionIndex != requester.Position.RegionIndex))
                {
                    importCost = adjustments.ImportTax[primary];
                }
                float shippingCost = connectionSummary.Distance * config.TransportCosts.CostPerTile[primary];

                int salesTax = adjustments.PurchaseTax[primary];

                if (connectionSummary.ProxyConnectionIdx != Tile.InvalidIndex16)
                {
                    // add flat rate export depot shipping fee
                    shippingCost += config.TransportCosts.ExportDepotFlatRate[primary];
                }

                float purchaseCost = 0;
                // float relativeGain = 0; // How much this supplier stands to gain by NOT incurring penalties from not selling
                if (requester.IsLocalOption)
                {
                    // profit -= adjustments.RunoffPenalty[primary];
                }
                else
                {
                    if (requester.OverridesBuyPrice)
                    {
                        purchaseCost = requester.OverrideBlock[MarketUtility.ResourceIdToMarketIndex(primary)];
                    }
                    else
                    {
                        // Err towards buyer purchase cost (TODO: unless they accept anything?)
                        // profit = requester.PriceNegotiator.PriceBlock[primary];
                        purchaseCost = supplier.PriceNegotiator.SellPriceBlock[MarketUtility.ResourceIdToMarketIndex(primary)];
                    }

                    /*
                    if (supplier.Storage.StorageExtensionReq != null)
                    {
                        // since this has a local option that would penalize runoff, that means the farm would sell it to this alternative for that much less (so the score for the purchaser should be higher)
                        // relativeGain += adjustments.RunoffPenalty[primary];
                    }
                    */
                }

                float totalCostForBuyer = purchaseCost + shippingCost + salesTax + importCost;
                float score = totalCostForBuyer; // buyer wants to minimize score

                // NEGOTIATION PASS
                bool deprioritized = false; // appears in medium level feedback, but not considered as valid buyer to sell to
                /* if (!requester.PriceNegotiator.AcceptsAnyPrice && (requester.PriceNegotiator.PriceBlock[primary] < totalCostForBuyer))
                {
                    foreach (var currResource in allResources)
                    {
                        // Mark that the sellers/buyers would have had a match here if there prices were more reasonable
                        requester.PriceNegotiator.OfferedRecord[currResource] = 1;

                        // Check if requester is actively requesting
                        if (requester.Requested[currResource] >= 1)
                        {
                            supplier.PriceNegotiator.OfferedRecord[currResource] = 1;
                        }
                    }

                    // If price points don't overlap, not a valid buyer/seller pair.
                    deprioritized = true;
                }
                */

                m_SellerPriorityWorkList.PushBack(new MarketRequesterPriorityInfo()
                {
                    Distance = connectionSummary.Distance,
                    ShippingCost = (int)Math.Ceiling(shippingCost),
                    Mask = overlap,
                    Target = supplier,
                    ProxyIdx = connectionSummary.ProxyConnectionIdx,
                    Path = connectionSummary,
                    Cost = (int)Math.Ceiling(score),
                    Deprioritized = deprioritized,
                    ExternalSupplier = supplier.Position.IsExternal
                });
            }

            m_SellerPriorityWorkList.Sort((a, b) =>
            {
                int dif = a.Cost - b.Cost;
                if (dif == 0)
                {
                    // tie break
                    if (b.ExternalSupplier && a.ExternalSupplier)
                    {
                        // no additional tiebreaker (random)
                        return 0;
                    }
                    else if (b.ExternalSupplier)
                    {
                        // favor a
                        return -1;
                    }
                    else if (a.ExternalSupplier)
                    {
                        // favor b
                        return 1;
                    }
                    else
                    {
                        // favor closest option
                        return b.Distance - a.Distance;
                    }
                }
                else
                {
                    return dif;
                }
            });

            // TODO: buyer buy price is different from lowest cost

            m_SellerPriorityWorkList.CopyTo(requester.Priorities.PrioritizedSuppliers);
        }

        private void ReassignSellerHighestPriorityBuyer(ResourceSupplier supplier, ResourceMask resourceMask)
        {
            // TODO: return if resource is not in seller's shipping mask
            // TODO: trim RequestWorkList to only include this resource
            MarketRequestInfo? found = FindHighestPriorityBuyer(supplier, m_RequestWorkList, resourceMask, out int baseProfit, out int relativeGain, out GeneratedTaxRevenue baseTaxRevenue, out ushort proxyIdx, out RoadPathSummary summary, out int costToBuyer);

            // Only add to mapping if value is not null AND requester has not already been claimed in an optimal match
            // TODO: matched per resource
            if (found.HasValue && !found.Value.Requester.MatchedThisTick)
            {
                // Save the offer for buyer processing that comes later
                ushort familiarityScore = 0;
                MarketSupplierOffer supplierOffer = new MarketSupplierOffer(supplier, costToBuyer, baseProfit, relativeGain, baseTaxRevenue, proxyIdx, summary, (MarketRequestInfo)found, resourceMask, familiarityScore);
                AddSellerOfferEntry(found.Value.Requester, supplierOffer);
                if (!m_RequesterWorkList.Contains(found.Value.Requester))
                {
                    m_RequesterWorkList.PushBack(found.Value.Requester);
                }
            }
        }

        private void AddSellerOfferEntry(ResourceRequester requester, MarketSupplierOffer offer)
        {
            if (m_SupplierOfferMap.ContainsKey(requester))
            {
                var offers = m_SupplierOfferMap[requester];
                offers.PushBack(offer);
                m_SupplierOfferMap[requester] = offers;
            }
            else
            {
                RingBuffer<MarketSupplierOffer> sellerOffers = new RingBuffer<MarketSupplierOffer>(4, RingBufferMode.Expand);
                sellerOffers.PushBack(offer);
                m_SupplierOfferMap.Add(requester, sellerOffers);
            }
        }

        /// <summary>
        /// Once a buyer/seller match is made, handles the resource changes, queues fulfillment, etc.
        /// </summary>
        private void FinalizeSale(MarketData marketData, TutorialState tutorial, MarketConfig config, ResourceSupplier supplier, MarketRequestInfo? found, MarketSupplierOffer supplierOffer, int currMarketIndex)
        {
            ResourceBlock adjustedValueRequested = found.Value.Requested;
            MarketRequestInfo? adjustedFound = found;

            if (found.Value.Requester.InfiniteRequests)
            {
                // Set requested value equal to this suppliers stock
                for (int i = 0; i < (int)ResourceId.COUNT; i++)
                {
                    ResourceId resource = (ResourceId)i;
                    if ((supplier.Storage.Current[resource] != 0) && (found.Value.Requested[resource] != 0))
                    {
                        int extensionCount = 0;
                        if (supplier.Storage.StorageExtensionStore != null && !adjustedFound.Value.Requester.IsLocalOption)
                        {
                            extensionCount += supplier.Storage.StorageExtensionStore.Current[resource];
                        }

                        // Debug.Log("[Sitting] Set request to max " + (supplier.Storage.Current[resource] + extensionCount) + " for resource " + resource.ToString());
                        adjustedValueRequested[resource] = supplier.Storage.Current[resource] + extensionCount;
                    }
                }

                if (adjustedFound.Value.Requester.IsLocalOption)
                {
                    // Remove sales / import taxes (since essentially sold to itself)
                    supplierOffer.BaseTaxRevenue.Sales = 0;
                    supplierOffer.BaseTaxRevenue.Import = 0;
                }
                // TODO: Determine how storage should work with sales taxes

                adjustedFound = new MarketRequestInfo(found.Value.Requester, adjustedValueRequested);
            }

            if (!ResourceBlock.Fulfills(supplier.Storage.Current, adjustedValueRequested))
            {
                if (supplier.Storage.StorageExtensionReq == adjustedFound.Value.Requester)
                {
                    // local option was optimal buyer, so didn't sell to alternatives. But no change to be made to the storage or extended storage.
                    // requeue request
                    m_RequestWorkList.PushBack(found.Value);
                    return;
                }
            }

            int regionPurchasedIn = ZavalaGame.SimGrid.Terrain.Regions[adjustedFound.Value.Requester.Position.TileIndex];
            int quantity = adjustedValueRequested.Count; // TODO: may be buggy if we ever have requests that cover multiple resources
            GeneratedTaxRevenue netTaxRevenue = new GeneratedTaxRevenue(
                supplierOffer.BaseTaxRevenue.Sales * quantity,
                supplierOffer.BaseTaxRevenue.Import * quantity,
                supplierOffer.BaseTaxRevenue.Penalties * quantity
                );

            MarketActiveRequestInfo activeRequest;
            if (supplier.Storage.InfiniteSupply)
            {
                activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, ResourceBlock.FulfillInfinite(supplier.ShippingMask, adjustedValueRequested), netTaxRevenue, supplierOffer.ProxyIdx, supplierOffer.Path);
            }
            else
            {
                ResourceBlock mainStorageBlock;
                ResourceBlock extensionBlock = new ResourceBlock();

                if (!ResourceBlock.Fulfills(supplier.Storage.Current, adjustedValueRequested))
                {
                    // subtract main storage from whole value
                    mainStorageBlock = ResourceBlock.Consume(ref adjustedValueRequested, supplier.Storage.Current);

                    if (supplier.Storage.StorageExtensionStore != null)
                    {
                        // subtract remaining value from extension storage
                        extensionBlock = ResourceBlock.Consume(ref supplier.Storage.StorageExtensionStore.Current, adjustedValueRequested);
                    }
                }
                else
                {
                    mainStorageBlock = ResourceBlock.Consume(ref supplier.Storage.Current, adjustedValueRequested);
                }

                activeRequest = new MarketActiveRequestInfo(supplier, adjustedFound.Value, mainStorageBlock + extensionBlock, netTaxRevenue, supplierOffer.ProxyIdx, supplierOffer.Path);
            }

            if (activeRequest.Supplied.IsZero)
            {
                // a previous match handled this transaction
                return;
            }

            ResourceStorageUtility.RefreshStorageDisplays(supplier.Storage);
            if (supplierOffer.BaseProfit - supplierOffer.RelativeGain < 0)
            {
                if (!activeRequest.Requester.IsLocalOption)
                {
                    supplier.MatchedThisTick = true;

                    if (activeRequest.Supplied.Milk == 0)
                    {
                        supplier.SoldAtALossExcludingMilk = true;
                        // Log.Msg("[MarketSystem] SOLD AT A LOSS, EXCLUDING MILK");
                    }
                    else
                    {
                        supplier.MatchedThisTickWasMilk = true;
                    }

                }
            }

            int stressThreshold = config.StressedPurchaseThresholds[currMarketIndex];
            if (stressThreshold != 0 && supplierOffer.TotalCost > stressThreshold)
            {
                /*Log.Msg("[MarketSystem] {0} purchased {1} at {2}, which is over {3}!",
                    activeRequest.Requester.transform.name,
                    activeRequest.Supplied,
                    supplierOffer.TotalCost,
                    config.StressedPurchaseThresholds[dummyMarketIndex]);
                */
                activeRequest.Requester.PurchasedAtStressedPrice = true;
            }

            // MarketUtility.RecordRevenueToHistory(marketData, netTaxRevenue, regionPurchasedIn);

            // mark this request as EnRoute
            m_StateA.FulfillQueue.PushBack(activeRequest); // picked up by fulfillment system

            // Log.Msg("[MarketSystem] Shipping {0} from '{1}' to '{2}'", activeRequest.Supplied, supplier.name, adjustedFound.Value.Requester.name);

            if (tutorial.CurrState >= TutorialState.State.ActiveSim)
            {
                // save purchase to Price Negotiator memories (buyer and seller)
                for (int rIdx = 0; rIdx < (int)ResourceId.COUNT; rIdx++)
                {
                    ResourceId resource = (ResourceId)rIdx;
                    int memoryMarketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
                    if (activeRequest.Supplied[resource] != 0)
                    {
                        if (!activeRequest.Requester.IsLocalOption)
                        {
                            PriceNegotiatorUtility.SaveLastPrice(activeRequest.Requester.PriceNegotiator, memoryMarketIndex, activeRequest.Requester.PriceNegotiator.BuyPriceBlock[memoryMarketIndex], !supplier.PriceNegotiator.FixedSellOffer);
                            PriceNegotiatorUtility.SaveLastPrice(supplier.PriceNegotiator, memoryMarketIndex, supplier.PriceNegotiator.SellPriceBlock[memoryMarketIndex], !activeRequest.Requester.PriceNegotiator.FixedBuyOffer);
                        }
                    }
                }
            }

            if (!adjustedFound.Value.Requester.IsLocalOption)
            {
                MarketUtility.RecordPurchaseToHistory(marketData, activeRequest.Supplied, regionPurchasedIn);
            }
            else
            {
                ScriptUtility.Trigger(GameTriggers.LetSat);
            }

            adjustedFound.Value.Requester.MatchedThisTick = true;
            if (supplierOffer.BaseTaxRevenue.Sales < 0 || supplierOffer.BaseTaxRevenue.Import < 0)
            {
                adjustedFound.Value.Requester.SubsidyAppliedThisTick = true;
            }
        }

        private int GetMarketIndexOfResourceBlock(ResourceBlock supplied)
        {
            if (supplied.PhosphorusCount > 0)
            {
                return 0;
            }
            else if (supplied.Grain > 0)
            {
                return 1;
            }
            else if (supplied.Milk > 0)
            {
                return 2;
            }
            Log.Warn("[MarketSystem] Failed to get MarketIndex for ResourceBlock {0}", supplied);
            return -1;
        }

        private void ProcessNegotiations(TutorialState tutorial)
        {
            // NEGOTIATIONS FOR SUPPLIERS
            foreach (var supplier in m_SupplierWorkList)
            {
                if (tutorial.CurrState >= TutorialState.State.ActiveSim)
                {
                    if (!supplier.PriceNegotiator.FixedSellOffer)
                    {
                        supplier.PostSaleSnapshot = MarketUtility.GetCompleteStorage(supplier);
                        for (int i = 0; i < (int)ResourceId.COUNT; i++)
                        {
                            ResourceId resource = (ResourceId)i;
                            int marketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
                            if (supplier.PreSaleSnapshot[resource] == supplier.PostSaleSnapshot[resource] && (supplier.PreSaleSnapshot[resource] != 0))
                            {
                                // None of this resource was sold; add stress price if there were any valid connections to negotiate with
                                if (supplier.PriceNegotiator.OfferedRecord[marketIndex] > (int)NegotiableCode.NO_OFFER)
                                {
                                    // Push negotiator and resource type for negotiation system
                                    PriceNegotiation neg = new PriceNegotiation(supplier.PriceNegotiator, resource, false, true);
                                    MarketUtility.QueueNegotiation(neg);
                                }
                            }
                        }
                    }
                }
            }

            // NEGOTIATIONS FOR REQUESTERS
            if (tutorial.CurrState >= TutorialState.State.ActiveSim)
            {
                // for all remaining, increment their age
                for (int i = 0; i < m_RequestWorkList.Count; i++)
                {
                    m_RequestWorkList[i].Age++;
                    if (m_RequestWorkList[i].Age >= m_RequestWorkList[i].Requester.AgeOfUrgency && m_RequestWorkList[i].Requester.AgeOfUrgency > 0)
                    {
                        m_StateD.NewUrgents.Add(m_RequestWorkList[i]);
                        ZavalaGame.Events.Dispatch(ResourcePurchaser.Event_PurchaseUnfulfilled, m_RequestWorkList[i].Requester.Position.TileIndex);
                    }

                    // for each actor with one or more requests not fulfilled, add price stress if there were any valid sellers that refused
                    // only look for better deal if offerer is not a fixed sell offer
                    if (!m_RequestWorkList[i].Requester.PriceNegotiator.FixedBuyOffer)
                    {
                        ref ResourcePriceNegotiator negotiator = ref m_RequestWorkList[i].Requester.PriceNegotiator;
                        for (int rIdx = 0; rIdx < (int)ResourceId.COUNT; rIdx++)
                        {
                            ResourceId resource = (ResourceId)rIdx;
                            int marketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
                            if (((negotiator.OfferedRecord & m_RequestWorkList[i].Requester.RequestMask)[marketIndex] != 0) && (negotiator.OfferedRecord[marketIndex] > (int)NegotiableCode.NO_OFFER))
                            {
                                // Push negotiator and resource type for negotiation system
                                PriceNegotiation neg = new PriceNegotiation(negotiator, resource, true, false);
                                MarketUtility.QueueNegotiation(neg);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessSkimmerCosts(SimGridState grid, MarketData marketData, BudgetData budget)
        {
            // Record skimmer cost per region
            for (int region = 0; region < grid.RegionCount; region++)
            {
                int cost = 1; //m_StateB.UserAdjustmentsPerRegion[region].SkimmerCost;
                if (BudgetUtility.TrySpendBudget(budget, cost, (uint)region))
                {
                    MarketUtility.RecordSkimmerCostToHistory(marketData, -cost, region);
                }
                else
                {
                    string actor = "region" + (region + 1) + "_city1";
                    PolicyUtility.ForcePolicyToNone(PolicyType.SkimmingPolicy, actor, region);
                }
            }
        }


        #endregion // Market Processing

        private void MoveReceivedToStorage()
        {
            foreach (var requester in m_StateA.Buyers)
            {
                if (requester.Storage && !requester.Received.IsZero)
                {
                    requester.Storage.Current += requester.Received;
                    ResourceStorageUtility.RefreshStorageDisplays(requester.Storage);
                    requester.Received = default;
                }
            }
        }
    }
}