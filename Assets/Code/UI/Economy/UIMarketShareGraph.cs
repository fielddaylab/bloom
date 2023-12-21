using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Data;
using Zavala.Economy;

namespace Zavala.UI
{
    public class UIMarketShareGraph : BatchedComponent
    {
        public MarketShareGraphRenderer Renderer;

        protected override void OnEnable() {
            base.OnEnable();

            Game.Events?.Register(GameEvents.MarketCycleTickCompleted, OnMarketTickCompleted);
            GameLoop.QueueAfterLateUpdate(OnMarketTickCompleted);
        }

        protected override void OnDisable() {
            Game.Events?.Deregister(GameEvents.MarketCycleTickCompleted, OnMarketTickCompleted);

            base.OnDisable();
        }

        private void OnMarketTickCompleted() {
            MarketData data = Game.SharedState.Get<MarketData>();
            PopulateFromData(data, (int) ZavalaGame.SimGrid.RegionCount);
        }

        public unsafe void PopulateFromData(MarketData data, int regionCount) {
            // tick count
            int totalTickCount = data.CFertilizerSaleHistory[0].Net.Count;

            if (totalTickCount < 2) {
                Renderer.Clear();
                return;
            }

            float* cFertBuffer = stackalloc float[totalTickCount];
            float* manureBuffer = stackalloc float[totalTickCount];
            float* dFertBuffer = stackalloc float[totalTickCount];

            Unsafe.Clear(cFertBuffer, totalTickCount);
            Unsafe.Clear(manureBuffer, totalTickCount);
            Unsafe.Clear(dFertBuffer, totalTickCount);

            for (int i = 0; i < regionCount; i++) {
                DataHistory cFert = data.CFertilizerSaleHistory[i];
                DataHistory manure = data.ManureSaleHistory[i];
                DataHistory dFert = data.DFertilizerSaleHistory[i];

                int ticksForThisRegion = Math.Min(totalTickCount, cFert.Net.Count);
                for (int j = 0; j < ticksForThisRegion; j++) {
                    cFertBuffer[j] += cFert.Net[j];
                    manureBuffer[j] += manure.Net[j];
                    dFertBuffer[j] += dFert.Net[j];
                }
            }

            Renderer.Populate(cFertBuffer, manureBuffer, dFertBuffer, totalTickCount);
        }
    }
}
