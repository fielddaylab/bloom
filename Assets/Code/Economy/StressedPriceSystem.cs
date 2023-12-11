using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Actors;

namespace Zavala.Economy
{
    [SysUpdate(GameLoopPhase.Update, 20)] // After MarketSystem
    public class StressedPriceSystem : SharedStateSystemBehaviour<MarketData>
    {
        private readonly RingBuffer<PriceNegotiation> m_QueuedNegotiations = new RingBuffer<PriceNegotiation>(8, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime)
        {
            if (!m_State.MarketTimer.HasAdvanced())
            {
                return;
            }

            //m_QueuedPriceMods.Clear();
            //m_State.Negotiators.CopyTo(m_NegotiatorWorkList);
            
            //m_MemorizeWorkList.Clear();
            //m_State.MemorizeQueue.CopyTo(m_MemorizeWorkList);

            foreach (var neg in m_QueuedNegotiations)
            {
                // For each negotiator, if they were trying to buy/sell
                // if they succeeded, reduce stress.
                // if they did not succeed, increase stress.
                // PriceNegotiatorUtility.AdjustPrice(neg);
            }

            //foreach (var negotiator in m_MemorizeWorkList)
            //{
                // For each negotiator, memorize the last price product bought/sold at
            //}
        }
    }
}