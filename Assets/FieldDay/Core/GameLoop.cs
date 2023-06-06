#if UNITY_2019_1_OR_NEWER
#define USE_SRP
#endif // UNITY_2019_1_OR_NEWER

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Scripting;

#if USE_SRP
using UnityEngine.Rendering;
#endif // USE_SRP

namespace FieldDay {
    /// <summary>
    /// Game loop manager.
    /// </summary>
    [DefaultExecutionOrder(-10000000), DisallowMultipleComponent]
    public sealed class GameLoop : MonoBehaviour {
        #region Inspector

        [SerializeField, Tooltip("Size of the per-frame allocation buffer, in KiB")]
        private int m_SingleFrameAllocBufferSize = Frame.RuntimeHeapSize;

        [SerializeField, Range(30, 120)]
        private int m_TargetFramerate = 60;

        #endregion // Inspector

        #region Global Events

        /// <summary>
        /// Invoked during FixedUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnFixedUpdate = new CastableEvent<float>(8);
        
        /// <summary>
        /// Invoked during Update.
        /// </summary>
        static public readonly CastableEvent<float> OnUpdate = new CastableEvent<float>(16);

        /// <summary>
        /// Invoked during Update with the unscaled delta time.
        /// </summary>
        static public readonly CastableEvent<float> OnUnscaledUpdate = new CastableEvent<float>(4);

        /// <summary>
        /// Invoked during LateUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnLateUpdate = new CastableEvent<float>(16);

        /// <summary>
        /// Invoked during LateUpdate with the unscaled delta time.
        /// </summary>
        static public readonly CastableEvent<float> OnUnscaledLateUpdate = new CastableEvent<float>(4);

        /// <summary>
        /// Invoked during Canvas.preWillRenderCanvases
        /// </summary>
        static public readonly ActionEvent OnCanvasPreRender = new ActionEvent(4);

        /// <summary>
        /// Invoked during OnGUI.
        /// </summary>
        static public readonly CastableEvent<Event> OnGuiEvent = new CastableEvent<Event>(8);

        /// <summary>
        /// Invoked at the end of the frame, after rendering has concluded and frame data has advanced.
        /// </summary>
        static public readonly CastableEvent<ushort> OnFrameAdvance = new CastableEvent<ushort>(16);

        /// <summary>
        /// Invoked when OnApplicationQuit has been triggered.
        /// </summary>
        static public readonly ActionEvent OnShutdown = new ActionEvent(16);

        #endregion // Global Events

        static private readonly WaitForEndOfFrame s_EndOfFrame = new WaitForEndOfFrame();
        static private readonly RingBuffer<Action> s_OnBootQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_FrameStartQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_AfterLateUpdateQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_CanvasPreRenderQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_EndOfFrameQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static internal GameLoopPhase s_CurrentPhase = GameLoopPhase.None;
        static private ushort s_PrevUpdateFrameIndex = Frame.InvalidIndex;

        #region Unity Events

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            Log.Msg("[GameLoop] Starting...");
            Frame.CreateAllocator(m_SingleFrameAllocBufferSize);

            Log.Msg("[GameLoop] Creating systems manager...");
            Game.Systems = new SystemsMgr();

            Log.Msg("[GameLoop] Creating shared state manager...");
            Game.SharedState = new SharedStateMgr();

            Application.targetFrameRate = m_TargetFramerate;

            // find all pre-boot
            foreach (var entrypoint in Reflect.FindMethods<InvokePreBootAttribute>(Reflect.FindAllUserAssemblies(), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)) {
                entrypoint.Info.Invoke(null, null);
            }

            enabled = false;
            StartCoroutine(DelayedBoot());
        }

        private void Start() {
            Log.Msg("[GameLoop] Boot finished");
            SetCurrentPhase(GameLoopPhase.Booted);
            Game.Systems?.ProcessInitQueue();
            FlushQueue(s_OnBootQueue);

            FinishCallbackRegistration();

            // find all boot
            foreach (var entrypoint in Reflect.FindMethods<InvokeOnBootAttribute>(Reflect.FindAllUserAssemblies(), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)) {
                entrypoint.Info.Invoke(null, null);
            }
        }

