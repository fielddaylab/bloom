using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zavala.Economy;
using Zavala.World;

namespace Zavala.Sim
{
    public enum UnlockConditionType {
        AvgPhosphorusRunoff,
        TotalPhosphorusRunoff,
        MarketShareTargets,
        RevenueTargets,
        AccrueWealth,
        WaterHealth
    }

    [Serializable]
    public struct UnlockConditionGroup {
        [Header("Common")]
        public UnlockConditionType Type; // type of unlock condition
        public RegionId[] ChecksRegions; // the regions whose history this condition will be checked against
        public int Scope; // Specifies the scope (e.g. "depth" of avg phosphorus)

        // NOTE: Only need to set conditions below under the header corresponding to this UnlockConditionType

        [Space(5)]
        [Header("Runoff")]
        public TargetThreshold TargetRunoff; // anything lower than threshold will satisfy condition

        [Space(5)]
        [Header("MarketShareTargets")]
        public TargetThreshold TargetCFertilizer;
        public TargetThreshold TargetManure;
        public TargetThreshold TargetDFertilizer;

        [Space(5)]
        [Header("RevenueTargets")]
        public TargetThreshold TargetSalesRevenue;
        public TargetThreshold TargetImportRevenue;
        public TargetThreshold TargetPenaltyRevenue;

        [Space(5)]
        [Header("AccrueWealth")]
        public TargetThreshold TargetWealth; // Budget needed to satisfy condition

        /* TODO
        [Space(5)]
        [Header("WaterHealth")]
        */
    }

    [Serializable]
    public struct UnlockGroup
    {
        public List<UnlockConditionGroup> UnlockConditions;
        public RegionId[] RegionIndexUnlocks; // TODO: maybe make this enum region id?
    }

    public class RegionUnlockState : SharedStateComponent
    {
        public List<UnlockGroup> UnlockGroups;
        [NonSerialized] public int UnlockCount;
    }

    static public class RegionUnlockUtility {
        static public void UnlockRegion(SimGridState grid, int region, SimWorldState worldState) {
            if (region >= grid.WorldData.Regions.Length) {
                // region to unlock not registered
                return;
            }

            SimDataUtility.LoadAndRegenRegionDataFromWorld(grid, grid.WorldData, region, worldState);
            // TODO: trigger RegionUnlocked for scripting purposes
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("regionId", ((RegionId) region).ToString());
                ScriptUtility.Trigger(GameTriggers.RegionUnlocked, varTable);
            }
            Debug.Log("[RegionUnlockSystem] Unlocked region " + region);
        }

        [DebugMenuFactory]
        static private DMInfo RegionUnlockDebugMenu() {
            DMInfo info = new DMInfo("Regions");
            info.AddButton("Unlock Next Region", () => {
                var r = Game.SharedState.Get<RegionUnlockState>();
                var w = Game.SharedState.Get<SimWorldState>();
                var data = r.UnlockGroups[r.UnlockCount++];
                foreach(int region in data.RegionIndexUnlocks) {
                    UnlockRegion(ZavalaGame.SimGrid, region, w);
                }
            }, () => Game.SharedState.TryGet(out RegionUnlockState r) && r.UnlockCount < r.UnlockGroups.Count);
            return info;
        }
    }
}
