using FieldDay.Systems;
using Zavala.Sim;

namespace Zavala.Actors {
    public sealed class ActorTimer : BatchedComponent {
        public SimTimer Timer;

        public bool HasAdvanced() {
            return Timer.HasAdvanced();
        }
    }
}