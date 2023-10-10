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
using FieldDay.Components;
using FieldDay.Processes;
using FieldDay.Data;
using BeauPools;
using FieldDay.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

#if USE_SRP
using UnityEngine.Rendering;
#endif // USE_SRP

namespace FieldDay {
    /// <summary>
    /// Game loop manager.
    /// </summary>
    [DefaultExecutionOrder(-10000000), DisallowMultipleComponent]
    public sealed class GameLoop : MonoBehaviour, ICameraPreCullCallback, ICameraPostRenderCallback, ICameraPreRenderCallback {
        #region Inspector

        [SerializeField, Tooltip("Size of the per-frame allocation buffer, in KiB")]
        private int m_SingleFrameAllocBufferSize = Frame.RuntimeHeapSize;

        [SerializeField, Range(30, 120)]
        private int m_TargetFramerate = 60;

        #endregion // Inspector

        #region Global Events

        /// <summary>
        /// Invoked during PreUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnPreUpdate = new CastableEvent<float>(16);

        /// <summary>
        /// Invoked during DebugUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnDebugUpdate = new CastableEvent<float>(16);

        /// <summary>
        /// Invoked during FixedUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnFixedUpdate = new CastableEvent<float>(8);

        /// <summary>
        /// Invoked after FixedUpdate.
        /// </summary>
        static public readonly CastableEvent<float> OnLateFixedUpdate = new CastableEvent<float>(8);

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
        
        // action queues
        static private readonly RingBuffer<Action> s_OnBootQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_FrameStartQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_AfterLateUpdateQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_CanvasPreRenderQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        static private readonly RingBuffer<Action> s_EndOfFrameQueue = new RingBuffer<Action>(16, RingBufferMode.Expand);
        
        // phase tracking
        static internal GameLoopPhase s_CurrentPhase = GameLoopPhase.None;
        static private ushort s_PrevUpdateFrameIndex = Frame.InvalidIndex;
        static private bool m_ReadyForRender;
        static private bool s_Initialized;

        // update masks
        static private int s_UpdateMask = Bits.All32;
        static private int s_QueuedUpdateMask = Bits.All32;

        // time scales
        static private float s_TimeScale = 1;
        static private float s_QueuedTimeScale = 1;

        // pausing
        static private bool s_DebugPause = false;

        // profiling
        static private PhaseTiming s_TimeProfiling;

        [NonSerialized] private bool m_Initialized;

        #region Unity Events

        private void Awake() {
            if (s_Initialized) {
                DestroyImmediate(gameObject);
                return;
            }

            s_Initialized = true;
            m_Initialized = true;
            DontDestroyOnLoad(gameObject);
            Log.Msg("[GameLoop] Starting...");
            Frame.MarkTimestampOffset();
            Frame.CreateAllocator(m_SingleFrameAllocBufferSize);

            Log.Msg("[GameLoop] Creating systems manager...");
            Game.Systems = new SystemsMgr();

            Log.Msg("[GameLoop] Creating component manager...");
            Game.Components = new ComponentMgr(Game.Systems);

            Log.Msg("[GameLoop] Creating shared state manager...");
            Game.SharedState = new SharedStateMgr();

            Log.Msg("[GameLoop] Creating process manager...");
            Game.Processes = new ProcessMgr();

            Log.Msg("[GameLoop] Creating audio manager...");
            Game.Audio = new AudioMgr();

            Application.targetFrameRate = m_TargetFramerate;

            Log.Msg("[GameLoop] Loading config vars...");
            ConfigVar.ReadAllFromResources();
            ConfigVar.ReadUserFromPlayerPrefs();

            // find all pre-boot
            foreach (var entrypoint in Reflect.FindMethods<InvokePreBootAttribute>(ReflectionCache.UserAssemblies, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)) {
                entrypoint.Info.Invoke(null, null);
            }

            enabled = false;
            useGUILayout = false;
            StartCoroutine(DelayedBoot());
        }

        private void Start() {
            Log.Msg("[GameLoop] Boot finished");
            SetCurrentPhase(GameLoopPhase.Booted);
            Game.Systems.ProcessInitQueue();
            FlushQueue(s_OnBootQueue);

            FinishCallbackRegistration();

            // find all boot
            foreach (var entrypoint in Reflect.FindMethods<InvokeOnBootAttribute>(ReflectionCache.UserAssemblies, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)) {
                entrypoint.Info.Invoke(null, null);
            }
        }

        private void FinishCallbackRegistration() {
            Canvas.preWillRenderCanvases += OnPreCanvasRender;

            CameraHelper.AddOnPreCull(this);
            CameraHelper.AddOnPreRender(this);
            CameraHelper.AddOnPostRender(this);

            Application.onBeforeRender += OnBeforeApplicationRender;

#if !UNITY_EDITOR
            StartCoroutine(EndOfFrameCoroutine());
#endif // !UNITY_EDITOR
            StartCoroutine(LateFixedUpdateRoutine());
        }

