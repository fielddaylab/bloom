using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.UI;

namespace Zavala.Economy
{
    [SharedStateInitOrder(10)]
    public sealed class ShopState : SharedStateComponent, IScenePreload
    {
        public UIShop ShopUI;
        public RingBuffer<int> CostQueue; // queue of new costs to add to the running tally in blueprint mode

        private int m_RunningCost = 0;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            ShopUI = FindAnyObjectByType<UIShop>(FindObjectsInactive.Include);
            CostQueue = new RingBuffer<int>(8);
            return null;
        }


        public void ResetRunningCost()
        {
            m_RunningCost = 0;
        }

        public void ModifyRunningCost(int deltaCost)
        {
            m_RunningCost += deltaCost;
        }

        public int GetRunningCost()
        {
            return m_RunningCost;
        }


        public void EnqueueCost(int cost)
        {
            Debug.Log("[Cost] Enqueueing cost: $" + cost);
            CostQueue.PushBack(cost);
        }
    }
}
