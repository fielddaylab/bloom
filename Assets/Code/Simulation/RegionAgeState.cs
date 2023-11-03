using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Sim
{
    public class RegionAgeState : SharedStateComponent
    {
        [NonSerialized] public bool SimPhosphorusAdvanced;
    }

    static public class RegionAgeUtility
    {
        static public void RegisterPTimerAdvanced(RegionAgeState ageState) {
            ageState.SimPhosphorusAdvanced = true;
        }
    }
}