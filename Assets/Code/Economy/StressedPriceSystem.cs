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
        private readonly RingBuffer<ResourcePriceNegotiator> m_NegotiatorWorkList = new RingBuffer<ResourcePriceNegotiator>(8, RingBufferMode.Expand);

        public override void ProcessWork(float deltaTime)
        {
            if (!m_State.MarketTimer.HasAdvanced())
            {
                return;
            }

            m_NegotiatorWorkList.Clear();
            m_State.Negotiators.CopyTo(m_NegotiatorWorkList);

            foreach(var negotiator in m_NegotiatorWorkList)
            {
                // For each negotiator, if they were trying to buy/sell
                // if they succeeded, reduce stress.
                // if they did not succeed, increase stress.
            }
        }
    }
}