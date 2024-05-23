using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.Rendering.UI;
using Zavala.Building;
using Zavala.Data;

namespace Zavala.Economy {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceRequester))]
    public sealed class ResourcePurchaser : BatchedComponent, IPersistBuildingComponent {
        static public readonly StringHash32 Event_PurchaseMade = "ResourcePurchaser::ResourcePurchased";
        static public readonly StringHash32 Event_PurchaseUnfulfilled = "ResourcePurchaser::PurchaseUnfulfilled";

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock RequestAmount;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public ResourceBlock PurchasePrice;

        public MoneyProducer MoneyProducer;

        [NonSerialized] public ResourceRequester Request;
        [NonSerialized] public ResourceStorage Storage;

        [NonSerialized] public RingBuffer<ResourceBlock> RequestAmountHistory = new RingBuffer<ResourceBlock>(8, RingBufferMode.Overwrite);

        // TODO: okay to define these methods here and call them elsewhere or should they be defined in ResourcePurchaserSystem?
        public void ChangeRequestAmount(ResourceId resource, int change) {
            if (RequestAmount[resource] + change <= 0) return;
            RequestAmount[resource] += change;
        }
        //public void ChangePurchasePrice(ResourceId resource, int change) {
        //    if (PurchasePrice[resource] + change <= 0) return;
        //    PurchasePrice[resource] += change;
        //}
        //public void ChangeDemandTimer(ResourceId resource, int change)
        //{
        //    ChangeRequestAmount(resource, change);
        //    // ChangePurchasePrice(resource, change);
        //    Log.Msg("[ResourcePurchaser] {0} demand changed by {1} for actor {2}", resource, change, transform.name);
        //}
        public void ChangeDemandAmount(ResourceId resource, int change) {
            ChangeRequestAmount(resource, change);
            // ChangePurchasePrice(resource, change);
            Log.Debug("[ResourcePurchaser] {0} demand changed by {1} for actor {2}", resource, change, transform.name);
        }

        private void Awake() {
            this.CacheComponent(ref Storage);
            this.CacheComponent(ref Request);

            RequestAmountHistory.PushBack(RequestAmount);
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            RequestAmount.Write8(ref writer);
        }

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            RequestAmount.Read8(ref reader);
        }
    }
}