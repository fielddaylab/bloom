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

    /*
    public enum OperationState {
        Low,
        Medium,
        High
    }
    */

    /// <summary>
    ///  Defines a tile that can be subject to stress 
    /// </summary>
    [DisallowMultipleComponent]
    // TODO: is this necessary?
    [RequireComponent(typeof(ResourcePurchaser))]
    public sealed class StressableActor1 : BatchedComponent {
        [NonSerialized] public Dictionary<StringHash32, Action<int>> EventResponses = new Dictionary<StringHash32, Action<int>>(); // stores stress event IDs and stress response actions
        [NonSerialized] public Action StressCapAction; // action to run when this tile reaches its stress cap
        
        [NonSerialized] public bool ResetStressOnCap = true; // set stress back to zero when cap reached?
        // TODO: temporarily hardcoded
        public int StressCap = 8;
        [NonSerialized] public int CurrentStress;
        [NonSerialized] public OperationState OperationState;
        [NonSerialized] public int TileIndex;

        private void Awake() {
            // TODO: does it make sense to calculate this first and store it? or better to calculate when needed
            TileIndex = ZavalaGame.SimGrid.HexSize.FastPosToIndex(HexVector.FromWorld(transform.position, ZavalaGame.SimWorld.WorldSpace));
            
            // TODO: hardcoded for now, make these assignable? esp. if there are stressable tiles other than City
            if (TryGetComponent(out ResourcePurchaser rp)) {
                EventResponses.Add(ResourcePurchaser.Event_PurchaseUnfulfilled, StressOnSelfEvent);
                EventResponses.Add(SimAlgaeState.Event_AlgaeGrew, StressOnAdjacentEvent);
                EventResponses.Add(ResourcePurchaser.Event_PurchaseMade, ResetStressOnSelfEvent);
                StressCapAction = () => {
                    // rp.ChangeDemand(ResourceId.Milk, -1);
                    ChangeOperationState(-1);
                };
            }
        }

        private void ChangeOperationState(int delta) {
            if (OperationState + delta <= OperationState.Great && OperationState + delta >= OperationState.Bad) {
                OperationState += delta;
            }
        }

        // TODO: there may be a more expedient data structure for these Actions?
        private void StressOnSelfEvent(int dispatcherTileIndex) {
            if (dispatcherTileIndex == TileIndex) {
                IncrementStress();
            }
        }

        private void ResetStressOnSelfEvent(int dispatcherTileIndex) {
            if (dispatcherTileIndex == TileIndex) {
                ResetStress();
                ChangeOperationState(+1);
            }
        }

        private void StressOnAdjacentEvent(int dispatcherTileIndex) {
            if (ZavalaGame.SimGrid.HexSize.FastIsNeighbor(TileIndex, dispatcherTileIndex, out var _)) {
                IncrementStress();
            }
        }

        private void IncrementStress() {
            CurrentStress++;
            DebugDraw.AddWorldText(transform.position, "Stressed to "+CurrentStress, Color.red, 3);
            Log.Msg("[StressableActor] Actor {0} stressed! Current: {1}", transform.name, CurrentStress);
        }
        private void ResetStress() {
            CurrentStress = 0;
        }

    }
}