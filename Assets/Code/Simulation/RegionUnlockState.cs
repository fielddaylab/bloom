using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Sim
{
    [Serializable]
    public struct UnlockGroup
    {
        public int[] RegionIndexUnlocks; // TODO: maybe make this enum region id?
    }

    public class RegionUnlockState : SharedStateComponent
    {
        public List<UnlockGroup> UnlockGroups;
    }
}
