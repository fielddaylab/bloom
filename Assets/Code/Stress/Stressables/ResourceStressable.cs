using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Actors
{
    public class ResourceStressable : BatchedComponent
    {
        readonly public int NumTriggersPerStressTick = 1; // how many times financial hardships need to affect this before it becomes a stress level
        public int TriggerCounter = 0;
    }
}