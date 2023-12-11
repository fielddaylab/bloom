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
    public class NegotiationSystem : SharedStateSystemBehaviour<MarketData>
    {
        private readonly RingBuffer<PriceNegotiation> m_QueuedNegotiations = new RingBuffer<PriceNegotiation>(8, RingBufferMode.Expand);
        private readonly RingBuffer<ResourcePriceNegotiator> m_NegotiatorWorkList = new RingBuffer<ResourcePriceNegotiator>(8, RingBufferMode.Expand);

        private int m_tickCounter = 0;

        public override void ProcessWork(float deltaTime)
        {
            if (!m_State.MarketTimer.HasAdvanced())
            {
                return;
            }

            m_tickCounter++;

            if (m_tickCounter < MarketParams.TicksPerNegotiation)
            {
                return;
            }
            m_tickCounter = 0;

            m_QueuedNegotiations.Clear();
            m_State.NegotiationQueue.CopyTo(m_QueuedNegotiations);

            foreach (var negotiator in m_State.Negotiators)
            {
                negotiator.PriceChange.SetAll(0);
            }

            for (int i = 0; i < m_QueuedNegotiations.Count; i++)
            {
                // For each negotiator, if they were trying to buy/sell
                // if they succeeded, reduce price stress.
                // if they did not succeed, increase price stress.
                int priceStep = m_QueuedNegotiations[i].IsSeller ? -m_QueuedNegotiations[i].Negotiator.PriceStep : m_QueuedNegotiations[i].Negotiator.PriceStep;
                PriceNegotiatorUtility.StagePrice(ref m_QueuedNegotiations[i].Negotiator, m_QueuedNegotiations[i].ResourceType, priceStep);
            }

            for (int i = 0; i < m_QueuedNegotiations.Count; i++)
            {
                PriceNegotiatorUtility.FinalizePrice(ref m_QueuedNegotiations[i].Negotiator, m_QueuedNegotiations[i].ResourceType);
            }
        }
    }
}