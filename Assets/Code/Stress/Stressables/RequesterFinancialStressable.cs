using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Data;

namespace Zavala.Actors
{
    public class RequesterFinancialStressable : BatchedComponent, IPersistBuildingComponent
    {
        readonly public int NumTriggersPerStressTick = 1; // how many times bloom needs to affect this before it becomes a stress level
        [NonSerialized] public int TriggerCounter = 0;

        [NonSerialized] public int PurchasedStressedSinceLast = 0;
        [NonSerialized] public int DealsFoundSinceLast = 0; //Matched with subsidy since last

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            reader.Read(ref TriggerCounter);
            reader.Read(ref PurchasedStressedSinceLast);
            reader.Read(ref DealsFoundSinceLast);
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            writer.Write(TriggerCounter);
            writer.Write(PurchasedStressedSinceLast);
            writer.Write(DealsFoundSinceLast);
        }
    }
}