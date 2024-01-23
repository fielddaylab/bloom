using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Zavala.Data;
using Zavala.Roads;
using Zavala.Scripting;
using Zavala.Sim;
using Zavala.UI;
using static UnityEditor.Experimental.GraphView.Port;

namespace Zavala.Economy
{
    [SharedStateInitOrder(10)]
    public sealed class MarketData : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject
    {
        public SimTimer MarketTimer;
        public bool UpdatePrioritiesNow;
        [NonSerialized] public uint TickIndex;

        public RingBuffer<ResourceRequester> Buyers;
        public RingBuffer<ResourceSupplier> Suppliers;
        public RingBuffer<ResourcePriceNegotiator> Negotiators;

        // buffer for buyers that requested for this market cycle
        public RingBuffer<MarketRequestInfo> RequestQueue; // Queue of requests, sitting idle
        public RingBuffer<MarketActiveRequestInfo> FulfillQueue; // Queue of requests which have found a match and need to route through a fulfiller
        public RingBuffer<MarketActiveRequestInfo> ActiveRequests; // List of requests actively being fulfilled (en-route)
        public RingBuffer<PriceNegotiation> NegotiationQueue;


        // Pie Chart
        public DataHistory[] CFertilizerSaleHistory;
        public DataHistory[] ManureSaleHistory;
        public DataHistory[] DFertilizerSaleHistory;

        // Bar Chart (Financial Targets)
        public DataHistory[] SalesTaxHistory;
        public DataHistory[] ImportTaxHistory;
        public DataHistory[] PenaltiesHistory;

        // Skimmer
        public DataHistory[] SkimmerCostHistory;

        // Milk Revenue
        public DataHistory[] MilkRevenueHistory;

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("MarketData");
        }

