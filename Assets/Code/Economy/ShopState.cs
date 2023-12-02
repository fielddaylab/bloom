using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
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

        [DebugMenuFactory]
        static private DMInfo ShopItemUnlockDebugMenu()
        {
            DMInfo info = new DMInfo("Shop");
            info.AddButton("Unlock Storage", () => {
                ShopUtility.UnlockTool(UserBuildTool.Storage);
            }, () => Game.SharedState.TryGet(out ShopState state));

            info.AddButton("Unlock Digester", () => {
                ShopUtility.UnlockTool(UserBuildTool.Digester);
            }, () => Game.SharedState.TryGet(out ShopState state));

            return info;
        }
    }
}
