using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Data;
using Zavala.Economy;
using Zavala.World;

namespace Zavala.Sim {
    public enum UnlockConditionType {
        AvgPhosphorusRunoff,
        TotalPhosphorusRunoff,
        MarketShareTargets,
        RevenueTargets,
        AccrueWealth,
        WaterHealth,
        RegionAge,
        NodeReached
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

        [Space(5)]
        [Header("RegionAge")]
        public TargetThreshold TargetAge;

        [Space(5)]
        [Header("NodeReached")]
        // string for serialized input, hash for storing the actual lookup
        public string NodeTitle;
        [NonSerialized]
        public StringHash32 NodeHash;

        /* TODO
        [Space(5)]
        [Header("WaterHealth")]
        */
    }

    [Serializable]
    public struct UnlockGroup
    {
        public int UnlockDelay;
        public List<UnlockConditionGroup> UnlockConditions;
        public RegionId[] RegionIndexUnlocks;
    }

    public class RegionUnlockState : SharedStateComponent, ISaveStateChunkObject, IRegistrationCallbacks
    {
        public List<UnlockGroup> UnlockGroups;
        [NonSerialized] public int UnlockCount;

        [NonSerialized] public bool SimPhosphorusAdvanced;

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("RegionUnlock");
        }

        public void OnRegister() {
            ZavalaGame.SaveBuffer.RegisterHandler("RegionUnlock", this);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref byte* data, ref int remaining, SaveStateChunkConsts consts) {
            UnlockCount = Unsafe.Read<byte>(ref data, ref remaining);
            SimPhosphorusAdvanced = Unsafe.Read<bool>(ref data, ref remaining);
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref byte* data, ref int written, int capacity, SaveStateChunkConsts consts) {
            Unsafe.Write((byte) UnlockCount, ref data, ref written, capacity);
            Unsafe.Write(SimPhosphorusAdvanced, ref data, ref written, capacity);
        }
    }

    static public class RegionUnlockUtility {
        static public void UnlockRegion(SimGridState grid, int region, SimWorldState worldState) {
            if (region >= grid.WorldData.Regions.Length) {
                // region to unlock not registered
                return;
            }

            SimDataUtility.LoadAndRegenRegionDataFromWorld(grid, grid.WorldData, region, worldState);
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                varTable.Set("regionId", ((RegionId) region).ToString());
                ScriptUtility.Trigger(GameTriggers.RegionUnlocked, varTable);
            }
            Debug.Log("[RegionUnlockSystem] Unlocked region " + region);
        }

        static public void RegisterPTimerAdvanced(RegionUnlockState unlockState) {
            unlockState.SimPhosphorusAdvanced = true;
        }

        static public void DecrementTimer(UnlockGroup group) {
            group.UnlockDelay -= 1;
        }

        [LeafMember("RegionUnlocked")]
        public static bool RegionUnlocked(int regionIndex) {
            return ZavalaGame.SharedState.Get<RegionUnlockState>().UnlockCount >= regionIndex;
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
