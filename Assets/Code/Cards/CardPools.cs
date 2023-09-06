using System;
using BeauPools;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Cards
{
    public class CardPools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class CardPool : SerializablePool<CardUI> { }

        #endregion // Types

        public CardPool Cards;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister() {
            Cards.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister() {

        }
    }
}