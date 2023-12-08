using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zavala.Economy;
using static UnityEngine.Rendering.CoreUtils;
using Zavala.Roads;
using static FieldDay.Audio.AudioMgr;
using FieldDay;
using BeauUtil;
using UnityEditor.SceneManagement;
using Mono.Cecil;

namespace Zavala.Economy {
    [RequireComponent(typeof(OccupiesTile))]
    public class ResourcePriceNegotiator : BatchedComponent
    {
        public ResourceBlock PriceBlock = new ResourceBlock(); // price at which this purchaser/seller purchases/sells a given resource
        // public ResourceBlock MemoryPriceBlock = new ResourceBlock(); // price at which purchaser/seller last purchased/sold a given resource

        public int PriceStep = 1;

        public bool AcceptsAnyPrice; // Accepts any price when purchasing
        public bool FixedOffer;      // Does not modify price when selling 

        protected override void OnEnable()
        {
            base.OnEnable();

            MarketUtility.RegisterNegotiator(this);
        }

        protected override void OnDisable()
        {
            MarketUtility.DeregisterNegotiator(this);

            base.OnDisable();
        }
    }

    public static class PriceNegotiatorUtility
    {
        /// <summary>
        /// Adjusts the given price at which a resource type is bought/sold
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        /// <param name="priceDelta"></param>
        public static void AdjustPrice(ResourcePriceNegotiator negotiator, ResourceId resource, int priceDelta)
        {
            negotiator.PriceBlock[resource] += priceDelta;
        }

        /// <summary>
        /// Save last transaction price to memory
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        public static void SaveLastPrice(ResourcePriceNegotiator negotiator, ResourceId resource, int price)
        {
            // negotiator.MemoryPriceBlock[resource] = price;
        }

        /// <summary>
        /// Load last transaction price from memory
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        public static void LoadLastPrice(ResourcePriceNegotiator negotiator, ResourceId resource)
        {
            // negotiator.PriceBlock[resource] = negotiator.MemoryPriceBlock[resource];
        }

        public static void InitializeSupplierNegotiator(ResourcePriceNegotiator negotiator, ResourceMask sells, int regionIndex)
        {
            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            // negotiator.MemoryPriceBlock += config.DefaultPurchasePerRegion[regionIndex].Sell & sells;
            negotiator.PriceBlock += config.DefaultPurchasePerRegion[regionIndex].Buy & sells;
            // negotiator.MemoryPriceBlock = negotiator.PriceBlock;
        }

        public static void InitializeRequesterNegotiator(ResourcePriceNegotiator negotiator, ResourceMask buys, int regionIndex)
        {
            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            negotiator.PriceBlock += config.DefaultPurchasePerRegion[regionIndex].Buy & buys;
            // negotiator.MemoryPriceBlock = negotiator.PriceBlock;
        }
    }
}
