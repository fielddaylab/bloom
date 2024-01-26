using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Data;

namespace Zavala.Actors
{
    public class BloomStressable : BatchedComponent, IPersistBuildingComponent
    {
        readonly public int NumTriggersPerStressTick = 1; // how many times bloom needs to affect this before it becomes a stress level
        public int TriggerCounter = 0;

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            reader.Read(ref TriggerCounter);
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            writer.Write(TriggerCounter);
        }
    }
}