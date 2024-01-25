using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Data;

namespace Zavala.Actors
{
    public class SupplierFinancialStressable : BatchedComponent, IPersistBuildingComponent
    {
        readonly public int NumTriggersPerStressTick = 1; // how many times bloom needs to affect this before it becomes a stress level
        [NonSerialized] public int TriggerCounter = 0;

        [NonSerialized] public int SoldAtLossSinceLast = 0;
        [NonSerialized] public int NonMilkSoldSinceLast = 0;

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            reader.Read(ref TriggerCounter);
            reader.Read(ref SoldAtLossSinceLast);
            reader.Read(ref NonMilkSoldSinceLast);
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            writer.Write(TriggerCounter);
            writer.Write(SoldAtLossSinceLast);
            writer.Write(NonMilkSoldSinceLast);
        }
    }
}