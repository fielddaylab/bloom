using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Economy;

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
    }
}
