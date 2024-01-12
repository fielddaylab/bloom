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
        Bad,
        Okay,
        Great
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

        [SerializeField] private int m_BadThreshold = 8;        // >=
        [SerializeField] private int m_OkayThreshold = 4;     // >=
        [SerializeField] private int m_GreatThreshold = 0;       // >=

        [SerializeField] private OperationState m_StartingState = OperationState.Great;


        #endregion // Inspector

        [NonSerialized] public Dictionary<StressCategory, int> CurrentStress;
        [NonSerialized] public Dictionary<OperationState, int> OperationThresholds;
        [NonSerialized] public int TotalStress;
        [NonSerialized] public float AvgStress;
        [NonSerialized] public OperationState OperationState;
        [NonSerialized] public bool ChangedOperationThisTick = false;
        [NonSerialized] public bool StressImproving = false; // this would be more robust as a PrevStress dictionary
        [NonSerialized] public OperationState PrevState;

        [NonSerialized] public Dictionary<StressCategory, bool> StressMask;
        [NonSerialized] public int StressCount;

        public int StressDelta = 1; // value jumps between operation states

        [NonSerialized] public OccupiesTile Position;

        public void OnRegister()
        {
            int startingStress = 0;
            if (m_StartingState == OperationState.Okay)
            {
                startingStress = m_OkayThreshold;
            }
            else if (m_StartingState == OperationState.Bad)
            {
                startingStress = m_GreatThreshold;
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
                { OperationState.Bad, m_BadThreshold },
                { OperationState.Okay, m_OkayThreshold },
                { OperationState.Great, m_GreatThreshold },
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
            actor.StressImproving = false;

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
            actor.StressImproving = true;

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
    }
}