        private void OnDestroy() {
            if (!m_Initialized) {
                return;
            }

            if (s_CurrentPhase != GameLoopPhase.Shutdown) {
                OnApplicationQuit();
            }
            Canvas.preWillRenderCanvases -= OnPreCanvasRender;
            Frame.DestroyAllocator();
            s_Initialized = m_Initialized = false;
        }

        private void OnApplicationQuit() {
            if (!m_Initialized) {
                return;
            }

            Log.Msg("[GameLoop] Shutting down...");
            SetCurrentPhase(GameLoopPhase.Shutdown);
            OnShutdown.Invoke();

            if (Game.Events != null) {
                Log.Msg("[GameLoop] Shutting down events...");
                Game.Events.Clear();
                Game.SetEventDispatcher(null);
            }

            Application.onBeforeRender -= OnBeforeApplicationRender;

            CameraHelper.RemoveOnPreCull(this);
            CameraHelper.RemoveOnPreRender(this);
            CameraHelper.RemoveOnPostRender(this);

            Log.Msg("[GameLoop] Shutting down audio manager...");
            Game.Audio.Shutdown();
            Game.Audio = null;

            Log.Msg("[GameLoop] Shutting down process manager...");
            Game.Processes.Shutdown();
            Game.Processes = null;

            Log.Msg("[GameLoop] Shutting down systems manager...");
            Game.Systems.Shutdown();
            Game.Systems = null;

            Log.Msg("[GameLoop] Shutting down shared state manager...");
            Game.SharedState.Shutdown();
            Game.SharedState = null;

            Log.Msg("[GameLoop] Shutting down component manager...");
            Game.Components.Shutdown();
            Game.Components = null;
        }

        private void FixedUpdate() {
            Frame.UnscaledDeltaTime = Time.deltaTime;
            Frame.DeltaTime = Frame.UnscaledDeltaTime * s_TimeScale;

            HandlePreUpdate();
            SetCurrentPhase(GameLoopPhase.FixedUpdate);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.FixedUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Processes.FixedUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnFixedUpdate.Invoke(Frame.DeltaTime);
            }
        }

