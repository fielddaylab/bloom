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

        [SerializeField] private OperationState m_StartingState = OperationState.High;


        #endregion // Inspector

        [NonSerialized] public Dictionary<StressCategory, int> CurrentStress;
        [NonSerialized] public Dictionary<OperationState, int> OperationThresholds;
        [NonSerialized] public int TotalStress;
        [NonSerialized] public float AvgStress;
        [NonSerialized] public OperationState OperationState;
        [NonSerialized] public bool ChangedOperationThisTick;
        [NonSerialized] public OperationState PrevState;

        [NonSerialized] public Dictionary<StressCategory, bool> StressMask;
        [NonSerialized] public int StressCount;

        public int StressDelta = 1; // value jumps between operation states

        [NonSerialized] public OccupiesTile Position;

        public void OnRegister()
        {
            int startingStress = 0;
            if (m_StartingState == OperationState.Medium)
            {
                startingStress = m_MediumThreshold;
            }
            else if (m_StartingState == OperationState.Low)
            {
                startingStress = m_HighThreshold;
            }


            int numStressCategories = 0;
            bool bloomStress = this.GetComponent<BloomStressable>();
            bool resourceStress = this.GetComponent<ResourceStressable>();
            bool financialStress = this.GetComponent<RequesterFinancialStressable>() || this.GetComponent<SupplierFinancialStressable>();

            if (bloomStress) { numStressCategories++; }
            if (resourceStress) { numStressCategories++; }
            if (financialStress) { numStressCategories++; }
            StressCount = numStressCategories;

            CurrentStress = new Dictionary<StressCategory, int>() {
                { StressCategory.Bloom, bloomStress ? startingStress : 0 },
                { StressCategory.Resource, resourceStress ? startingStress : 0 },
                { StressCategory.Financial, financialStress ? startingStress : 0 },
            };

            StressMask = new Dictionary<StressCategory, bool>() {
                { StressCategory.Bloom, bloomStress },
                { StressCategory.Resource, resourceStress },
                { StressCategory.Financial, financialStress },
            };

            OperationThresholds = new Dictionary<OperationState, int>() {
                { OperationState.Low, m_LowThreshold },
                { OperationState.Medium, m_MediumThreshold },
                { OperationState.High, m_HighThreshold },
            };

            Position = this.GetComponent<OccupiesTile>();

            StressUtility.RecalculateTotalStress(this);
            StressUtility.UpdateOperationState(this);
        }

        public void OnDeregister()
        {
        }
    }

    static public class StressUtility { 
        static public void IncrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]++;
            //RecalculateTotalStress(actor);

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
                Log.Msg("[StressableActor] Actor {0} increased {1} stress! Current: {2}", actor.transform.name, category, actor.CurrentStress[category]);
            }
        }

        static public void DecrementStress(StressableActor actor, StressCategory category)
        {
            actor.CurrentStress[category]--;
            // RecalculateTotalStress(actor);

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
                Log.Msg("[StressableActor] Actor {0} decreased {1} stress! Current: {2}", actor.transform.name, category, actor.CurrentStress[category]);
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
            if (actor.AvgStress >= actor.OperationThresholds[OperationState.Low])
            {
                if (actor.OperationState != OperationState.Low)
                {
                    actor.PrevState = actor.OperationState;
                    actor.OperationState = OperationState.Low;
                    actor.ChangedOperationThisTick = true;
                }
            }
            else if (actor.AvgStress >= actor.OperationThresholds[OperationState.Medium])
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