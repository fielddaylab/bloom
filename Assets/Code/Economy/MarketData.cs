using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using Zavala.Data;
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
        public RingBuffer<MarketRequestInfo> RequestQueue;
        public RingBuffer<MarketActiveRequestInfo> FulfullQueue;
        public RingBuffer<MarketActiveRequestInfo> ActiveRequests;

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
            FulfullQueue = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
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

    public struct GeneratedTaxRevenue {
        public int Sales;
        public int Import;
        public int Penalties;

        public GeneratedTaxRevenue(int sales, int import, int penalties) {
            Sales = sales;
            Import = import;
            Penalties = penalties;
        }
    }


    public struct MarketActiveRequestInfo
    {
        public ResourceSupplier Supplier;
        public ResourceRequester Requester;
        public ResourceBlock Requested;
        public RequestFulfiller Fulfiller;

        public MarketActiveRequestInfo(ResourceSupplier supplier, ResourceRequester requester, ResourceBlock request) {
            Supplier = supplier;
            Requester = requester;
            Requested = request;
            Fulfiller = null;
        }

        public MarketActiveRequestInfo(ResourceSupplier supplier, MarketRequestInfo request) {
            Supplier = supplier;
            Requester = request.Requester;
            Requested = request.Requested;
            Fulfiller = null;
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
            marketData.FulfullQueue.PushBack(request);
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

        #endregion // DataHistory
    }
}