        void IRegistrationCallbacks.OnRegister() {
            Buyers = new RingBuffer<ResourceRequester>(16, RingBufferMode.Expand);
            Suppliers = new RingBuffer<ResourceSupplier>(16, RingBufferMode.Expand);
            Negotiators = new RingBuffer<ResourcePriceNegotiator>(16, RingBufferMode.Expand);
            RequestQueue = new RingBuffer<MarketRequestInfo>(16, RingBufferMode.Expand);
            FulfillQueue = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
            ActiveRequests = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
            NegotiationQueue = new RingBuffer<PriceNegotiation>(16, RingBufferMode.Expand);

            int pieChartHistoryDepth = 36;
            DataHistoryUtil.InitializeDataHistory(ref CFertilizerSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref ManureSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref DFertilizerSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);

            int barChartHistoryDepth = 36;
            DataHistoryUtil.InitializeDataHistory(ref SalesTaxHistory, RegionInfo.MaxRegions, barChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref ImportTaxHistory, RegionInfo.MaxRegions, barChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref PenaltiesHistory, RegionInfo.MaxRegions, barChartHistoryDepth);

            int miscHistoryDepth = 4;
            DataHistoryUtil.InitializeDataHistory(ref SkimmerCostHistory, RegionInfo.MaxRegions, miscHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref MilkRevenueHistory, RegionInfo.MaxRegions, miscHistoryDepth);

            ZavalaGame.SaveBuffer.RegisterHandler("MarketData", this);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref byte* data, ref int remaining, SaveStateChunkConsts consts) {
            Unsafe.Read(ref TickIndex, ref data, ref remaining);

            // TODO: Implement
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref byte* data, ref int written, int capacity, SaveStateChunkConsts consts) {
            Unsafe.Write(TickIndex, ref data, ref written, capacity);
            
            // TODO: Implement
        }
    }

    public struct MarketSupplierPriorityList
    {
        public RingBuffer<MarketSupplierPriorityInfo> PrioritizedBuyers;

        public void Create() {
            PrioritizedBuyers = new RingBuffer<MarketSupplierPriorityInfo>(4, RingBufferMode.Expand);
        }
    }

    public struct MarketRequesterPriorityList
    {
        public RingBuffer<MarketRequesterPriorityInfo> PrioritizedSuppliers;

        public void Create()
        {
            PrioritizedSuppliers = new RingBuffer<MarketRequesterPriorityInfo>(4, RingBufferMode.Expand);
        }
    }

    public struct MarketSupplierOffer {
        public ResourceSupplier Supplier;
        public int TotalCost; // total cost to the buyer this is being offered to

        public int BaseProfit;
        public int RelativeGain;
        public GeneratedTaxRevenue BaseTaxRevenue;
        public ushort ProxyIdx;
        public RoadPathSummary Path;

        public ResourceMask ResourceMask;
        public MarketRequestInfo FoundRequest;

        public ushort FamiliarityScore;

        public MarketSupplierOffer(ResourceSupplier supplier, int totalCost, int baseProfit, int relativeGain, GeneratedTaxRevenue baseRevenue, ushort proxyIdx, RoadPathSummary path, MarketRequestInfo found, ResourceMask resourceMask, ushort familiarity)
        {
            Supplier = supplier;
            TotalCost = totalCost;

            BaseProfit = baseProfit;
            RelativeGain = relativeGain;
            BaseTaxRevenue = baseRevenue;
            ProxyIdx = proxyIdx;
            Path = path;

            FoundRequest = found;
            ResourceMask = resourceMask;

            FamiliarityScore = familiarity;
        }
    }

    public struct MarketSupplierPriorityInfo
    {
        public ResourceRequester Target;
        public ResourceMask Mask;
        public int Distance;
        public ushort ProxyIdx;
        public RoadPathSummary Path;
        public int Profit;
        public int CostToBuyer;
        public int ShippingCost;
        public int RelativeGain;
        public GeneratedTaxRevenue TaxRevenue;
        public bool Deprioritized;
    }

    // TODO: prune redundant fields as necessary
    public struct MarketRequesterPriorityInfo
    {
        public ResourceSupplier Target;
        public ResourceMask Mask;
        public int Distance;
        public ushort ProxyIdx;
        public RoadPathSummary Path;
        public int Cost;
        public int ShippingCost;
        public bool Deprioritized;
        public bool ExternalSupplier;
    }

    public struct MarketRequestInfo
    {
        public ResourceRequester Requester;
        public ResourceBlock Requested;
        public int Age;

        public MarketRequestInfo(ResourceRequester requester, ResourceBlock request) {
            Requester = requester;
            Requested = request;
            Age = 0;
        }
    }

    public struct GeneratedTaxRevenue
    {
        public int Sales;
        public int Import;
        public int Penalties;

        public GeneratedTaxRevenue(int sales, int import, int penalties) {
            Sales = sales;
            Import = import;
            Penalties = penalties;
        }
    }

    [Serializable]
    public struct TargetThreshold
    {
        public bool Below; // true for down/below, false for up/above
        public float Value; // threshold % (where the target bar is set)

        public TargetThreshold(bool below, float val) {
            Below = below;
            Value = val;
        }
    }

    public struct MarketActiveRequestInfo
    {
        public ResourceSupplier Supplier;
        public ResourceRequester Requester;
        public ushort ProxyIdx;
        public ResourceBlock Requested;
        public ResourceBlock Supplied;
        public RequestFulfiller Fulfiller;
        public GeneratedTaxRevenue Revenue;
        public RoadPathSummary Path;

        public MarketActiveRequestInfo(ResourceSupplier supplier, ResourceRequester requester, ushort proxyIdx, ResourceBlock request, ResourceBlock supply, GeneratedTaxRevenue revenue, RoadPathSummary summary) {
            Supplier = supplier;
            Requester = requester;
            ProxyIdx = proxyIdx;
            Requested = request;
            Supplied = supply;
            Fulfiller = null;
            Revenue = revenue;
            Path = summary;
        }

        public MarketActiveRequestInfo(ResourceSupplier supplier, MarketRequestInfo request, ResourceBlock supplied, GeneratedTaxRevenue revenue, ushort proxyIdx, RoadPathSummary summary) {
            Supplier = supplier;
            Requester = request.Requester;
            ProxyIdx = proxyIdx;
            Requested = request.Requested;
            Supplied = supplied;
            Fulfiller = null;
            Revenue = revenue;
            Path = summary;
        }
    }

    public struct MarketQueryResultInfo {
        public ResourceRequester Requester;
        public ResourceSupplier Supplier;
        public ResourceId Resource;
        public RoadPathFlags PathFlags;
        public int Profit;
        public int ShippingCost;
        public int Distance;
        public int CostToBuyer;
        public ushort ProxyIdx;
        public GeneratedTaxRevenue TaxRevenue;
    }

    /// <summary>
    /// Market utility methods.
    /// </summary>
    static public class MarketUtility
    {
        static public readonly int NumMarkets = 3; // Grain, Milk, and Phosphorus

        #region Register

        static public void RegisterBuyer(ResourceRequester requester) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            marketData.Buyers.PushBack(requester);
        }

        static public void DeregisterBuyer(ResourceRequester requester) {
            if (Game.IsShuttingDown) {
                return;
            }

            MarketData marketData = Game.SharedState.Get<MarketData>();
            if (marketData != null) {
                marketData.Buyers.FastRemove(requester);
            }
        }

        static public void RegisterSupplier(ResourceSupplier storage) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            marketData.Suppliers.PushBack(storage);
        }

        static public void DeregisterSupplier(ResourceSupplier storage) {
            if (Game.IsShuttingDown) {
                return;
            }

            MarketData marketData = Game.SharedState.Get<MarketData>();
            if (marketData != null) {
                marketData.Suppliers.FastRemove(storage);
            }
        }

        static public void RegisterNegotiator(ResourcePriceNegotiator negotiator)
        {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            marketData.Negotiators.PushBack(negotiator);
        }

        static public void DeregisterNegotiator(ResourcePriceNegotiator negotiator)
        {
            if (Game.IsShuttingDown)
            {
                return;
            }

            MarketData marketData = Game.SharedState.Get<MarketData>();
            if (marketData != null)
            {
                marketData.Negotiators.FastRemove(negotiator);
            }
        }

        static public void RegisterInfiniteProducer(ResourceProducer producer) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            // QueueRequest(producer.Request, producer.Requires);
            QueueMultipleSingleRequests(producer.Request, producer.Requires);
        }

        static public void DeregisterInfiniteProducer(ResourceProducer producer) {
            if (Game.IsShuttingDown) {
                return;
            }
        }

        #endregion // Register

        #region Requests

        /// <summary>
        /// Queues a resource request, using the given resource request.
        /// </summary>
        static public bool QueueRequest(ResourceRequester requester, ResourceBlock request) {
            request &= requester.RequestMask;

            if (requester.Storage != null 
                && !ResourceBlock.CanAddFull(in requester.Storage.Current, in request, in requester.Storage.Capacity)) {
                // couldn't fit this request in storage, don't queue it
                Log.Warn("[MarketUtility] Requester {0} could not fit request {1}, not queuing", requester, request);
                return false;
            }
            if (request.IsPositive && requester.RequestCount < requester.MaxRequests) {
                requester.Requested += request;
                requester.RequestCount++;
                MarketData marketData = Game.SharedState.Get<MarketData>();
                Assert.NotNull(marketData);
                marketData.RequestQueue.PushBack(new MarketRequestInfo(requester, request));
                Log.Msg("[MarketUtility] Producer '{0}' requested {1}", requester.name, request);
                return true;
            }

            return false;
        }

        // this feels so gross i'm sorry
        static public bool QueueMultipleSingleRequests(ResourceRequester requester, ResourceBlock request) {
            if (request.IsZero) return true; // nothing here, do nothing and return success
            for (int i = 0; i < request.Manure; i++) {
                if (!QueueRequest(requester, new ResourceBlock() {Manure = 1})) return false;
            }
            for (int i = 0; i < request.MFertilizer; i++) {
                if (!QueueRequest(requester, new ResourceBlock() { MFertilizer = 1 })) return false;
            }
            for (int i = 0; i < request.DFertilizer; i++) {
                if (!QueueRequest(requester, new ResourceBlock() { DFertilizer = 1 })) return false;
            }
            for (int i = 0; i < request.Milk; i++) {
                if (!QueueRequest(requester, new ResourceBlock() { Milk = 1 })) return false;
            }
            for (int i = 0; i < request.Grain; i++) {
                if (!QueueRequest(requester, new ResourceBlock() { Grain = 1 })) return false;
            }
            return true;
        }

        /// <summary>
        /// QUeues a request to be actively fulfilled.
        /// </summary>
        static public void QueueShipping(MarketActiveRequestInfo request) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            marketData.FulfillQueue.PushBack(request);
        }

        static public bool CanHoldRequest(ResourceRequester requester) {
            if (requester.Storage == null) {
                Log.Warn("[CanHoldRequest] Requester {0} has null storage, returning false", requester.name);
                return false;
            }
            Log.Msg("[CanHoldRequest] Checking Requester {0}...", requester.name);
            return ResourceBlock.CanAddFull(requester.Storage.Current, requester.Requested, requester.Storage.Capacity);
        }

        #endregion // Requests

        #region Production

        /// <summary>
        /// Returns if the given resource producer can produce right away.
        /// </summary>
        static public bool CanProduceNow(ResourceProducer producer, out ResourceBlock produced) {
            ResourceStorage storage = producer.Storage;
            produced = producer.Produces;
            if (produced.IsZero) {
                return false;
            }

            return ResourceBlock.CanAddFull(storage.Current, produced, storage.Capacity) && ResourceBlock.Fulfills(storage.Current, producer.Requires);
        }

        /// <summary>
        /// Returns if the given money producer can produce right away.
        /// </summary>
        static public bool CanProduceNow(MoneyProducer producer, out int producedAmt) {
            ResourceStorage storage = producer.Storage;
            producedAmt = producer.ProducesAmt;
            if (producedAmt == 0) {
                return false;
            }

            return ResourceBlock.Fulfills(storage.Current, producer.Requires);
        }

        #endregion // Production

        #region DataHistory

        public static void RecordPurchaseToHistory(MarketData marketData, ResourceBlock purchased, int regionIndex) {
            // MFertilizer is CFertilizer
            if (purchased.MFertilizer != 0) {
                marketData.CFertilizerSaleHistory[regionIndex].AddPending(purchased.MFertilizer);
            }
            if (purchased.Manure != 0) {
                marketData.ManureSaleHistory[regionIndex].AddPending(purchased.Manure);
            }
            if (purchased.DFertilizer != 0) {
                marketData.DFertilizerSaleHistory[regionIndex].AddPending(purchased.DFertilizer);
            }
        }

        public static void RecordRevenueToHistory(MarketData marketData, GeneratedTaxRevenue taxRevenue, int regionIndex) {
            if (taxRevenue.Sales != 0) {
                marketData.SalesTaxHistory[regionIndex].AddPending(taxRevenue.Sales);
            }
            if (taxRevenue.Import != 0) {
                marketData.ImportTaxHistory[regionIndex].AddPending(taxRevenue.Import);
            }
            if (taxRevenue.Penalties != 0) {
                marketData.PenaltiesHistory[regionIndex].AddPending(taxRevenue.Penalties);
            }
        }

        public static void RecordSkimmerCostToHistory(MarketData marketData, int skimmerCost, int regionIndex)
        {
            marketData.SkimmerCostHistory[regionIndex].AddPending(skimmerCost);
        }

        public static void RecordMilkRevenueToHistory(MarketData marketData, int milkRevenue, int regionIndex) {
            marketData.MilkRevenueHistory[regionIndex].AddPending(milkRevenue);
        }

        public static void FinalizeCycleHistory(MarketData marketData) {
            // Pie Chart
            for (int i = 0; i < marketData.CFertilizerSaleHistory.Length; i++) {
                marketData.CFertilizerSaleHistory[i].ConvertPending();
            }
            for (int i = 0; i < marketData.ManureSaleHistory.Length; i++) {
                marketData.ManureSaleHistory[i].ConvertPending();
            }
            for (int i = 0; i < marketData.DFertilizerSaleHistory.Length; i++) {
                marketData.DFertilizerSaleHistory[i].ConvertPending();
            }

            // Bar Chart (Financial Targets)
            for (int i = 0; i < marketData.SalesTaxHistory.Length; i++) {
                marketData.SalesTaxHistory[i].ConvertPending();
            }
            for (int i = 0; i < marketData.ImportTaxHistory.Length; i++) {
                marketData.ImportTaxHistory[i].ConvertPending();
            }
            for (int i = 0; i < marketData.PenaltiesHistory.Length; i++) {
                marketData.PenaltiesHistory[i].ConvertPending();
            }

            // Skimmer
            for (int i = 0; i < marketData.SkimmerCostHistory.Length; i++)
            {
                marketData.SkimmerCostHistory[i].ConvertPending();
            }

            // Milk Revenue
            for (int i = 0; i < marketData.MilkRevenueHistory.Length; i++) {
                marketData.MilkRevenueHistory[i].ConvertPending();
            }
        }

        public static void CalculateRatios(ref float[] ratios, int[] values) {
            int total = 0;

            for (int i = 0; i < values.Length; i++) {
                total += values[i];
            }

            if (total <= 0) {
                total = 1;
            }

            if (ratios == null) {
                ratios = new float[values.Length];
            }

            for (int i = 0; i < ratios.Length; i++) {
                ratios[i] = (float)values[i] / total;
            }
        }

        public static void CalculateRatios(ref float[] ratios, UnsafeSpan<int> values) {
            int total = 0;

            for (int i = 0; i < values.Length; i++) {
                total += values[i];
            }

            if (total <= 0) {
                total = 1;
            }

            if (ratios == null) {
                ratios = new float[values.Length];
            }

            for (int i = 0; i < ratios.Length; i++) {
                ratios[i] = (float) values[i] / total;
            }
        }

        #endregion // DataHistory

        public static bool EvaluateTargetThreshold(float value, TargetThreshold target) {
            if ((value >= target.Value && target.Below) || (value <= target.Value && !target.Below)) {
                return false;
            }

            return true;
        }

        #region Roads

        public static void TriggerConnectionTriggers(MarketData marketData, RoadNetwork network, HexGridSize gridSize) {
            foreach(var supplier in marketData.Suppliers) {
                foreach (var requester in marketData.Buyers) {
                    // ignore buyers that don't overlap with shipping mask
                    ResourceMask overlap = requester.RequestMask & supplier.ShippingMask;
                    if (overlap == 0) {
                        continue;
                    }

                    RoadPathSummary connectionSummary = RoadUtility.IsConnected(network, gridSize, supplier.Position.TileIndex, requester.Position.TileIndex);
                    if (!connectionSummary.Connected) {
                        continue;
                    }
                    if (connectionSummary.Distance != 0 && requester.IsLocalOption) {
                        // Exclude local options from lists of non-local tiles
                        continue;
                    }

                    // Check for cafo-grain farm connection
                    if (!Game.SharedState.Get<BlueprintState>().IsActive)
                    {
                        bool dairyFarmSupplier = supplier.Position.Type == BuildingType.DairyFarm;
                        bool grainFarmRequester = requester.Position.Type == BuildingType.GrainFarm;
                        bool cityRequester = requester.Position.Type == BuildingType.City;
                        bool storageRequester = requester.Position.Type == BuildingType.Storage;
                        bool difTiles = supplier.Position.TileIndex != requester.Position.TileIndex;
                        if (dairyFarmSupplier && grainFarmRequester && difTiles)
                        {
                            ScriptUtility.Trigger(GameTriggers.FarmConnection);
                            Game.SharedState.Get<WinLossState>().FarmsConnectedInRegion[requester.Position.RegionIndex] = true;
                        }
                        if (dairyFarmSupplier && cityRequester && difTiles)
                        {
                            ScriptUtility.Trigger(GameTriggers.CityConnection);
                        }
                        if (storageRequester) {

                        }
                    }
                }
            }
        }

        #endregion // Roads

        #region Queries

        /// <summary>
        /// Gathers info on where the given requester is purchasing their supplies from.
        /// </summary>
        static public int GatherPurchaseSources(ResourceRequester requester, RingBuffer<MarketQueryResultInfo> buffer, ResourceMask resource) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            int count = 0;
            foreach(var supplier in marketData.Suppliers) {
                // if local option is best option for given resource, product is not up for sale
                bool localIsBest = false;
                foreach (var data in supplier.Priorities.PrioritizedBuyers) { 
                    if ((data.Target.RequestMask & resource) != 0) {
                        if (data.Target.IsLocalOption) {
                            localIsBest = true;
                            break;
                        }
                        else {
                            localIsBest = false;
                            break;
                        }
                    }
                }
                if (localIsBest) {
                    continue;
                }
                // Grain farm in another region is being populated with data from a grain farm within local region
                // else product is up for sale
                foreach(var data in supplier.Priorities.PrioritizedBuyers) {
                    if (data.Target == requester && (data.Mask & resource) != 0) {
                        buffer.PushBack(new MarketQueryResultInfo() {
                            Requester = requester,
                            Supplier = supplier,
                            Resource = ResourceUtility.FirstResource((data.Mask & resource)),
                            ShippingCost = data.ShippingCost,
                            Distance = data.Distance,
                            PathFlags = data.Path.Flags,
                            Profit = data.Profit /*- data.RelativeGain*/,
                            CostToBuyer = data.CostToBuyer,
                            ProxyIdx = data.ProxyIdx,
                            TaxRevenue = data.TaxRevenue // NOTE: does not return correct value when supplier ships through export depot proxy
                        });
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Updates the result from GatherPurchaseSources.
        /// Will not find any new sources, but will update prices, distance, etc.
        /// </summary>
        static public int UpdatePurchaseSources(ResourceRequester requester, RingBuffer<MarketQueryResultInfo> buffer) {
            for(int i = buffer.Count - 1; i >= 0; i--) {
                ref MarketQueryResultInfo result = ref buffer[i];
                bool found = false;
                foreach(var data in result.Supplier.Priorities.PrioritizedBuyers) {
                    if (data.Target == requester) {
                        result.ShippingCost = data.ShippingCost;
                        result.Distance = data.Distance;
                        result.PathFlags = data.Path.Flags;
                        result.Profit = data.Profit /*- data.RelativeGain*/;
                        result.TaxRevenue = data.TaxRevenue;
                        result.CostToBuyer = data.CostToBuyer;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    buffer.FastRemoveAt(i);
                }
            }
            return buffer.Count;
        }

        static public int GatherShippingSources(ResourceSupplier supplier, RingBuffer<MarketQueryResultInfo> buffer, ResourceMask resource) {
            int count = 0;
            foreach (var data in supplier.Priorities.PrioritizedBuyers) {
                if ((data.Mask & resource) != 0) {
                    buffer.PushBack(new MarketQueryResultInfo() {
                        Requester = data.Target,
                        Supplier = supplier,
                        Resource = ResourceUtility.FirstResource(data.Mask & resource),
                        ShippingCost = data.ShippingCost,
                        Distance = data.Distance,
                        PathFlags = data.Path.Flags,
                        Profit = data.Profit /* - data.RelativeGain */,
                        TaxRevenue = data.TaxRevenue
                    });
                    count++;
                }
            }
            return count;
        }

        static public int UpdateShippingSources(ResourceSupplier supplier, RingBuffer<MarketQueryResultInfo> buffer, ResourceMask resource) {
            // pretty much identical to gathering, not much point in doing a fancier search
            buffer.Clear();
            return GatherShippingSources(supplier, buffer, resource);
        }

        static public ResourceBlock GetCompleteStorage(ResourceSupplier supplier)
        {
            ResourceBlock totalStorage = supplier.Storage.Current;
            if (supplier.Storage.StorageExtensionStore != null)
            {
                totalStorage += supplier.Storage.StorageExtensionStore.Current;
            }

            return totalStorage;
        }

        #endregion // Queries

        #region Negotiation

        /// <summary>
        /// Queues a price negotiation
        /// </summary>
        static public void QueueNegotiation(PriceNegotiation negotiation)
        {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            marketData.NegotiationQueue.PushBack(negotiation);
        }

        #endregion // Negotiation

        #region Market Index

        private const int PHOSPH_INDEX = 0;
        private const int GRAIN_INDEX = 1;
        private const int MILK_INDEX = 2;

        /// <summary>
        /// Converts a resource mask into a market index
        /// </summary>
        /// <returns></returns>
        static public int ResourceMaskToMarketIndex(ResourceMask mask)
        {
            switch(mask)
            {
                case ResourceMask.Phosphorus:
                    return PHOSPH_INDEX;
                case ResourceMask.Grain:
                    return GRAIN_INDEX;
                case ResourceMask.Milk:
                    return MILK_INDEX;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Converts a resource mask into a market index
        /// </summary>
        /// <returns></returns>
        static public ResourceMask MarketIndexToResourceMask(int index)
        {
            switch (index)
            {
                case PHOSPH_INDEX:
                    return ResourceMask.Phosphorus;
                case GRAIN_INDEX:
                    return ResourceMask.Grain;
                case MILK_INDEX:
                    return ResourceMask.Milk;
                default:
                    return ResourceMask.Milk;
            }
        }

        /// <summary>
        /// Converts a resource mask into a market index
        /// </summary>
        /// <returns></returns>
        static public int ResourceIdToMarketIndex(ResourceId resource)
        {
            switch (resource)
            {
                case ResourceId.Manure:
                case ResourceId.MFertilizer:
                case ResourceId.DFertilizer:
                    return PHOSPH_INDEX;
                case ResourceId.Grain:
                    return GRAIN_INDEX;
                case ResourceId.Milk:
                    return MILK_INDEX;
                default:
                    return -1;
            }
        }

        #endregion // Market Index
    }
}