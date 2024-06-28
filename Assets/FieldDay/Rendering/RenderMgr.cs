#if UNITY_2019_1_OR_NEWER
#define USE_SRP
#endif // UNITY_2019_1_OR_NEWER

#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USE_URP
#endif // UNITY_2019_1_OR_NEWER

using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using UnityEngine;
using UnityEngine.Rendering;

#if USE_URP
using UnityEngine.Rendering.Universal;
#endif // USE_URP

namespace FieldDay.Rendering {
    public sealed class RenderMgr : ICameraPreRenderCallback {
        private bool m_LastKnownFullscreen;
        private Resolution m_LastKnownResolution;

        private Camera m_PrimaryCamera;
        private Camera m_FallbackCamera;

        private RingBuffer<CameraClampToVirtualViewport> m_ClampedViewportCameras = new RingBuffer<CameraClampToVirtualViewport>(2, RingBufferMode.Expand);
        private Rect m_VirtualViewport = new Rect(0, 0, 1, 1);

        private float m_MinAspect;
        private float m_MaxAspect;
        private bool m_HasLetterboxing;

        private bool m_ShouldCheckFallback = true;
        private bool m_UsingFallback = false;
        private ushort m_LastLetterboxFrameRendered = Frame.InvalidIndex;

        #region Callbacks

        public readonly CastableEvent<bool> OnFullscreenChanged = new CastableEvent<bool>(2);
        public readonly CastableEvent<Resolution> OnResolutionChanged = new CastableEvent<Resolution>(2);

        #endregion // Callbacks

        #region Events

        internal void Initialize() {
            GameLoop.OnCanvasPreRender.Register(OnCanvasPreUpdate);
            GameLoop.OnApplicationPreRender.Register(OnApplicationPreRender);
            GameLoop.OnFrameAdvance.Register(OnApplicationPostRender);

            Game.Scenes.OnAnySceneUnloaded.Register(OnSceneLoadUnload);
            Game.Scenes.OnAnySceneEnabled.Register(OnSceneLoadUnload);
            Game.Scenes.OnSceneReady.Register(OnSceneLoadUnload);

            CameraHelper.AddOnPreRender(this);
        }

        internal void LateInitialize() {
            Game.Gui.OnPrimaryCameraChanged.Register(OnGuiCameraChanged);
            OnGuiCameraChanged(Game.Gui.PrimaryCamera);
        }

        internal void PollScreenSettings() {
            bool fullscreen = ScreenUtility.GetFullscreen();
            if (m_LastKnownFullscreen != fullscreen) {
                m_LastKnownFullscreen = fullscreen;
                OnFullscreenChanged.Invoke(fullscreen);
            }

            Resolution resolution = ScreenUtility.GetResolution();
            if (resolution.width != m_LastKnownResolution.width || resolution.height != m_LastKnownResolution.height || resolution.refreshRate != m_LastKnownResolution.refreshRate) {
                m_LastKnownResolution = resolution;
                OnResolutionChanged.Invoke(resolution);
            }
        }

        internal void Shutdown() {
            GameLoop.OnCanvasPreRender.Deregister(OnCanvasPreUpdate);
            GameLoop.OnApplicationPreRender.Deregister(OnApplicationPreRender);
            GameLoop.OnFrameAdvance.Deregister(OnApplicationPostRender);

            Game.Scenes.OnAnySceneUnloaded.Deregister(OnSceneLoadUnload);
            Game.Scenes.OnAnySceneEnabled.Deregister(OnSceneLoadUnload);
            Game.Scenes.OnSceneReady.Deregister(OnSceneLoadUnload);

            CameraHelper.RemoveOnPreRender(this);

            OnResolutionChanged.Clear();
            OnFullscreenChanged.Clear();
        }

        #endregion // Events

        #region World Camera

        public Camera PrimaryCamera {
            get { return m_PrimaryCamera; }
        }

        public void SetPrimaryCamera(Camera camera) {
            if (m_PrimaryCamera != null) {
                Log.Warn("[RenderMgr] Primary world camera already set to '{0}' - make sure to deregister it first", m_PrimaryCamera);
            }
            m_PrimaryCamera = camera;
            m_ShouldCheckFallback = true;
            Log.Msg("[RenderMgr] Assigned primary world camera as '{0}'", camera);
        }

