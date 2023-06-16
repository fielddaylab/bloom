using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Systems;

namespace FieldDay.Components {
    /// <summary>
    /// Component manager.
    /// </summary>
    public sealed class ComponentMgr {
        private SystemsMgr m_SystemsMgr;
        private Dictionary<Type, List<IComponentData>> m_ComponentLists;
        private RingBuffer<IComponentData> m_AddQueue = new RingBuffer<IComponentData>(64, RingBufferMode.Expand);
        private RingBuffer<IComponentData> m_RemovalQueue = new RingBuffer<IComponentData>(64, RingBufferMode.Expand);
        private int m_ModificationLock;

        public ComponentMgr(SystemsMgr systemsMgr) {
            Assert.NotNull(systemsMgr);
            m_SystemsMgr = systemsMgr;
            m_ComponentLists = new Dictionary<Type, List<IComponentData>>(16);
        }

        #region Registry

        /// <summary>
        /// Adds the given component to the component registry
        /// and all relevant system instances.
        /// </summary>
        public void Register(IComponentData component) {
            if (m_ModificationLock > 0) {
                m_RemovalQueue.FastRemove(component);
                if (!m_AddQueue.Contains(component)) {
                    m_AddQueue.PushBack(component);
                }
                return;
            }

            RegisterImpl(component);
        }

        /// <summary>
        /// Removes the given component from the component registry
        /// and all relevant system instances.
        /// </summary>
        public void Deregister(IComponentData component) {
            if (m_ModificationLock > 0) {
                if (!m_AddQueue.FastRemove(component)) {
                    if (!m_RemovalQueue.Contains(component)) {
                        m_RemovalQueue.PushBack(component);
                    }
                }
                return;
            }

            DeregisterImpl(component);
        }

        private void RegisterImpl(IComponentData component) {
            Type componentType = component.GetType();
            if (!m_ComponentLists.TryGetValue(componentType, out List<IComponentData> components)) {
                components = new List<IComponentData>(8);
                m_ComponentLists.Add(componentType, components);
            }
            components.Add(component);

            m_SystemsMgr.AddComponent(component);
        }

        private void DeregisterImpl(IComponentData component) {
            Type componentType = component.GetType();
            if (m_ComponentLists.TryGetValue(componentType, out List<IComponentData> components)) {
                components.FastRemove(component);
            }

            m_SystemsMgr.RemoveComponent(component);
        }

        #endregion // Registry

        #region Lock/Unlock

        /// <summary>
        /// Locks the component manager from further modifications.
        /// This ensures component lists remain consistent.
        /// </summary>
        public void Lock() {
            m_ModificationLock++;
        }

        /// <summary>
        /// Unlocks the component manager, allowing further modifications
        /// and processing all queued modifications.
        /// </summary>
        public void Unlock() {
            Assert.True(m_ModificationLock > 0, "Unbalanced Lock/Unlock calls");
            if (m_ModificationLock-- == 1) {
                while (m_AddQueue.TryPopBack(out IComponentData component)) {
                    RegisterImpl(component);
                }

                while (m_RemovalQueue.TryPopBack(out IComponentData component)) {
                    DeregisterImpl(component);
                }
            }
        }

        /// <summary>
        /// Locks and returns a lock object
        /// that will unlock once disposed.
        /// </summary>
        public DisposableLock GetLock() {
            return new DisposableLock();
        }

        /// <summary>
        /// Disposable lock object.
        /// </summary>
        public struct DisposableLock : IDisposable {
            private ComponentMgr m_Mgr;

            internal DisposableLock(ComponentMgr mgr) {
                m_Mgr = mgr;
                mgr.Lock();
            }

            public void Dispose() {
                if (m_Mgr != null) {
                    m_Mgr.Unlock();
                    m_Mgr = null;
                }
            }
        }

        #endregion // Lock/Unlock

        #region Iteration

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public ComponentIterator<IComponentData> ComponentsOfType(Type componentType) {
            if (m_ComponentLists.TryGetValue(componentType, out List<IComponentData> components)) {
                return new ComponentIterator<IComponentData>(components);
            }
            return default;
        }

        /// <summary>
        /// Enumerates all the components of the given type.
        /// </summary>
        public ComponentIterator<T> ComponentsOfType<T>() where T : class, IComponentData {
            if (m_ComponentLists.TryGetValue(typeof(T), out List<IComponentData> components)) {
                return new ComponentIterator<T>(components);
            }
            return default;
        }

        #endregion // Iteration

        #region Events

        internal void Shutdown() {
            m_SystemsMgr = null;
            foreach(var list in m_ComponentLists) {
                list.Value.Clear();
            }
            m_ComponentLists.Clear();
            m_ComponentLists = null;
        }

        #endregion // Events
    }

    /// <summary>
    /// Component iterator.
    /// </summary>
    public struct ComponentIterator<T> : IEnumerator<T>, IDisposable where T : class, IComponentData {
        private List<IComponentData>.Enumerator m_Source;

        internal ComponentIterator(List<IComponentData> source) {
            m_Source = source.GetEnumerator();
        }

        public T Current {
            get { return (T) m_Source.Current; }
        }

        object IEnumerator.Current { get { return Current; } }

        public void Dispose() {
            m_Source = default;
        }

        public bool MoveNext() {
            return m_Source.MoveNext();
        }

        void IEnumerator.Reset() {
            ((IEnumerator) m_Source).Reset();
        }
    }
}