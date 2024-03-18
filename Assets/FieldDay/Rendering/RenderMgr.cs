using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    public sealed class RenderMgr {
        private bool m_LastKnownFullscreen;
        private Resolution m_LastKnownResolution;

        #region Callbacks

        public readonly CastableEvent<bool> OnFullscreenChanged = new CastableEvent<bool>(2);
        public readonly CastableEvent<Resolution> OnResolutionChanged = new CastableEvent<Resolution>(2);

        #endregion // Callbacks

        #region Events

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
            OnResolutionChanged.Clear();
            OnFullscreenChanged.Clear();
        }

        #endregion // Events
    }
}