        private void FinishCallbackRegistration() {
            Canvas.preWillRenderCanvases += OnPreCanvasRender;

            #if !UNITY_EDITOR
            StartCoroutine(EndOfFrameCoroutine());
            #endif // !UNITY_EDITOR
        }

        private void OnDestroy() {
            if (s_CurrentPhase != GameLoopPhase.Shutdown) {
                OnApplicationQuit();
            }
            Canvas.preWillRenderCanvases -= OnPreCanvasRender;
            Frame.DestroyAllocator();
        }

        private void OnApplicationQuit() {
            Log.Msg("[GameLoop] Shutting down...");
            SetCurrentPhase(GameLoopPhase.Shutdown);
            OnShutdown.Invoke();

            if (Game.Events != null) {
                Game.Events.Clear();
                Game.Events = null;
            }

            Game.Systems.Shutdown();
            Game.Systems = null;
        }

        private void FixedUpdate() {
            FlushQueue(s_OnBootQueue);
            HandlePreUpdate();
            SetCurrentPhase(GameLoopPhase.FixedUpdate);
            Game.Systems?.FixedUpdate(Time.fixedDeltaTime);
            OnFixedUpdate.Invoke(Time.fixedDeltaTime);
        }

        private void Update() {
            // just in case we didn't have a FixedUpdate this frame
            HandlePreUpdate();

            // time-scaled update
            SetCurrentPhase(GameLoopPhase.Update);
            Game.Systems?.Update(Time.deltaTime);
            OnUpdate.Invoke(Time.deltaTime);

            // unscaled update
            SetCurrentPhase(GameLoopPhase.UnscaledUpdate);
            Game.Systems?.UnscaledUpdate(Time.unscaledDeltaTime);
            OnUnscaledUpdate.Invoke(Time.unscaledDeltaTime);

            // flush event queue
            Game.Events?.Flush();
        }

        private void LateUpdate() {
            // time-scaled late update
            SetCurrentPhase(GameLoopPhase.LateUpdate);
            Game.Systems?.LateUpdate(Time.unscaledDeltaTime);
            OnLateUpdate.Invoke(Time.deltaTime);

            // unscaled late update
            SetCurrentPhase(GameLoopPhase.UnscaledLateUpdate);
            Game.Systems?.UnscaledLateUpdate(Time.unscaledDeltaTime);
            OnUnscaledLateUpdate.Invoke(Time.unscaledDeltaTime);

            // flush event queue
            Game.Events?.Flush();

            FlushQueue(s_AfterLateUpdateQueue);
        }

        private void OnGUI() {
            EventType type = Event.current.type;
            OnGuiEvent.Invoke(Event.current);

            #if UNITY_EDITOR
            if (type == EventType.Repaint) {
                OnEndOfFrame();
            }
            #endif // UNITY_EDITOR
        }

        private void OnCameraPreCull() {

        }

        private void OnCameraPreRender() {

        }

        private void OnCameraPostRender() {

        }

        private IEnumerator DelayedBoot() {
            yield return s_EndOfFrame;
            enabled = true;
        }

        static private IEnumerator EndOfFrameCoroutine() {
            while(true) {
                yield return s_EndOfFrame;
                OnEndOfFrame();
            }
        }

        #endregion // Unity Events

        #region Handlers

        static private void HandlePreUpdate() {
            if (s_PrevUpdateFrameIndex != Frame.Index) {
                SetCurrentPhase(GameLoopPhase.PreUpdate);
                s_PrevUpdateFrameIndex = Frame.Index;
                FlushQueue(s_FrameStartQueue);
            }
        }