        public void RemovePrimaryCamera(Camera camera) {
            if (camera == null || m_PrimaryCamera != camera) {
                return;
            }

            m_PrimaryCamera = null;
            m_ShouldCheckFallback = true;
            Log.Msg("[RenderMgr] Removed primary world camera");
        }

        #endregion // World Camera

        #region Clamped Viewport

        public void EnableAspectClamping(int width, int height) {
            m_MinAspect = (float) width / height;
            m_MaxAspect = m_MinAspect;
        }

        public void EnableMinimumAspectClamping(int width, int height) {
            m_MinAspect = (float) width / height;
            m_MaxAspect = float.MaxValue;
        }

        public void EnableAspectClamping(Vector2Int min, Vector2Int max) {
            m_MinAspect = (float) min.x / min.y;
            m_MaxAspect = (float) max.x / max.y;
        }

        public void DisableAspectClamping() {
            m_MinAspect = m_MaxAspect = 0;
            m_VirtualViewport = new Rect(0, 0, 1, 1);
        }

        public Rect VirtualViewport {
            get { return m_VirtualViewport; }
        }

        public void AddClampedViewportCamera(CameraClampToVirtualViewport camera) {
            Assert.NotNull(camera);
            m_ClampedViewportCameras.PushBack(camera);
        }

        public void RemoveClampedViewportCamera(CameraClampToVirtualViewport camera) {
            Assert.NotNull(camera);
            m_ClampedViewportCameras.FastRemove(camera);
        }

        #endregion // Clamped Viewport

        #region Fallback

        public bool HasFallbackCamera() {
            return m_FallbackCamera;
        }

        public void CreateDefaultFallbackCamera() {
            if (m_FallbackCamera) {
                Log.Warn("[RenderMgr] Fallback camera already in place.");
                return;
            }

            GameObject go = new GameObject("[RenderMgr Fallback]");
            Camera camera = go.AddComponent<Camera>();
            GameObject.DontDestroyOnLoad(go);
            go.SetActive(false);

            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.orthographicSize = 0.5f;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor | CameraClearFlags.Depth;
            camera.depth = -100;

#if USE_URP
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.requiresDepthTexture = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresColorTexture = false;
            data.stopNaN = false;
            data.dithering = false;
#endif // USE_URP

            m_FallbackCamera = camera;

            Log.Msg("[RenderMgr] Created default fallback camera");

            OnGuiCameraChanged(Game.Gui.PrimaryCamera);
            go.SetActive(m_UsingFallback);
        }

        // TODO: SetCustomFallbackCamera

        /// <summary>
        /// Marks the "fallback camera" state as dirty.
        /// This will force it to be reevaluated before the next render.
        /// </summary>
        public void QueueFallbackCameraReevaluate() {
            m_ShouldCheckFallback = true;
        }

        #endregion // Fallback

        #region Handlers

        private void OnGuiCameraChanged(Camera uiCam) {
            if (!m_FallbackCamera) {
                return;
            }
#if USE_URP
            var data = m_FallbackCamera.GetUniversalAdditionalCameraData();
            if (uiCam != null) {
                if (!data.cameraStack.Contains(uiCam)) {
                    data.cameraStack.Add(uiCam);
                }
            } else {
                data.cameraStack.Clear();
            }
#endif // USE_URP
        }

        private void OnSceneLoadUnload() {
            m_ShouldCheckFallback = true;
        }

        private void CheckIfNeedsFallback() {
            if (!m_ShouldCheckFallback) {
                return;
            }

            bool needsFallback = !CameraUtility.AreAnyCamerasDirectlyRendering(m_FallbackCamera);
            if (Ref.Replace(ref m_UsingFallback, needsFallback)) {
                if (m_FallbackCamera) {
                    m_FallbackCamera.gameObject.SetActive(needsFallback);
                }
                Log.Msg("[RenderMgr] Fallback camera switched to {0}", needsFallback ? "ON" : "OFF");
            }
            
            m_ShouldCheckFallback = false;
        }

