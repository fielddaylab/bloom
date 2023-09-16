using FieldDay.Components;

namespace Zavala.Actors {
    public sealed class ActorPhosphorusGenerator : BatchedComponent {
        public int Amount; // base amount that should be produced per unit of a given unit
        public int AmountProducedLastTick; // total amount produced last time generated (whether actor timer or resource producer on produce)
    }
}