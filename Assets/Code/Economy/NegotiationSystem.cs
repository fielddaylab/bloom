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
        private readonly RingBuffer<ResourcePriceNegotiator> m_NegotiatorsWorkList = new RingBuffer<ResourcePriceNegotiator>(8, RingBufferMode.Expand);

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

            int marketIndex;
            // For each negotation forcing a price change, stage it
            for (int i = 0; i < m_QueuedNegotiations.Count; i++)
            {
                // if they succeeded in buying/selling, reduce price stress.
                // if they did not succeed, increase price stress.
                int priceStep = m_QueuedNegotiations[i].IsIncrease ? m_QueuedNegotiations[i].Negotiator.PriceStep : -m_QueuedNegotiations[i].Negotiator.PriceStep;
                PriceNegotiatorUtility.StagePrice(ref m_QueuedNegotiations[i].Negotiator, m_QueuedNegotiations[i].ResourceType, priceStep);
            }

            // finalize the negotiated price changes
            for (int i = 0; i < m_QueuedNegotiations.Count; i++)
            {
                PriceNegotiatorUtility.FinalizePrice(ref m_QueuedNegotiations[i].Negotiator, m_QueuedNegotiations[i].ResourceType);
            }

            // For each negotiator, if they sucessfully traded, try for a better deal next time 
            m_NegotiatorsWorkList.Clear();
            m_State.Negotiators.CopyTo(m_NegotiatorsWorkList);

            foreach (var negotiator in m_NegotiatorsWorkList)
            {
                PriceNegotiatorUtility.ImprovePrices(negotiator);
                negotiator.SettledRecord.SetAll(0);
            }

            m_NegotiatorsWorkList.CopyTo(m_State.Negotiators);
            m_NegotiatorsWorkList.Clear();
        }
    }
}