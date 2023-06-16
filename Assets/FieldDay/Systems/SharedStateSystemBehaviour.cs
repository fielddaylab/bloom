using System;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System operating on a shared state instance.
    /// </summary>
    public abstract class SharedStateSystemBehaviour<TState> : MonoBehaviour, ISystem
        where TState : class, ISharedState {
        [SharedStateReference] static private TState s_State;

        [NonSerialized] protected TState m_State;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_State = s_State) != null;
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
            Game.SharedState.TryGet(out s_State);
        }

        public virtual void Shutdown() {
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on two shared state instance.
    /// </summary>
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState {
        
        [SharedStateReference] static private TStateA s_StateA;
        [SharedStateReference] static private TStateB s_StateB;

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_StateA = s_StateA) != null && (m_StateB = s_StateB) != null;
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
            Game.SharedState.TryGet(out s_StateA);
            Game.SharedState.TryGet(out s_StateB);
        }

        public virtual void Shutdown() {
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on three shared state instances.
    /// </summary>
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState {

        [SharedStateReference] static private TStateA s_StateA;
        [SharedStateReference] static private TStateB s_StateB;
        [SharedStateReference] static private TStateC s_StateC;

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_StateA = s_StateA) != null && (m_StateB = s_StateB) != null && (m_StateC = s_StateC) != null; 
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
            Game.SharedState.TryGet(out s_StateA);
            Game.SharedState.TryGet(out s_StateB);
            Game.SharedState.TryGet(out s_StateC);
        }

        public virtual void Shutdown() {
        }

        #endregion // Lifecycle
    }
}