using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Sim
{
    public enum UnlockConditionType {
        AvgPhosphorus,
        TotalPhosphorus
    }

    [Serializable]
    public struct UnlockConditionGroup {
        public UnlockConditionType Type; // type of unlock condition
        public int Scope; // Specifies the scope (e.g. "depth" of avg phosphorus)
        public float Threshold; // anything lower than threshold will satisfy condition
        public RegionId[] ChecksRegions; // the regions whose history this condition will be checked against
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
