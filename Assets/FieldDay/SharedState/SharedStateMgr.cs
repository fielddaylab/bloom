using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.SharedState {
    /// <summary>
    /// Manager for shared singleton state objects.
    /// This maintains access to singleton state objects by type.
    /// </summary>
    public sealed class SharedStateMgr {
        static private readonly StaticInjector<SharedStateReferenceAttribute, ISharedState> s_StaticInjector = new StaticInjector<SharedStateReferenceAttribute, ISharedState>();

        private readonly Dictionary<Type, ISharedState> m_States = new Dictionary<Type, ISharedState>(32);

        #region Add/Remove

        /// <summary>
        /// Registers the given ISharedState instance.
        /// </summary>
        public void Register(ISharedState state) {
            Assert.NotNull(state);
            Type stateType = state.GetType();
            Assert.False(m_States.ContainsKey(stateType), "[SharedStateMgr] Shared state of type '{0}' already registered", stateType);
            m_States.Add(stateType, state);
            s_StaticInjector.Inject(state);
            RegistrationCallbacks.InvokeRegister(state);
            Log.Msg("[SharedStateMgr] State '{0}' registered", stateType.FullName);
        }

        /// <summary>
        /// Deregisters the given ISharedState instance.
        /// </summary>
        public void Deregister(ISharedState state) {
            Assert.NotNull(state);
            Type stateType = state.GetType();
            if (m_States.TryGetValue(stateType, out ISharedState currentState) && currentState == state) {
                m_States.Remove(stateType);
                s_StaticInjector.Remove(state);
                RegistrationCallbacks.InvokeDeregister(state);
                Log.Msg("[SharedStateMgr] State '{0}' deregistered", stateType.FullName);
            }
        }

        /// <summary>
        /// Clears all ISharedState instances.
        /// </summary>
        public void Clear() {
            foreach(var state in m_States.Values) {
                s_StaticInjector.Remove(state);
                RegistrationCallbacks.InvokeDeregister(state);
            }
            m_States.Clear();
        }

        #endregion // Add/Remove

        #region Lookup

        /// <summary>
        /// Returns the shared state object of the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISharedState Get(Type type) {
            if (!m_States.TryGetValue(type, out ISharedState state)) {
                Assert.Fail("No shared state object found for type '{0}'", type.FullName);
            }
            return state;
        }

        /// <summary>
        /// Returns the shared state object for the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() where T : class, ISharedState {
            if (!m_States.TryGetValue(typeof(T), out ISharedState state)) {
                Assert.Fail("No shared state object found for type '{0}'", typeof(T).FullName);
            }
            return (T) state;
        }

        /// <summary>
        /// Attempts to return the shared state object for the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Type type, out ISharedState state) {
            return m_States.TryGetValue(type, out state);
        }

        /// <summary>
        /// Attempts to return the shared state object for the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet<T>(out T state) where T : class, ISharedState {
            if (m_States.TryGetValue(typeof(T), out ISharedState baseState)) {
                state = (T) baseState;
                return true;
            }

            state = null;
            return false;
        }

        /// <summary>
        /// Looks up all shared states that pass the given predicate.
        /// </summary>
        public int LookupAll(Predicate<ISharedState> predicate, List<ISharedState> sharedStates) {
            int found = 0;
            foreach(var state in m_States.Values) {
                if (predicate(state)) {
                    sharedStates.Add(state);
                    found++;
                }
            }
            return found;
        }

        /// <summary>
        /// Looks up all shared states that implement the given interface or class.
        /// </summary>
        public int LookupAll<T>(List<T> sharedStates) where T : class {
            int found = 0;
            foreach (var state in m_States.Values) {
                T casted = state as T;
                if (casted != null) {
                    sharedStates.Add(casted);
                    found++;
                }
            }
            return found;
        }

        /// <summary>
        /// Looks up all shared states that pass the given predicate.
        /// </summary>
        public int LookupAll<U>(Predicate<ISharedState, U> predicate, U predicateArg, List<ISharedState> sharedStates) {
            int found = 0;
            foreach (var state in m_States.Values) {
                if (predicate(state, predicateArg)) {
                    sharedStates.Add(state);
                    found++;
                }
            }
            return found;
        }

        #endregion // Lookup

        #region Require

        /// <summary>
        /// Retrieves the currently registered instance of the given type.
        /// Will create a new instance if one is not registered.
        /// 
        /// NOTE: Calling this with UnityEngine.Object-derived objects may cause memory leaks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISharedState Require(Type type) {
            if (!m_States.TryGetValue(type, out ISharedState state)) {
                state = CreateInstance(type);
                m_States.Add(type, state);
                s_StaticInjector.Inject(state);
                RegistrationCallbacks.InvokeRegister(state);
                Log.Msg("[SharedStateMgr] State '{0}' created on Require", type.FullName);
            }
            return state;
        }

        /// <summary>
        /// Retrieves the currently registered instance of the given type.
        /// Will create a new instance if one is not registered.
        /// 
        /// NOTE: Calling this with UnityEngine.Object-derived objects may cause memory leaks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Require<T>() where T : class, ISharedState {
            Type stateType = typeof(T);
            if (!m_States.TryGetValue(stateType, out ISharedState state)) {
                state = CreateInstance(stateType);
                m_States.Add(stateType, state);
                s_StaticInjector.Inject(state);
                RegistrationCallbacks.InvokeRegister(state);
                Log.Msg("[SharedStateMgr] State '{0}' created on Require", stateType.FullName);
            }
            return (T) state;
        }

        /// <summary>
        /// Creates a new instance of the given state type.
        /// </summary>
        static private ISharedState CreateInstance(Type stateType) {
            Assert.True(typeof(ISharedState).IsAssignableFrom(stateType), "Type '{0}' is not derived from ISharedState", stateType.FullName);
            if (typeof(UnityEngine.Object).IsAssignableFrom(stateType)) {
                Log.Error("[SharedStateMgr] Attempting to create instance of UnityEngine.Object derived class '{0}' - please don't use Require() for this type", stateType.FullName);
            }
            return (ISharedState) Activator.CreateInstance(stateType);
        }

        #endregion // Require
    }
}