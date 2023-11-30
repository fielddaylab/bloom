using BeauUtil;
using FieldDay;
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

        public int RunningCost = 0;

        public bool ManualUpdateRequested;

        public IEnumerator<WorkSlicer.Result?> Preload() {
            ShopUI = Game.Gui.GetShared<UIShop>();
            CostQueue = new RingBuffer<int>(8, RingBufferMode.Expand);
            return null;
        }
    }
}
