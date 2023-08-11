using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using Zavala.Roads;
using Zavala.Sim;

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

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Buyers = new RingBuffer<ResourceRequester>(16, RingBufferMode.Expand);
            Suppliers = new RingBuffer<ResourceSupplier>(16, RingBufferMode.Expand);
            RequestQueue = new RingBuffer<MarketRequestInfo>(16, RingBufferMode.Expand);
            FulfullQueue = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
            ActiveRequests = new RingBuffer<MarketActiveRequestInfo>(16, RingBufferMode.Expand);
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
        /// Returns if the given producer can produce right away.
        /// </summary>
        static public bool CanProduceNow(ResourceProducer producer, out ResourceBlock produced) {
            ResourceStorage storage = producer.Storage;
            produced = producer.Produces;
            if (produced.IsZero) {
                return false;
            }

            return ResourceBlock.CanAddFull(storage.Current, produced, storage.Capacity) && ResourceBlock.Fulfills(storage.Current, producer.Requires);
        }

        #endregion // Production
    }
}