        static private void OnPreCanvasRender() {
            SetCurrentPhase(GameLoopPhase.CanvasPreRender);
            OnCanvasPreRender.Invoke();
            FlushQueue(s_CanvasPreRenderQueue);
        }

        static private void OnEndOfFrame() {
            SetCurrentPhase(GameLoopPhase.FrameAdvance);
            FlushQueue(s_EndOfFrameQueue);

            Frame.IncrementFrame();
            Frame.ResetAllocator();
            OnFrameAdvance.Invoke(Frame.Index);
        }

        #endregion // Handlers

        #region Queues

        /// <summary>
        /// Queues an action to happen at the start of the next frame.
        /// </summary>
        static public void QueueOnBoot(Action action) {
            s_OnBootQueue.PushBack(action);
        }

        /// <summary>
        /// Queues an action to happen before any updates occur on the next frame.
        /// </summary>
        static public void QueuePreUpdate(Action action) {
            s_FrameStartQueue.PushBack(action);
        }

        /// <summary>
        /// Queues an action to happen at the end of the current frame.
        /// </summary>
        static public void QueueEndOfFrame(Action action) {
            s_EndOfFrameQueue.PushBack(action);
        }

        /// <summary>
        /// Queues an action to happen after GameLoop.OnLateUpdate
        /// </summary>
        static public void QueueAfterLateUpdate(Action action) {
            s_AfterLateUpdateQueue.PushBack(action);
        }

        /// <summary>
        /// Queues an action to happen during Canvas.preWillRenderCanvases
        /// </summary>
        static public void QueueBeforeCanvasRender(Action action) {
            s_CanvasPreRenderQueue.PushBack(action);
        }

        static private void FlushQueue(RingBuffer<Action> queue) {
            while (queue.TryPopFront(out Action action)) {
                action();
            }
        }

        #endregion // Queues

        #region Phase

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private void SetCurrentPhase(GameLoopPhase phase) {
            s_CurrentPhase = phase;
            Log.Msg("[GameLoop] Entering phase '{0}' on frame {1}", phase, Frame.Index);
        }

        /// <summary>
        /// Is the game loop currently executing pre-update, fixed update, or late update.
        /// </summary>
        static public bool IsUpdating() {
            return PhaseInRange(s_CurrentPhase, GameLoopPhase.PreUpdate, GameLoopPhase.UnscaledLateUpdate);
        }

        /// <summary>
        /// Is the game loop currently executing rendering steps.
        /// </summary>
        static public bool IsRendering() {
            return PhaseInRange(s_CurrentPhase, GameLoopPhase.PreCull, GameLoopPhase.PostRender);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool PhaseInRange(GameLoopPhase phase, GameLoopPhase start, GameLoopPhase end) {
            return phase >= start && phase <= end;
        }

        #endregion // Phase
    }

    /// <summary>
    /// Game loop phase.
    /// </summary>
    [LabeledEnum(false)]
    public enum GameLoopPhase : uint {
        [Label("None")] None,
        [Label("Booted")] Booted,

        [Label("Pre Update")] PreUpdate,
        [Label("Fixed Update")] FixedUpdate,
        [Label("Update")] Update,
        [Label("Unscaled Update")] UnscaledUpdate,
        [Label("Late Update")] LateUpdate,
        [Label("Unscaled Late Update")] UnscaledLateUpdate,

        [Label("Canvas Pre-Render")] CanvasPreRender,

        [Label("Pre-Cull")] PreCull,
        [Label("Pre-Render")] PreRender,
        [Label("Post-Render")] PostRender,

        [Label("Frame Advanced")] FrameAdvance,

        [Label("Shutdown")] Shutdown,
    }

    /// <summary>
    /// Attribute marking a static method to be invoked before GameLoop.OnBooted has been invoked
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class InvokePreBootAttribute : PreserveAttribute { }

    /// <summary>
    /// Attribute marking a static method to be invoked after GameLoop.OnBooted has been invoked
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class InvokeOnBootAttribute : PreserveAttribute { }
}