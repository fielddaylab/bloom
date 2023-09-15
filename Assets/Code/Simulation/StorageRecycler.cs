using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Actors;
using Zavala.Economy;

namespace Zavala.Sim {
    /// <summary>
    /// Returns Storage from one component to another.
    /// In the case of dairy farms, used by the Let It Sit option to return the manure left to sit back to the dairy farm
    /// </summary>
    [RequireComponent(typeof(ResourceStorage))]
    public class StorageRecycler : BatchedComponent, IRegistrationCallbacks
    {
        public ResourceStorage ReturnTo; // The storage this storage gets returned to
        [SerializeField] private ActorTimer m_Timer; // This recycler's timer
        [SerializeField] private ActorTimer m_ReturnToTimer; // The timer of the other storage

        public void OnDeregister() {
            
        }

        public void OnRegister() {
            if (m_Timer != null && m_ReturnToTimer != null) {
                // synce this timer with ReturnTo's Timer
                m_Timer.Timer.Period = m_ReturnToTimer.Timer.Period;
            }
        }
    }
}
