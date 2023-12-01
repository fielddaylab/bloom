using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Data;
using Zavala.Economy;

namespace Zavala.UI
{
    public class UIMarketShareGraph : BatchedComponent
    {
        public int MaxHistoryCount = 20;
        public MarketShareGraphRenderer Renderer;

        public void PopulateFromData(MarketData data, int regionCount) {
            
        }
    }
}
