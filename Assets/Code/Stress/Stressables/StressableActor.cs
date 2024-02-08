using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.Economy;
using BeauUtil.Debugger;
using Zavala.Sim;
using FieldDay.Debugging;
using Zavala.Building;
using Zavala.Data;

namespace Zavala.Actors {

    public enum OperationState : byte {
        Bad,
        Okay,
        Great
    }

    public enum StressCategory : byte
    {
        Bloom,
        Resource,
        Financial,

        [Hidden]
        COUNT
    }

    /// <summary>
    ///  Defines a tile that can be subject to stress 
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class StressableActor : BatchedComponent, IRegistrationCallbacks, IPersistBuildingComponent {
        #region Inspector

        public int StressCap = 8;

        public int BadThreshold = 8;        // >=
        public int OkayThreshold = 4;     // >=
        public int GreatThreshold = 0;       // >=

        [SerializeField] private OperationState m_StartingState = OperationState.Great;


        #endregion // Inspector

        [NonSerialized] public EMap<StressCategory, int> CurrentStress; // TODO: Replace with array
        [NonSerialized] public BitSet32 StressImproving; // TODO: Replace with bitmask
        [NonSerialized] public EMap<OperationState, int> OperationThresholds;
        [NonSerialized] public int TotalStress;
        [NonSerialized] public float AvgStress;
        [NonSerialized] public OperationState OperationState;
        [NonSerialized] public bool ChangedOperationThisTick = false;
        [NonSerialized] public OperationState PrevState;

        [NonSerialized] public BitSet32 StressMask; // TODO: Replace with bitmask
        [NonSerialized] public int StressCount;

        public int StressDelta = 1; // value jumps between operation states

        [NonSerialized] public OccupiesTile Position;

        public void OnRegister()
        {
            int startingStress = 0;
            if (m_StartingState == OperationState.Okay)
            {
                startingStress = OkayThreshold;
            }
            else if (m_StartingState == OperationState.Bad)
            {
                startingStress = GreatThreshold;
            }
            OperationState = m_StartingState;
            PrevState = m_StartingState;


            int numStressCategories = 0;
            bool bloomStress = this.GetComponent<BloomStressable>();
            bool resourceStress = this.GetComponent<ResourceStressable>();
            bool financialStress = this.GetComponent<RequesterFinancialStressable>() || this.GetComponent<SupplierFinancialStressable>();

            if (bloomStress) { numStressCategories++; }
            if (resourceStress) { numStressCategories++; }
            if (financialStress) { numStressCategories++; }
            StressCount = numStressCategories;

            CurrentStress = new EMap<StressCategory, int>(3);
            CurrentStress[0] = bloomStress ? startingStress : 0;
            CurrentStress[1] = resourceStress ? startingStress : 0;
            CurrentStress[2] = financialStress ? startingStress : 0;

            StressImproving = new BitSet32();

            StressMask = new BitSet32();
            StressMask[(int) StressCategory.Bloom] = bloomStress;
            StressMask[(int) StressCategory.Resource] = resourceStress;
            StressMask[(int) StressCategory.Financial] = financialStress;

            OperationThresholds = new EMap<OperationState, int>(3);
            OperationThresholds[0] = BadThreshold;
            OperationThresholds[1] = OkayThreshold;
            OperationThresholds[2] = GreatThreshold;

            Position = this.GetComponent<OccupiesTile>();

            StressUtility.RecalculateTotalStress(this);
            StressUtility.UpdateOperationState(this);
        }

        public void OnDeregister()
        {
        }

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            for(int i = 0; i < 3; i++) {
                writer.Write(CurrentStress[i]);
            }
            writer.Write(TotalStress);
            writer.Write(AvgStress);
            writer.Write(OperationState);
            writer.Write(PrevState);

            uint mask;
            StressImproving.Unpack(out mask);
            writer.Write((byte) mask);
            StressMask.Unpack(out mask);
            writer.Write((byte) mask);
        }

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            for(int i = 0; i < 3; i++) {
                reader.Read(ref CurrentStress[i]);
            }
            reader.Read(ref TotalStress);
            reader.Read(ref AvgStress);
            reader.Read(ref OperationState);
            reader.Read(ref PrevState);

            StressImproving = new BitSet32(reader.Read<byte>());
            StressMask = new BitSet32(reader.Read<byte>());
        }
    }

    static public class StressUtility { 
        static public void IncrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]++;
            //RecalculateTotalStress(actor);
            actor.StressImproving[(int)category] = false;

            if (actor.CurrentStress[category] > actor.StressCap)
            {
                // hit upper bound; undo
                actor.CurrentStress[category]--;
                //RecalculateTotalStress(actor);
            }
            else
            {
                // apply changes
                RecalculateTotalStress(actor);
                UpdateOperationState(actor);
                DebugDraw.AddWorldText(actor.transform.position + Vector3.up, "Stressed to " + actor.CurrentStress[category], Color.red, 3);
                // Log.Msg("[StressableActor] Actor {0} increased {1} stress! Current: {2}", actor.transform.name, category, actor.CurrentStress[category]);
            }
        }

        static public void DecrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]--;
            // RecalculateTotalStress(actor);
            actor.StressImproving[(int)category] = true;

            if (actor.CurrentStress[category] < 0)
            {
                // hit lower bound
                actor.CurrentStress[category]++;
                // RecalculateTotalStress(actor);
            }
            else
            {
                // apply changes
                RecalculateTotalStress(actor);
                UpdateOperationState(actor);

                DebugDraw.AddWorldText(actor.transform.position + Vector3.up, "Unstressed to " + actor.CurrentStress[category], Color.blue, 3);
                // Log.Msg("[StressableActor] Actor {0} decreased {1} stress! Current: {2}", actor.transform.name, category, actor.CurrentStress[category]);
            }
        }

        static public void ResetStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category] = 0;
            RecalculateTotalStress(actor);
        }

        static public void RecalculateTotalStress(StressableActor actor)
        {
            actor.TotalStress = actor.CurrentStress[StressCategory.Bloom]
                + actor.CurrentStress[StressCategory.Resource]
                + actor.CurrentStress[StressCategory.Financial];
            if (actor.StressCount == 0) {
                actor.AvgStress = 0;
                return;
            }
            actor.AvgStress = actor.TotalStress / actor.StressCount;
        }

        static public void UpdateOperationState(StressableActor actor)
        {
            actor.ChangedOperationThisTick = false;

            if (actor.AvgStress >= actor.OperationThresholds[OperationState.Bad])
            {
                if (actor.OperationState != OperationState.Bad)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Bad;
                    actor.ChangedOperationThisTick = true;
                    return;
                }
            }
            else if (actor.AvgStress >= actor.OperationThresholds[OperationState.Okay])
            {
                if (actor.OperationState != OperationState.Okay)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Okay;
                    actor.ChangedOperationThisTick = true;
                    return;
                }
            }
            else // if (actor.TotalStress >= actor.OperationThresholds[OperationState.High])
            {
                if (actor.OperationState != OperationState.Great)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Great;
                    actor.ChangedOperationThisTick = true;
                    return;
                }
            }
            actor.PrevState = actor.OperationState;
        }

        static public bool IsPeakStressed(StressableActor actor, StressCategory stressType)
        {
            return actor.CurrentStress[stressType] >= actor.BadThreshold;
        }
    }
}