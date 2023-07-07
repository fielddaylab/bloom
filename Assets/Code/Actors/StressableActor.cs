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
    /// <summary>
    ///  Defines a tile that can be subject to stress 
    /// </summary>
    [DisallowMultipleComponent]
    // TODO: is this necessary?
    [RequireComponent(typeof(ResourcePurchaser))]
    public sealed class StressableActor : BatchedComponent {
        //private ResourcePurchaser rp;

        [NonSerialized] public Dictionary<StringHash32, Action> EventResponses = new Dictionary<StringHash32, Action>(); // stores stress event IDs and stress response actions
        [NonSerialized] public Action StressCapAction; // action to run when this tile reaches its stress cap
        
        public bool ResetStressOnCap; // set stress back to zero when cap reached?
        // TODO: temporarily hardcoded
        public int StressCap = 8;
        [NonSerialized] public int CurrentStress;
        // [NonSerialized] public int TileIndex;

        private void Awake() {
            // does it make sense to calculate this first and store it? or better to calculate when needed
            // TileIndex = ZavalaGame.SimGrid.HexSize.FastPosToIndex(HexVector.FromWorld(transform.position, ZavalaGame.SimWorld.WorldSpace));
            // TODO: hardcoded for now, make these assignable? might need an enum for event types or something?
            if (TryGetComponent(out ResourcePurchaser rp)) {
                EventResponses.Add(ResourcePurchaser.Event_PurchaseUnfulfilled, IncrementStress);
                // EventResponses.Add(SimAlgaeState.Event_AlgaeGrew, IncrementStress);
                EventResponses.Add(ResourcePurchaser.Event_PurchaseMade, ResetStress);
                StressCapAction = () => {
                    rp.ChangeDemand(ResourceId.Milk, -1);
                };
            }
        }

        private void IncrementStress() {
            CurrentStress++;
            DebugDraw.AddWorldText(transform.position, ":(", Color.red, 3);
            Log.Msg("[StressableActor] Actor {0} stressed :(", transform.name);
        }
        private void ResetStress() {
            CurrentStress = 0;
        }

    }
}