        private void LateFixedUpdate() {
            SetCurrentPhase(GameLoopPhase.LateFixedUpdate);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.LateFixedUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Processes.LateFixedUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnLateFixedUpdate.Invoke(Frame.DeltaTime);
            }
        }

        private void Update() {
            Frame.UnscaledDeltaTime = Time.deltaTime;
            Frame.DeltaTime = Frame.UnscaledDeltaTime * s_TimeScale;

            // just in case we didn't have a FixedUpdate this frame
            HandlePreUpdate();

            // time-scaled update
            SetCurrentPhase(GameLoopPhase.Update);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.Update(Frame.DeltaTime, s_UpdateMask);
                Game.Processes.Update(Frame.DeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnUpdate.Invoke(Frame.DeltaTime);
            }

            // unscaled update
            SetCurrentPhase(GameLoopPhase.UnscaledUpdate);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.UnscaledUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Processes.UnscaledUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnUnscaledUpdate.Invoke(Frame.UnscaledDeltaTime);
            }

            // flush event queue
            Game.Events?.Flush();
        }

        private void LateUpdate() {
            // time-scaled late update
            SetCurrentPhase(GameLoopPhase.LateUpdate);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.LateUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Processes.LateUpdate(Frame.DeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnLateUpdate.Invoke(Frame.DeltaTime);
            }

            // unscaled late update
            SetCurrentPhase(GameLoopPhase.UnscaledLateUpdate);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.UnscaledLateUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Processes.UnscaledLateUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Components.Unlock();
                OnUnscaledLateUpdate.Invoke(Frame.UnscaledDeltaTime);
            }

            Game.Audio.Update(Frame.UnscaledDeltaTime);

            // flush event queue
            Game.Events?.Flush();

            FlushQueue(s_AfterLateUpdateQueue);

            m_ReadyForRender = true;
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

        void ICameraPreCullCallback.OnCameraPreCull(Camera camera, CameraCallbackSource source) {
            if (!m_ReadyForRender) {
                return;
            }

            SetCurrentPhase(GameLoopPhase.PreCull);
        }

        void ICameraPreRenderCallback.OnCameraPreRender(Camera camera, CameraCallbackSource source) {
            if (!m_ReadyForRender) {
                return;
            }

            SetCurrentPhase(GameLoopPhase.PreRender);
        }

        void ICameraPostRenderCallback.OnCameraPostRender(Camera camera, CameraCallbackSource source) {
            if (!m_ReadyForRender) {
                return;
            }

            SetCurrentPhase(GameLoopPhase.PostRender);
        }

        private void OnBeforeApplicationRender() {
#if UNITY_EDITOR
            if (EditorApplication.isPaused) {
                return;
            }
#endif // UNITY_EDITOR
            SetCurrentPhase(GameLoopPhase.ApplicationPreRender);
            if (!IsPaused()) {
                Game.Components.Lock();
                Game.Systems.ApplicationPreRender(Frame.DeltaTime, s_UpdateMask);
                Game.Components.Unlock();
            }
        }

        private IEnumerator DelayedBoot() {
            yield return s_EndOfFrame;
            enabled = true;
        }

        private IEnumerator LateFixedUpdateRoutine() {
            object fixedUpdate = new WaitForFixedUpdate();
            while(true) {
                yield return fixedUpdate;
                LateFixedUpdate();
            }
        }

        static private IEnumerator EndOfFrameCoroutine() {
            while (true) {
                yield return s_EndOfFrame;
                OnEndOfFrame();
            }
        }

        #endregion // Unity Events

        #region Handlers

        static private void HandlePreUpdate() {
            if (s_PrevUpdateFrameIndex != Frame.Index) {
                ReportPerfTimings(s_TimeProfiling);
                s_TimeProfiling.Clear();

                SetCurrentPhase(GameLoopPhase.DebugUpdate);
                s_PrevUpdateFrameIndex = Frame.Index;
                Frame.UnscaledDeltaTime = Time.unscaledDeltaTime;
                DequeueNextValues();
                FlushQueue(s_FrameStartQueue);

                FlushQueue(s_OnBootQueue);

                // DEBUG UPDATE
                Game.Components.Lock();
                Game.Systems.DebugUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Processes.DebugUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                Game.Components.Unlock();

                OnDebugUpdate.Invoke(Frame.UnscaledDeltaTime);
                Game.Audio.PreUpdate(Frame.UnscaledDeltaTime);

                // PRE UPDATE

                SetCurrentPhase(GameLoopPhase.PreUpdate);
                Frame.UnscaledDeltaTime = Time.deltaTime;
                DequeueNextValues();

                if (!IsPaused()) {
                    Game.Components.Lock();
                    Game.Systems.PreUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                    Game.Processes.PreUpdate(Frame.UnscaledDeltaTime, s_UpdateMask);
                    Game.Components.Unlock();

                    OnPreUpdate.Invoke(Frame.UnscaledDeltaTime);
                }

                DequeueNextValues();

                m_ReadyForRender = false;
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
            DequeueNextValues();
            Game.Processes.FrameAdvanced();
            OnFrameAdvance.Invoke(Frame.Index);
            m_ReadyForRender = false;
        }

        static private void DequeueNextValues() {
            s_UpdateMask = s_QueuedUpdateMask;
            s_TimeScale = s_QueuedTimeScale;
            Frame.DeltaTime = Frame.UnscaledDeltaTime * s_TimeScale;
        }

        static private unsafe void ReportPerfTimings(PhaseTiming timing) {
            // TODO: Implement
            //using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
            //    psb.Builder.Append("[GameLoop] PreUpdate ").AppendNoAlloc(timing.Duration[0]).Append("\n FixedUpdate ").AppendNoAlloc(timing.Duration[1])
            //        .Append("\n Update ").AppendNoAlloc(timing.Duration[2] + timing.Duration[3])
            //        .Append("\n LateUpdate ").AppendNoAlloc(timing.Duration[4] + timing.Duration[5])
            //        .Append("\n CanvasPreRender ").AppendNoAlloc(timing.Duration[6])
            //        .Append("\n CameraPreCull ").AppendNoAlloc(timing.Duration[7]).Append("\n CameraPreRender ").AppendNoAlloc(timing.Duration[8])
            //        .Append("\n CameraPostRender ").AppendNoAlloc(timing.Duration[9])
            //        .Append("\n FrameAdvanced ").AppendNoAlloc(timing.Duration[10]);
            //    Log.Msg(psb.Builder.Flush());
            //}
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
            if (s_CurrentPhase != phase) {
                s_TimeProfiling.MarkEnd(s_CurrentPhase);
                s_CurrentPhase = phase;
                s_TimeProfiling.MarkStart(phase);
                //Log.Msg("[GameLoop] Entering phase '{0}' on frame {1}", phase, Frame.Index);
            }
        }

        /// <summary>
        /// Is the game loop currently executing pre-update, fixed update, or late update.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsUpdating() {
            return PhaseInRange(s_CurrentPhase, GameLoopPhase.DebugUpdate, GameLoopPhase.UnscaledLateUpdate);
        }

        /// <summary>
        /// Is the game loop currently executing rendering steps.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsRendering() {
            return PhaseInRange(s_CurrentPhase, GameLoopPhase.PreCull, GameLoopPhase.PostRender);
        }

        /// <summary>
        /// Is the game loop currently executing the given phase.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPhase(GameLoopPhase phase) {
            return s_CurrentPhase == phase;
        }

        /// <summary>
        /// Is the game loop currently executing any of the given phases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPhase(GameLoopPhaseMask phaseMask) {
            return ((GameLoopPhaseMask) (1u << (int) s_CurrentPhase) & phaseMask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal bool PhaseInRange(GameLoopPhase phase, GameLoopPhase start, GameLoopPhase end) {
            return phase >= start && phase <= end;
        }

        #endregion // Phase

        #region Update Mask

        /// <summary>
        /// Mask of all process and system categories allowed to update.
        /// </summary>
        static public int UpdateMask { get { return s_UpdateMask; } }

        /// <summary>
        /// Queues updates for specific categories to be suspended.
        /// </summary>
        static public void SuspendUpdates(int suspendMask) {
            s_QueuedUpdateMask &= ~suspendMask;
        }

        /// <summary>
        /// Returns if the any update category of the given mask is suspended.
        /// </summary>
        static public bool IsSuspended(int updateMask) {
            return (s_UpdateMask & updateMask) != updateMask;
        }

        /// <summary>
        /// Queues updates for specific categories to be resumed.
        /// </summary>
        static public void ResumeUpdates(int resumeMask) {
            s_QueuedUpdateMask |= resumeMask;
        }

        #endregion // Update Mask

        #region Time Scale

        /// <summary>
        /// Custom GameLoop timescale.
        /// </summary>
        static public float TimeScale {
            get { return s_TimeScale; }
            set { s_QueuedTimeScale = value; }
        }

        #endregion // Time Scale

        #region Pausing

        /// <summary>
        /// Returns if the game loop is paused.
        /// </summary>
        static public bool IsPaused() {
            return s_DebugPause;
        }

        /// <summary>
        /// Sets the debug pause value.
        /// </summary>
        static internal void SetDebugPause(bool paused) {
            s_DebugPause = paused;
        }

        #endregion // Pausing
    }

    /// <summary>
    /// Game loop phase.
    /// </summary>
    [LabeledEnum(false)]
    public enum GameLoopPhase : uint {
        [Label("None")] None,
        [Label("Booted")] Booted,

        [Label("Debug Update")] DebugUpdate,
        [Label("Pre Update")] PreUpdate,
        [Label("Fixed Update")] FixedUpdate,
        [Label("Late Fixed Update")] LateFixedUpdate,
        [Label("Update")] Update,
        [Label("Unscaled Update")] UnscaledUpdate,
        [Label("Late Update")] LateUpdate,
        [Label("Unscaled Late Update")] UnscaledLateUpdate,

        [Label("Canvas Pre-Render")] CanvasPreRender,
        [Label("Application Pre-Render")] ApplicationPreRender,

        [Label("Pre-Cull")] PreCull,
        [Label("Pre-Render")] PreRender,
        [Label("Post-Render")] PostRender,

        [Label("Frame Advanced")] FrameAdvance,

        [Label("Shutdown")] Shutdown,
    }

    /// <summary>
    /// Game loop phase mask.
    /// </summary>
    [Flags]
    public enum GameLoopPhaseMask : uint {
        None = 0,
        Booted = 1 << (int) GameLoopPhase.Booted,

        DebugUpdate = 1 << (int) GameLoopPhase.DebugUpdate,
        PreUpdate = 1 << (int) GameLoopPhase.PreUpdate,
        FixedUpdate = 1 << (int) GameLoopPhase.FixedUpdate,
        LateFixedUpdate = 1 << (int) GameLoopPhase.LateFixedUpdate,
        Update = 1 << (int) GameLoopPhase.Update,
        UnscaledUpdate = 1 << (int) GameLoopPhase.UnscaledUpdate,
        LateUpdate = 1 << (int) GameLoopPhase.LateUpdate,
        UnscaledLateUpdate = 1 << (int) GameLoopPhase.UnscaledLateUpdate,

        CanvasPreRender = 1 << (int) GameLoopPhase.CanvasPreRender,
        ApplicationPreRender = 1 << (int) GameLoopPhase.ApplicationPreRender,

        PreCull = 1 << (int) GameLoopPhase.PreCull,
        PreRender = 1 << (int) GameLoopPhase.PreRender,
        PostRender = 1 << (int) GameLoopPhase.PostRender,

        FrameAdvance = 1 << (int) GameLoopPhase.FrameAdvance,

        Shutdown = 1 << (int) GameLoopPhase.Shutdown,
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