        private void OnCanvasPreUpdate() {
            if (m_MinAspect <= 0 || m_MaxAspect <= 0) {
                m_HasLetterboxing = false;
                return;
            }

            m_VirtualViewport = UpdateAspectRatioClamping();
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Virtual viewport is {0}", m_VirtualViewport.ToString());
            }

            for(int i = 0; i < m_ClampedViewportCameras.Count; i++) {
                ref var c = ref m_ClampedViewportCameras[i];
                Rect r = c.Viewport;
                r.x = m_VirtualViewport.x + r.x * m_VirtualViewport.width;
                r.y = m_VirtualViewport.y + r.y * m_VirtualViewport.height;
                r.width = r.width * m_VirtualViewport.width;
                r.height = r.height * m_VirtualViewport.height;
                c.Camera.rect = r;
            }

            m_HasLetterboxing = true;
        }

        private void OnApplicationPreRender() {
            CheckIfNeedsFallback();
        }

        private void OnApplicationPostRender() {
            
        }

        private Rect UpdateAspectRatioClamping() {
            float currentAspect = (float) m_LastKnownResolution.width / m_LastKnownResolution.height;
            float finalAspect = Mathf.Clamp(currentAspect, m_MinAspect, m_MaxAspect);

            float aspectW = finalAspect;
            float aspectH = 1;

            if (aspectW > currentAspect) {
                aspectH = currentAspect / finalAspect;
                aspectW = aspectH * finalAspect;
            }

            float diffX = 1 - (aspectW / currentAspect),
                diffY = 1 - (aspectH / 1);

            Rect r = default;
            r.x = diffX / 2;
            r.y = diffY / 2;
            r.width = 1 - diffX;
            r.height = 1 - diffY;

            return r;
        }

        void ICameraPreRenderCallback.OnCameraPreRender(Camera inCamera, CameraCallbackSource inSource) {
            if (m_LastLetterboxFrameRendered == Frame.Index) {
                return;
            }

            m_LastLetterboxFrameRendered = Frame.Index;

            Graphics.SetRenderTarget(null);

            if (m_UsingFallback && !m_FallbackCamera) {
                if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                    Log.Trace("[RenderMgr] Clearing backbuffer as fallback");
                }
                GL.PushMatrix();
                GL.LoadOrtho();
                GL.Clear(true, true, Color.black, 1);
                GL.PopMatrix();
            }

            if (m_HasLetterboxing && m_ClampedViewportCameras.Count > 0) {
                GL.Viewport(new Rect(0, 0, m_LastKnownResolution.width, m_LastKnownResolution.height));
                if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                    Log.Trace("[RenderMgr] Rendering letterboxing for viewport {0}", m_VirtualViewport.ToString());
                }
                CameraHelper.RenderLetterboxing(m_VirtualViewport, Color.black);
            }

            if (DebugFlags.IsFlagSet(DebuggingFlags.VisualizeEntireScreen)) {
                GL.PushMatrix();
                GL.LoadOrtho();
                GL.Viewport(new Rect(0, 0, m_LastKnownResolution.width, m_LastKnownResolution.height));
                GL.Clear(true, true, Color.magenta, 1);
                GL.PopMatrix();

                string debugText = string.Format("Screen Dimensions: {0} ({1})", m_LastKnownResolution, m_LastKnownFullscreen ? "FULLSCREEN" : "NOT FULLSCREEN");

                DebugDraw.AddViewportText(new Vector2(0.5f, 1), new Vector2(0, -8), debugText, Color.white, 0, TextAnchor.UpperCenter, DebugTextStyle.BackgroundDarkOpaque);
            }
        }

        #endregion // Handlers

        #region Debug

        private enum DebuggingFlags {
            TraceExecution,
            VisualizeEntireScreen
        }

        [EngineMenuFactory]
        static private DMInfo CreateRenderDebugMenu() {
            DMInfo info = new DMInfo("RenderMgr", 16);
            DebugFlags.Menu.AddSingleFrameFlagButton(info, DebuggingFlags.TraceExecution);
            DebugFlags.Menu.AddFlagToggle(info, DebuggingFlags.VisualizeEntireScreen);
            return info;
        }

        #endregion // Debug
    }
}