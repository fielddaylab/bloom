using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FieldDay {

    /// <summary>
    /// Event dispatcher collection.
    /// </summary>
    public sealed class EventDispatcher<TArg> : IEventDispatcher {
        #region Types

        /// <summary>
        /// Record of a queued event.
        /// </summary>
        private struct QueuedEvent {
            public StringHash32 Id;
            public TArg Arg;

            public QueuedEvent(StringHash32 id, TArg arg) {
                Id = id;
                Arg = arg;
            }
        }

        #endregion // Types

        private readonly Dictionary<uint, CastableEvent<TArg>> m_HandlerBlocks;
        private readonly RingBuffer<QueuedEvent> m_EventQueue;
        private readonly RingBuffer<CastableEvent<TArg>> m_BlockPool;
        private readonly int m_DefaultEventHandlerCapacity;
        private readonly List<uint> m_TempEventIdList;

        public EventDispatcher(int initialEventTypeCapacity = 32, int initialEventQueueCapacity = 32, int defaultHandlerCapacity = 4) {
            m_HandlerBlocks = new Dictionary<uint, CastableEvent<TArg>>(initialEventTypeCapacity);
            m_EventQueue = new RingBuffer<QueuedEvent>(initialEventQueueCapacity, RingBufferMode.Expand);

            m_BlockPool = new RingBuffer<CastableEvent<TArg>>(4, RingBufferMode.Expand);

            m_TempEventIdList = new List<uint>(initialEventTypeCapacity / 2);
            m_DefaultEventHandlerCapacity = defaultHandlerCapacity;
        }

        #region Internal

        // Gets or creates a handler block for the given event id
        private CastableEvent<TArg> GetOrCreateEventBlock(StringHash32 id) {
            if (id.IsEmpty) {
                throw new ArgumentNullException("eventId", "Cannot register for an empty eventId");
            }
            if (!m_HandlerBlocks.TryGetValue(id.HashValue, out CastableEvent<TArg> block)) {
                if (m_BlockPool.Count > 0) {
                    block = m_BlockPool.PopBack();
                } else {
                    block = new CastableEvent<TArg>(m_DefaultEventHandlerCapacity);
                }
                m_HandlerBlocks.Add(id.HashValue, block);
            }
            return block;
        }

        #endregion // Internal

        #region Register

        /// <summary>
        /// Registers a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Register(StringHash32 eventId, Action action, UnityEngine.Object context = null) {
            CastableEvent<TArg> block = GetOrCreateEventBlock(eventId);
            block.Register(action, context);
            return this;
        }

        /// <summary>
        /// Registers a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Register(StringHash32 eventId, Action<TArg> action, UnityEngine.Object context = null) {
            CastableEvent<TArg> block = GetOrCreateEventBlock(eventId);
            block.Register(action, context);
            return this;
        }

        /// <summary>
        /// Registers a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Register(StringHash32 eventId, RefAction<TArg> action, UnityEngine.Object context = null) {
            CastableEvent<TArg> block = GetOrCreateEventBlock(eventId);
            block.Register(action, context);
            return this;
        }

        /// <summary>
        /// Registers a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Register<TCasted>(StringHash32 eventId, Action<TCasted> action, UnityEngine.Object context = null) {
            CastableEvent<TArg> block = GetOrCreateEventBlock(eventId);
            block.Register<TCasted>(action, context);
            return this;
        }

        #endregion // Register

        #region Deregister

        /// <summary>
        /// Deregisters a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Deregister(StringHash32 eventId, Action action) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> block)) {
                block.Deregister(action);
            }
            return this;
        }

        /// <summary>
        /// Deregisters a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Deregister(StringHash32 eventId, Action<TArg> action) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> block)) {
                block.Deregister(action);
            }
            return this;
        }

        /// <summary>
        /// Deregisters a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Deregister(StringHash32 eventId, RefAction<TArg> action) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> block)) {
                block.Deregister(action);
            }
            return this;
        }

        /// <summary>
        /// Deregisters a handler for the given event id.
        /// </summary>
        public EventDispatcher<TArg> Deregister<TCasted>(StringHash32 eventId, Action<TCasted> action) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> block)) {
                block.Deregister(action);
            }
            return this;
        }

        /// <summary>
        /// Deregisters all handlers for the given event id.
        /// </summary>
        public EventDispatcher<TArg> DeregisterAll(StringHash32 eventId) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> block)) {
                block.Clear();
                m_BlockPool.PushBack(block);
                m_HandlerBlocks.Remove(eventId.HashValue);
            }
            return this;
        }

        /// <summary>
        /// Deregisters all handlers bound to the given context.
        /// </summary>
        public EventDispatcher<TArg> DeregisterAllForContext(UnityEngine.Object context) {
            if (ReferenceEquals(context, null)) {
                return this;
            }

            m_TempEventIdList.Clear();
            foreach (var kv in m_HandlerBlocks) {
                if (kv.Value.DeregisterAll(context) > 0 && kv.Value.IsEmpty) {
                    m_BlockPool.PushBack(kv.Value);
                    m_TempEventIdList.Add(kv.Key);
                }
            }

            foreach (var eventId in m_TempEventIdList) {
                m_HandlerBlocks.Remove(eventId);
            }

            m_TempEventIdList.Clear();
            return this;
        }

        #endregion // Deregister

        #region Invoke

        /// <summary>
        /// Dispatches the given event to all corresponding handlers.
        /// </summary>
        public void Dispatch(StringHash32 eventId) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> evt)) {
                evt.Invoke(default(TArg));
            }
        }

        /// <summary>
        /// Dispatches the given event to all corresponding handlers.
        /// </summary>
        public void Dispatch(StringHash32 eventId, TArg arg) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> evt)) {
                evt.Invoke(ref arg);
            }
        }

        /// <summary>
        /// Dispatches the given event to all corresponding handlers.
        /// </summary>
        public void Dispatch(StringHash32 eventId, ref TArg arg) {
            if (m_HandlerBlocks.TryGetValue(eventId.HashValue, out CastableEvent<TArg> evt)) {
                evt.Invoke(ref arg);
            }
        }

        /// <summary>
        /// Queues an event to dispatch at the next time Flush() is called
        /// </summary>
        public void Queue(StringHash32 eventId) {
            m_EventQueue.PushBack(new QueuedEvent(eventId, default(TArg)));
        }

        /// <summary>
        /// Queues an event to dispatch at the next time Flush() is called
        /// </summary>
        public void Queue(StringHash32 eventId, TArg arg) {
            m_EventQueue.PushBack(new QueuedEvent(eventId, arg));
        }

        /// <summary>
        /// Flushes all queued events.
        /// </summary>
        public void Flush() {
            QueuedEvent evt;
            while(m_EventQueue.TryPopFront(out evt)) {
                Dispatch(evt.Id, ref evt.Arg);
            }
        }

        /// <summary>
        /// Returns an enumerator that waits for the given event id to be dispatched.
        /// </summary>
        public WaitForEventEnumerator Wait(StringHash32 eventId) {
            return WaitForEventEnumerator.Create(this, eventId);
        }

        #endregion // Invoke

        #region Cleanup

        /// <summary>
        /// Clears all event handlers and queued events.
        /// </summary>
        public void Clear() {
            m_EventQueue.Clear();
            foreach(var kv in m_HandlerBlocks) {
                kv.Value.Clear();
                m_BlockPool.PushBack(kv.Value);
            }
            m_HandlerBlocks.Clear();
        }

        /// <summary>
        /// Cleans up any event handlers owned by now-dead objects.
        /// </summary>
        public void CleanupDeadReferences() {
            m_TempEventIdList.Clear();
            foreach (var kv in m_HandlerBlocks) {
                int eventDeadCount = kv.Value.DeregisterAllWithDeadContext();
                if (eventDeadCount > 0) {
                    Log.Msg("[EventDispatcher] Found {0} stale handlers for event '{1}' - make sure to deregister your handlers in OnDisable or OnDestroy", eventDeadCount, new StringHash32(kv.Key).ToDebugString());
                    if (kv.Value.IsEmpty) {
                        m_BlockPool.PushBack(kv.Value);
                        m_TempEventIdList.Add(kv.Key);
                    }
                }
            }

            foreach (var eventId in m_TempEventIdList) {
                m_HandlerBlocks.Remove(eventId);
            }

            m_TempEventIdList.Clear();
        }

        #endregion // Cleanup

        #region Explicit Interface Implementations

        IEventDispatcher IEventDispatcher.Register(StringHash32 eventId, Action action, UnityEngine.Object context) {
            return Register(eventId, action, context);
        }

        IEventDispatcher IEventDispatcher.Deregister(StringHash32 eventId, Action action) {
            return Deregister(eventId, action);
        }

        IEventDispatcher IEventDispatcher.DeregisterAll(StringHash32 eventId) {
            return DeregisterAll(eventId);
        }

        IEventDispatcher IEventDispatcher.DeregisterAllForContext(UnityEngine.Object context) {
            return DeregisterAllForContext(context);
        }

        #endregion // Explicit Interface Implementations
    }

    /// <summary>
    /// Event dispatcher interface.
    /// </summary>
    public interface IEventDispatcher {
        /// <summary>
        /// Cleans up any event handlers owned by now-dead objects.
        /// </summary>
        void CleanupDeadReferences();

        /// <summary>
        /// Flushes all queued events.
        /// </summary>
        void Flush();

        /// <summary>
        /// Clears all queued events and handlers.
        /// </summary>
        void Clear();

        /// <summary>
        /// Dispatches the given event to all corresponding handlers.
        /// </summary>
        void Dispatch(StringHash32 eventId);

        /// <summary>
        /// Queues an event to dispatch at the next time Flush() is called
        /// </summary>
        void Queue(StringHash32 eventId);

        /// <summary>
        /// Registers a parameter-less handler for the given event type.
        /// </summary>
        IEventDispatcher Register(StringHash32 eventId, Action action, UnityEngine.Object context = null);

        /// <summary>
        /// Deregisters a parameter-less handler from the given event type.
        /// </summary>
        IEventDispatcher Deregister(StringHash32 eventId, Action action);

        /// <summary>
        /// Deregisters all handlers for the given event id.
        /// </summary>
        IEventDispatcher DeregisterAll(StringHash32 eventId);

        /// <summary>
        /// Deregisters all handlers bound to the given context.
        /// </summary>
        IEventDispatcher DeregisterAllForContext(UnityEngine.Object context);

        /// <summary>
        /// Waits for the given event to be dispatched.
        /// </summary>
        WaitForEventEnumerator Wait(StringHash32 eventId);
    }

    /// <summary>
    /// Enumerator class that waits until an event is dispatched.
    /// </summary>
    public sealed class WaitForEventEnumerator : IEnumerator, IDisposable {
        private const int UNINITIALIZED = 0;
        private const int WAITING = 1;
        private const int DONE = 2;

        static private readonly RingBuffer<WaitForEventEnumerator> s_Pool = new RingBuffer<WaitForEventEnumerator>(8, RingBufferMode.Expand);

        private IEventDispatcher m_Parent;
        private StringHash32 m_EventId;
        private int m_Phase;

        private readonly Action m_CachedHandler;

        public object Current { get { return null; } }

        internal WaitForEventEnumerator() {
            m_CachedHandler = OnInvoke;
        }

        static internal WaitForEventEnumerator Create(IEventDispatcher parent, StringHash32 eventId) {
            if (!s_Pool.TryPopBack(out WaitForEventEnumerator inst)) {
                inst = new WaitForEventEnumerator();
            }
            inst.Init(parent, eventId);
            return inst;
        }

        internal void Init(IEventDispatcher parent, StringHash32 eventId) {
            if (m_Phase > UNINITIALIZED) {
                m_Parent.Deregister(eventId, m_CachedHandler);
                m_Phase = UNINITIALIZED;
            }

            m_Parent = parent;
            m_EventId = eventId;
        }

        private void OnInvoke() {
            m_Phase = DONE;
        }

        public void Dispose() {
            if (m_Phase > UNINITIALIZED) {
                m_Parent.Deregister(m_EventId, m_CachedHandler);
                s_Pool.PushBack(this);
                m_Phase = UNINITIALIZED;
            }

            m_EventId = StringHash32.Null;
        }

        public bool MoveNext() {
            if (m_Phase == UNINITIALIZED) {
                m_Parent.Register(m_EventId, m_CachedHandler);
                m_Phase = WAITING;
            }

            return m_Phase == WAITING;
        }

        public void Reset() {
            if (m_Phase > UNINITIALIZED) {
                m_Parent.Deregister(m_EventId, m_CachedHandler);
                m_Phase = UNINITIALIZED;
            }
        }
    }
}