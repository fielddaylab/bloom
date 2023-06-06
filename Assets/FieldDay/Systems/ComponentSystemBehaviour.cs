using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System operating on all instances of a given component.
    /// </summary>
    public abstract class ComponentSystemBehaviour<TComponent> : MonoBehaviour, IComponentSystem<TComponent>
        where TComponent : class, IComponentData {
        #region Inspector

        [SerializeField] private int m_InitialCapacity = 64;

        #endregion // Inspector

        protected RingBuffer<TComponent> m_Components;

        public int Count {
            get { return m_Components.Count; }
        }

        public Type ComponentType {
            get { return typeof(TComponent); }
        }

        #region Work

        public virtual bool HasWork() {
            return m_Components.Count > 0;
        }

        public virtual void ProcessWork(float deltaTime) {
            for (int i = 0, count = m_Components.Count; i < count; i++) {
                ProcessWorkForComponent(m_Components[i], deltaTime);
            }
        }

        public virtual void ProcessWorkForComponent(TComponent component, float deltaTime) {
        }

        #endregion // Work

        #region Add/Remove

        void IComponentSystem.Add(object component) {
            Add((TComponent) component);
        }

        void IComponentSystem.Remove(object component) {
            Remove((TComponent) component);
        }

        public void Add(TComponent component) {
            Assert.False(m_Components.Contains(component));
            m_Components.PushBack(component);
            OnComponentAdded(component);
        }

        public void Remove(TComponent component) {
            Assert.True(m_Components.Contains(component));
            m_Components.FastRemove(component);
            OnComponentRemoved(component);
        }

        protected virtual void OnComponentAdded(TComponent component) { }
        protected virtual void OnComponentRemoved(TComponent component) { }

        #endregion // Add/Remove

        #region Lifecycle

        public virtual void Initialize() {
            m_Components = new RingBuffer<TComponent>(m_InitialCapacity, RingBufferMode.Expand);
        }

        public virtual void Shutdown() {
            foreach(var component in m_Components) {
                OnComponentRemoved(component);
            }
            m_Components.Clear();
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on all instances of a given component paired with another component type.
    /// </summary>
    public abstract class ComponentSystemBehaviour<TPrimary, TSecondary> : MonoBehaviour, IComponentSystem<TPrimary>
        where TPrimary : class, IComponentData
        where TSecondary : class, IComponentData {
        #region Inspector

        [SerializeField] private int m_InitialCapacity = 64;

        #endregion // Inspector

        protected RingBuffer<ComponentTuple<TPrimary, TSecondary>> m_Components;

        public int Count {
            get { return m_Components.Count; }
        }

        public Type ComponentType {
            get { return typeof(TPrimary); }
        }

        #region Work

        public virtual bool HasWork() {
            return m_Components.Count > 0;
        }

        public virtual void ProcessWork(float deltaTime) {
            for (int i = 0, count = m_Components.Count; i < count; i++) {
                ComponentTuple<TPrimary, TSecondary> tuple = m_Components[i];
                ProcessWorkForComponent(tuple.Primary, tuple.Secondary, deltaTime);
            }
        }

        public virtual void ProcessWorkForComponent(TPrimary primary, TSecondary secondary, float deltaTime) {
        }

        #endregion // Work

        #region Add/Remove

        void IComponentSystem.Add(object component) {
            Add((TPrimary) component);
        }

        void IComponentSystem.Remove(object component) {
            Remove((TPrimary) component);
        }

        public void Add(TPrimary component) {
            int found = m_Components.FindIndex((a, b) => a.Primary == b, component);
            Assert.True(found < 0, "component already added");
            if (ComponentUtility.Sibling(component, out TSecondary additional0)) {
                m_Components.PushBack(new ComponentTuple<TPrimary, TSecondary>(component, additional0));
                OnComponentAdded(component, additional0);
            }
        }

        public void Remove(TPrimary component) {
            int found = m_Components.FindIndex((a, b) => a.Primary == b, component);
            if (found >= 0) {
                ComponentTuple<TPrimary, TSecondary> componentData = m_Components[found];
                m_Components.FastRemoveAt(found);
                OnComponentRemoved(componentData.Primary, componentData.Secondary);
            }
        }

        protected virtual void OnComponentAdded(TPrimary primary, TSecondary secondary) { }
        protected virtual void OnComponentRemoved(TPrimary primary, TSecondary secondary) { }

        #endregion // Add/Remove

        #region Lifecycle

        public virtual void Initialize() {
            m_Components = new RingBuffer<ComponentTuple<TPrimary, TSecondary>>(m_InitialCapacity, RingBufferMode.Expand);
        }

        public virtual void Shutdown() {
            foreach (var component in m_Components) {
                OnComponentRemoved(component.Primary, component.Secondary);
            }
            m_Components.Clear();
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on all instances of a given component paired with two component types.
    /// </summary>
    public abstract class ComponentSystemBehaviour<TPrimary, TComponentA, TComponentB> : MonoBehaviour, IComponentSystem<TPrimary>
        where TPrimary : class, IComponentData
        where TComponentA : class, IComponentData
        where TComponentB : class, IComponentData {
        #region Inspector

        [SerializeField] private int m_InitialCapacity = 64;

        #endregion // Inspector

        protected RingBuffer<ComponentTuple<TPrimary, TComponentA, TComponentB>> m_Components;

        public int Count {
            get { return m_Components.Count; }
        }

        public Type ComponentType {
            get { return typeof(TPrimary); }
        }

        #region Work

        public virtual bool HasWork() {
            return m_Components.Count > 0;
        }

        public virtual void ProcessWork(float deltaTime) {
            for (int i = 0, count = m_Components.Count; i < count; i++) {
                ComponentTuple<TPrimary, TComponentA, TComponentB> tuple = m_Components[i];
                ProcessWorkForComponent(tuple.Primary, tuple.ComponentA, tuple.ComponentB, deltaTime);
            }
        }

        public virtual void ProcessWorkForComponent(TPrimary primary, TComponentA componentA, TComponentB componentB, float deltaTime) {
        }

        #endregion // Work

        #region Add/Remove

        void IComponentSystem.Add(object component) {
            Add((TPrimary) component);
        }

        void IComponentSystem.Remove(object component) {
            Remove((TPrimary) component);
        }

        public void Add(TPrimary component) {
            int found = m_Components.FindIndex((a, b) => a.Primary == b, component);
            Assert.True(found < 0, "component already added");
            if (ComponentUtility.Siblings(component, out TComponentA additional0, out TComponentB additional1)) {
                m_Components.PushBack(new ComponentTuple<TPrimary, TComponentA, TComponentB>(component, additional0, additional1));
                OnComponentAdded(component, additional0, additional1);
            }
        }

        public void Remove(TPrimary component) {
            int found = m_Components.FindIndex((a, b) => a.Primary == b, component);
            if (found >= 0) {
                ComponentTuple<TPrimary, TComponentA, TComponentB> componentData = m_Components[found];
                m_Components.FastRemoveAt(found);
                OnComponentRemoved(componentData.Primary, componentData.ComponentA, componentData.ComponentB);
            }
        }

        protected virtual void OnComponentAdded(TPrimary primary, TComponentA componentA, TComponentB componentB) { }
        protected virtual void OnComponentRemoved(TPrimary primary, TComponentA componentA, TComponentB componentB) { }

        #endregion // Add/Remove

        #region Lifecycle

        public virtual void Initialize() {
            m_Components = new RingBuffer<ComponentTuple<TPrimary, TComponentA, TComponentB>>(m_InitialCapacity, RingBufferMode.Expand);
        }

        public virtual void Shutdown() {
            foreach (var component in m_Components) {
                OnComponentRemoved(component.Primary, component.ComponentA, component.ComponentB);
            }
            m_Components.Clear();
        }

        #endregion // Lifecycle
    }
}