using FieldDay.Components;
using UnityEngine;

namespace Zavala.Actors {
    public sealed class ActorPhosphorusGenerator : BatchedComponent {
        public int Amount; // base amount that should be produced per unit of a given resource
        public int AmountProducedLastTick; // total amount produced last time generated (whether actor timer or resource producer on produce)
        public Transform RunoffParticleOrigin = null;
        public bool ConsumeFertilizerForRunoff; // false for grain farms, because it is already consumed in production
        public bool SoldManureRecently = false; // set to true if selling manure/otherwise dealing with runoff
    }
}