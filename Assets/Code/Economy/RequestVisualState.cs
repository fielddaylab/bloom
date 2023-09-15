using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Roads;

namespace Zavala.Economy {
    public class RequestVisualState : SharedStateComponent, IRegistrationCallbacks
    {
        [Serializable] public class RequestVisualPool : SerializablePool<RequestVisual> { }

        public RequestVisualPool RequestPool;

        public List<MarketRequestInfo> NewUrgents;

        public RingBuffer<MarketActiveRequestInfo> FulfilledQueue;
        // TODO: there's got to be a better system than this. Some sort of unique id to each requestVisual so there aren't all these lookups.
        public Dictionary<ResourceRequester, RingBuffer<RequestVisual>> VisualMap; // maps a ring of request visuals to each requester

        public void OnRegister() {
            FulfilledQueue = new RingBuffer<MarketActiveRequestInfo>(8, RingBufferMode.Expand);
            VisualMap = new Dictionary<ResourceRequester, RingBuffer<RequestVisual>>();
            NewUrgents = new List<MarketRequestInfo>();
        }

        public void OnDeregister() {
        }
    }
}
