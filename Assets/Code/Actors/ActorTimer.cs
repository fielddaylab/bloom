using BeauUtil;
using FieldDay.Components;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Actors {
    [DisallowMultipleComponent]
    public sealed class ActorTimer : BatchedComponent {
        public SimTimer Timer;

        public bool HasAdvanced() {
            return Timer.HasAdvanced();
        }

        protected override void OnDisable() {
            Timer.Accumulator = 0;

            base.OnDisable();
        }
    }
}