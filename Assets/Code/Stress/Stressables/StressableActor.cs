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

namespace Zavala.Actors {

    public enum OperationState {
        Low,
        Medium,
        High
    }

    public enum StressCategory
    {
        Bloom,
        Resource,
        Financial
    }

    /// <summary>
    ///  Defines a tile that can be subject to stress 
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class StressableActor : BatchedComponent, IRegistrationCallbacks {
        #region Inspector

        public int StressCap = 8;

        [SerializeField] private int m_LowThreshold = 8;        // >=
        [SerializeField] private int m_MediumThreshold = 4;     // >=
        [SerializeField] private int m_HighThreshold = 0;       // >=

        #endregion // Inspector

        [NonSerialized] public Dictionary<StressCategory, int> CurrentStress;
        [NonSerialized] public Dictionary<OperationState, int> OperationThresholds;
        [NonSerialized] public int TotalStress;
        [NonSerialized] public OperationState OperationState;
        [NonSerialized] public bool ChangedOperationThisTick;
        [NonSerialized] public OperationState PrevState;

        public int StressDelta = 1; // value jumps between operation states

        [NonSerialized] public OccupiesTile Position;

        public void OnRegister()
        {
            CurrentStress = new Dictionary<StressCategory, int>() {
                { StressCategory.Bloom, 0 },
                { StressCategory.Resource, 0 },
                { StressCategory.Financial, 0 },
            };

            OperationThresholds = new Dictionary<OperationState, int>() {
                { OperationState.Low, m_LowThreshold },
                { OperationState.Medium, m_MediumThreshold },
                { OperationState.High, m_HighThreshold },
            };

            Position = this.GetComponent<OccupiesTile>();
        }

        public void OnDeregister()
        {
        }
    }

    static public class StressUtility { 
        static public void IncrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]++;
            RecalculateTotalStress(actor);

            if (actor.TotalStress > actor.StressCap)
            {
                // hit upper bound; undo
                actor.CurrentStress[category]--;
                RecalculateTotalStress(actor);
            }
            else
            {
                // apply changes
                UpdateOperationState(actor);

                DebugDraw.AddWorldText(actor.transform.position, "Stressed to " + actor.CurrentStress[category], Color.red, 3);
                Log.Msg("[StressableActor] Actor {0} stressed! Current: {1}", actor.transform.name, actor.CurrentStress[category]);
            }
        }

        static public void DecrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]--;
            RecalculateTotalStress(actor);

            if (actor.CurrentStress[category] < 0)
            {
                // hit lower bound
                actor.CurrentStress[category]++;
                RecalculateTotalStress(actor);
            }
            else
            {
                // apply changes
                UpdateOperationState(actor);

                DebugDraw.AddWorldText(actor.transform.position, "Unstressed to " + actor.CurrentStress[category], Color.blue, 3);
                Log.Msg("[StressableActor] Actor {0} unstressed! Current: {1}", actor.transform.name, actor.CurrentStress[category]);
            }

        }

        static public void ResetStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category] = 0;
            RecalculateTotalStress(actor);
        }

        static private void RecalculateTotalStress(StressableActor actor)
        {
            actor.TotalStress = actor.CurrentStress[StressCategory.Bloom]
                + actor.CurrentStress[StressCategory.Resource]
                + actor.CurrentStress[StressCategory.Financial];
        }

        static private void UpdateOperationState(StressableActor actor)
        {
            if (actor.TotalStress >= actor.OperationThresholds[OperationState.Low])
            {
                if (actor.OperationState != OperationState.Low)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Low;
                    actor.ChangedOperationThisTick = true;
                }
            }
            else if (actor.TotalStress >= actor.OperationThresholds[OperationState.Medium])
            {
                if (actor.OperationState != OperationState.Medium)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Medium;
                    actor.ChangedOperationThisTick = true;
                }
            }
            else // if (actor.TotalStress >= actor.OperationThresholds[OperationState.High])
            {
                if (actor.OperationState != OperationState.High)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.High;
                    actor.ChangedOperationThisTick = true;
                }
            }
        }
    }
}