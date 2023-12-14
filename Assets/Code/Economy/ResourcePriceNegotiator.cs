using FieldDay.Components;
using System;
using UnityEngine;
using FieldDay;

namespace Zavala.Economy {
    public enum NegotiatorType
    {
        Seller,
        Buyer
    }

    public struct PriceNegotiation
    {
        public ResourcePriceNegotiator Negotiator;
        public ResourceId ResourceType;
        public bool IsIncrease; // price threshold increases if seller is stressed or buyer is not stressed

        public PriceNegotiation(ResourcePriceNegotiator neg, ResourceId type, bool isIncrease)
        {
            Negotiator = neg;
            ResourceType = type;
            IsIncrease = isIncrease;
        }
    }

    public enum NegotiableCode
    {
        NO_OFFER,
        NEGOTIABLE, 
        NON_NEGOTIABLE
    }

    [RequireComponent(typeof(OccupiesTile))]
    public class ResourcePriceNegotiator : BatchedComponent
    {
        public MarketPriceBlock PriceBlock = new MarketPriceBlock(); // price at which this purchaser/seller purchases/sells a given resource
        // public ResourceBlock MemoryPriceBlock = new ResourceBlock(); // price at which purchaser/seller last purchased/sold a given resource

        public int PriceStep = 1;

        public bool AcceptsAnyPrice; // Accepts any price when purchasing
        public bool FixedSellOffer;      // Does not modify price when selling 
        public bool FixedBuyOffer;      // Does not modify price when purchasing

        /*
        [NonSerialized] public ResourceBlock OfferedRecord; // Record of whether there was some other entity to negotiate price with, for each resource
        [NonSerialized] public ResourceBlock SettledRecord; // Record of whether a deal was settled (price overlap) since last negotiation phase
        [NonSerialized] public ResourceBlock PriceChange; // Price change per market tick, for each resource
        */

        [NonSerialized] public MarketPriceBlock OfferedRecord; // Record of whether there was some other entity to negotiate price with, for each resource
        [NonSerialized] public MarketPriceBlock SettledRecord; // Record of whether a deal was settled (price overlap) since last negotiation phase
        [NonSerialized] public MarketPriceBlock PriceChange; // Price change per market tick, for each resource

        [NonSerialized] public bool PriceNegotiated;

        [NonSerialized] public ResourceMask BuyMask;
        [NonSerialized] public ResourceMask SellMask;

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
        public static void StagePrice(ref ResourcePriceNegotiator negotiator, ResourceId resource, int priceDelta)
        {
            int marketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
            // only apply priceDelta once per market tick (even if multiple requests went unfulfilled)
            negotiator.PriceChange[marketIndex] = priceDelta;
        }

        /// <summary>
        /// Finalizes the staged price changes
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        /// <param name="priceDelta"></param>
        public static void FinalizePrice(ref ResourcePriceNegotiator negotiator, ResourceId resource)
        {
            int marketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
            negotiator.PriceBlock += negotiator.PriceChange;
            negotiator.PriceChange[marketIndex] = 0;
        }

        /// <summary>
        /// For every resource that was sold, try to get a better deal next time
        /// </summary>
        /// <param name="negotiator"></param>
        public static void ImprovePrices(ResourcePriceNegotiator negotiator)
        {

            // for each resource, if settled record, if buy mask add, if sell mask reduce
            // If negotiator.SettledRecord

            int priceStep;

            for (int i = 0; i < (int)ResourceId.COUNT; i++)
            {
                ResourceId resource = (ResourceId)i;
                int marketIndex = MarketUtility.ResourceIdToMarketIndex(resource);
                if (negotiator.SettledRecord[marketIndex] > 0)
                {
                    // if overlaps with sell mask (is selling this resource)
                    if ((negotiator.SettledRecord & negotiator.SellMask)[marketIndex] > 0)
                    {
                        if (!negotiator.FixedSellOffer && negotiator.SettledRecord[marketIndex] == (int)NegotiableCode.NEGOTIABLE)
                        {
                            priceStep = MarketParams.NegotiationStep;
                            StagePrice(ref negotiator, resource, priceStep);
                            FinalizePrice(ref negotiator, resource);
                        }
                    }
                    // else if overlaps with buy mask (is buying this resource)
                    else if ((negotiator.SettledRecord & negotiator.BuyMask)[marketIndex] > 0)
                    {
                        if (!negotiator.FixedBuyOffer && negotiator.SettledRecord[marketIndex] == (int)NegotiableCode.NEGOTIABLE)
                        {
                            priceStep = -MarketParams.NegotiationStep;
                            StagePrice(ref negotiator, resource, priceStep);
                            FinalizePrice(ref negotiator, resource);
                        }
                    }
                }
            }

            //negotiator.PriceBlock += negotiator.PriceChange;
            //negotiator.PriceChange[resource] = 0;
        }

        /// <summary>
        /// Save last transaction price to memory
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        public static void SaveLastPrice(ResourcePriceNegotiator negotiator, int marketIndex, int price, bool negotiable)
        {
            negotiator.SettledRecord[marketIndex] = negotiable ? (int)NegotiableCode.NEGOTIABLE : (int)NegotiableCode.NON_NEGOTIABLE;
            // negotiator.MemoryPriceBlock[resource] = price;
        }

        /// <summary>
        /// Load last transaction price from memory
        /// </summary>
        /// <param name="negotiator"></param>
        /// <param name="resource"></param>
        public static void LoadLastPrice(ResourcePriceNegotiator negotiator, int marketIndex)
        {
            // negotiator.PriceBlock[resource] = negotiator.MemoryPriceBlock[resource];
        }

        public static void InitializeSupplierNegotiator(ResourcePriceNegotiator negotiator, ResourceMask sells, int regionIndex)
        {
            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            // negotiator.MemoryPriceBlock += config.DefaultPurchasePerRegion[regionIndex].Sell & sells;
            // negotiator.PriceBlock += config.DefaultMarketPurchasePerRegion[regionIndex].Sell & sells;
            negotiator.PriceBlock += config.DefaultMarketPurchasePerRegion[regionIndex].Sell & sells;
            negotiator.SellMask = sells;
            // negotiator.MemoryPriceBlock = negotiator.PriceBlock;
        }

        public static void InitializeRequesterNegotiator(ResourcePriceNegotiator negotiator, ResourceMask buys, int regionIndex)
        {
            MarketConfig config = Game.SharedState.Get<MarketConfig>();
            // negotiator.PriceBlock += config.DefaultPurchasePerRegion[regionIndex].Buy & buys;
            negotiator.PriceBlock += config.DefaultMarketPurchasePerRegion[regionIndex].Buy & buys;
            negotiator.BuyMask = buys;
            // negotiator.MemoryPriceBlock = negotiator.PriceBlock;
        }
    }
}