using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Actors
{
    public class RequesterFinancialStressable : BatchedComponent
    {
        readonly public int NumTriggersPerStressTick = 1; // how many times bloom needs to affect this before it becomes a stress level
        [NonSerialized] public int TriggerCounter = 0;

        [NonSerialized] public int PurchasedStressedSinceLast = 0;
        [NonSerialized] public int MatchedSinceLast = 0; //Matched with subsidy since last
    }
}