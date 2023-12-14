using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using Leaf.Runtime;
using System;
using UnityEngine;

namespace Zavala.Sim {
    public sealed class SimAlgaeState : SharedStateComponent, IRegistrationCallbacks {
        static public readonly StringHash32 Event_AlgaeFormed = "AlgaeState::AlgaeFormed";
        static public readonly StringHash32 Event_AlgaeGrew = "AlgaeState::AlgaeGrew";
        static public readonly StringHash32 Event_AlgaePeaked = "AlgaeState::AlgaePeaked";

        [NonSerialized] public int CurrentMinPForAlgaeGrowth;
        [NonSerialized] public AlgaeBuffers Algae;
        // public GameObject AlgaePrefab;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault;
            SimGridState gridState = ZavalaGame.SimGrid;
            Algae.Create(gridState.HexSize);
        }
    }
    public class SimAlgaeUtility {
        static public float RemoveAlgae(SimAlgaeState algaeState, int tileIndex, float amount) {
            Assert.True(amount >= 0);
            ref float percentAlgae = ref algaeState.Algae.State[tileIndex].PercentAlgae;
            amount = Math.Min(amount, percentAlgae);
            percentAlgae -= amount;

            return amount;
        }

        [LeafMember("SetAlgaeGrowthThreshold")]
        public static void SetAlgaeGrowthThreshold(int minPForAlgae) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = minPForAlgae;
        }

        [LeafMember("OffsetAlgaeGrowthThreshold")]
        public static void OffsetAlgaeGrowthThreshold(int delta) {
            Game.SharedState.Get<SimAlgaeState>().CurrentMinPForAlgaeGrowth = AlgaeSim.MinPForAlgaeGrowthDefault + delta;
        }

    }
}