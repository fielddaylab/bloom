using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Zavala.Data;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Economy
{
    [SharedStateInitOrder(10)]
    public sealed class MarketData : SharedStateComponent, IRegistrationCallbacks
    {
        public SimTimer MarketTimer;

        public RingBuffer<ResourceRequester> Buyers;
        public RingBuffer<ResourceSupplier> Suppliers;

        // buffer for buyers that requested for this market cycle
        public RingBuffer<MarketRequestInfo> RequestQueue; // Queue of requests, sitting idle
        public RingBuffer<MarketActiveRequestInfo> FulfillQueue; // Queue of requests which have found a match and need to route through a fulfiller
        public RingBuffer<MarketActiveRequestInfo> ActiveRequests; // List of requests actively being fulfilled (en-route)

        // Pie Chart
        public DataHistory[] CFertilizerSaleHistory;
        public DataHistory[] ManureSaleHistory;
        public DataHistory[] DFertilizerSaleHistory;

        // Bar Chart (Financial Targets)
        public DataHistory[] SalesTaxHistory;
        public DataHistory[] ImportTaxHistory;
        public DataHistory[] PenaltiesHistory;


        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Buyers = new RingBuffer<ResourceRequester>(16, RingBufferMode.Expand);
            Suppliers = new RingBuffer<ResourceSupplier>(16, RingBufferMode.Expand);
            RequestQueue = new RingBuffer<MarketRequestInfo>(16, RingBufferMode.Expand);
            FulfillQueue = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
            ActiveRequests = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);

            int pieChartHistoryDepth = 10;
            DataHistoryUtil.InitializeDataHistory(ref CFertilizerSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref ManureSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref DFertilizerSaleHistory, RegionInfo.MaxRegions, pieChartHistoryDepth);

            int barChartHistoryDepth = 10;
            DataHistoryUtil.InitializeDataHistory(ref SalesTaxHistory, RegionInfo.MaxRegions, barChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref ImportTaxHistory, RegionInfo.MaxRegions, barChartHistoryDepth);
            DataHistoryUtil.InitializeDataHistory(ref PenaltiesHistory, RegionInfo.MaxRegions, barChartHistoryDepth);
        }
    }

    public struct MarketSupplierPriorityList
    {
        public RingBuffer<MarketSupplierPriorityInfo> PrioritizedBuyers;

        public void Create() {
            PrioritizedBuyers = new RingBuffer<MarketSupplierPriorityInfo>(4, RingBufferMode.Expand);
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
        public GeneratedTaxRevenue TaxRevenue;
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

    /// <summary>
    /// Market utility methods.
    /// </summary>
    static public class MarketUtility
    {
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

        #endregion // Register

        #region Requests

        /// <summary>
        /// Queues a resource request, using the given resource request.
        /// </summary>
        static public bool QueueRequest(ResourceRequester requester, ResourceBlock request) {
            request &= requester.RequestMask;

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

        /// <summary>
        /// QUeues a request to be actively fulfilled.
        /// </summary>
        static public void QueueShipping(MarketActiveRequestInfo request) {
            MarketData marketData = Game.SharedState.Get<MarketData>();
            Assert.NotNull(marketData);
            marketData.FulfillQueue.PushBack(request);
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
            // TODO: if we're only ever checking these in the intro, may be worth adding another variable to only calc this during intro
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
                    bool dairyFarmSupplier = supplier.Position.Type == BuildingType.DairyFarm;
                    bool grainFarmRequester = requester.Position.Type == BuildingType.GrainFarm;
                    bool cityRequester = requester.Position.Type == BuildingType.City;
                    bool difTiles = supplier.Position.TileIndex != requester.Position.TileIndex;
                    if (dairyFarmSupplier && grainFarmRequester && difTiles) {
                        ScriptUtility.Trigger(GameTriggers.FarmConnection);
                    }
                    if (dairyFarmSupplier && cityRequester && difTiles) {
                        ScriptUtility.Trigger(GameTriggers.CityConnection);
                    }
                }
            }
        }

        #endregion // Roads
    }
}