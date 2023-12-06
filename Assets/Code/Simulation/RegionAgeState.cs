using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;

namespace Zavala.Sim {
    public class RegionAgeState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public bool SimPhosphorusAdvanced;
        [NonSerialized] public Dictionary<RegionId, int> AgeTriggers;

        public void OnRegister() {
            AgeTriggers = new Dictionary<RegionId, int>(3);
        }

        public void OnDeregister() {
        }

    }

     static public class RegionAgeUtility
    {
        static public void RegisterPTimerAdvanced(RegionAgeState ageState) {
            ageState.SimPhosphorusAdvanced = true;
        }

        [LeafMember("AddRegionAgeTrigger")]
        static public void AddRegionAgeTrigger(int region, int age) {
            ZavalaGame.SharedState.Get<RegionAgeState>().AgeTriggers.Add((RegionId)region-1, age); // 1-indexed to 0-indexed
        }

        [LeafMember("AddRegionAgeDeltaTrigger")]
        static public void AddRegionAgeDeltaTrigger(int region, int delay) {
            region--; // 1-indexed to 0-indexed
            int finalAge = ZavalaGame.SharedState.Get<SimGridState>().Regions[region].Age + delay;
            ZavalaGame.SharedState.Get<RegionAgeState>().AgeTriggers.Add((RegionId)region, finalAge); 
        }
    }
}