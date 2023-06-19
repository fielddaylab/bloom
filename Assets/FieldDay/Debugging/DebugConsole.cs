#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using BeauUtil;
using UnityEngine;
using System.Diagnostics;
using BeauUtil.Debugger;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using FieldDay.Systems;
using BeauRoutine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

using Debug = UnityEngine.Debug;

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug console.
    /// </summary>
    [DefaultExecutionOrder(-9999)]
    public sealed class DebugConsole : MonoBehaviour {
        static private DMInfo s_RootMenu;

        #region Events

        /// <summary>
        /// Invoked when time scale is updated.
        /// </summary>
        static public readonly CastableEvent<bool> OnPauseUpdated = new CastableEvent<bool>();

        /// <summary>
        /// Invoked when time scale is updated.
        /// </summary>
        static public readonly CastableEvent<float> OnTimeScaleUpdated = new CastableEvent<float>();

        #endregion // Events

        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleKey = KeyCode.BackQuote;
        [SerializeField] private CanvasGroup m_MinimalGroup = null;
        [SerializeField] private ConsoleTimeDisplay m_TimeDisplay = null;

        [Header("Debug Menu")]
        [SerializeField] private DMMenuUI m_DebugMenus = null;

        #endregion // Inspector

        [NonSerialized] private float m_TimeScale = 1;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private bool m_MinimalVisible;
        [NonSerialized] private bool m_VisibilityWhenDebugMenuOpened;
        [NonSerialized] private bool m_MenuOpen;
        [NonSerialized] private bool m_MenuUIInitialized;

        private void Awake() {
            GameLoop.OnPreUpdate.Register(OnPreUpdate);
            GameLoop.QueuePreUpdate(LoadMenu);
        }

        private void Start() {
            m_DebugMenus.gameObject.SetActive(false);
            m_Canvas.enabled = false;
            m_MinimalGroup.blocksRaycasts = false;
        }

        private void OnDestroy() {
            GameLoop.OnPreUpdate.Deregister(OnPreUpdate);
        }

        private void OnPreUpdate() {
            CheckTimeInput();
            UpdateMinimalLayer();
            UpdateMenu();
        }

        #region Timescale

        private void CheckTimeInput() {
            if (Input.GetKey(KeyCode.LeftShift)) {
                if (Input.GetKeyDown(KeyCode.Minus)) {
                    UpdateTimescale(m_TimeScale / 2);
                } else if (Input.GetKeyDown(KeyCode.Equals)) {
                    if (m_TimeScale * 2 < 100) {
                        UpdateTimescale(m_TimeScale * 2);
                    }
                } else if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    UpdateTimescale(1);
                }
            }
        }

        private void UpdateTimescale(float timeScale) {
            m_TimeScale = timeScale;
            if (!m_Paused) {
                Time.timeScale = timeScale;
                OnTimeScaleUpdated.Invoke(timeScale);
            }

            m_TimeDisplay.UpdateTimescale(m_TimeScale);
        }

        private void SetPaused(bool paused) {
            if (m_Paused == paused) {
                return;
            }

            m_Paused = paused;
            Routine.Settings.Paused = paused;
            OnPauseUpdated.Invoke(paused);

            if (paused) {
                Time.timeScale = 0;
                m_TimeDisplay.UpdateStateLabel("PAUSED");
                OnTimeScaleUpdated.Invoke(0);
            } else {
                Time.timeScale = m_TimeScale;
                m_TimeDisplay.UpdateStateLabel("PLAYING");
                OnTimeScaleUpdated.Invoke(m_TimeScale);
            }
        }

        #endregion // Timescale

        #region Menu

        private void UpdateMenu() {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                SetMenuVisible(!m_MenuOpen);
            }

            if (m_DebugMenus.isActiveAndEnabled) {
                m_DebugMenus.UpdateElements();

                if (Input.GetMouseButtonDown(1)) {
                    m_DebugMenus.TryPopMenu();
                } else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) {
                    m_DebugMenus.TryPreviousPage();
                } else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) {
                    m_DebugMenus.TryNextPage();
                }
            }
        }

        static private void LoadMenu() {
            s_RootMenu = new DMInfo("Debug", 16);

            foreach (var pair in Reflect.FindMethods<DebugMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
                if (pair.Info.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                if (pair.Info.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) pair.Info.Invoke(null, Array.Empty<object>());

                if (menu != null) {
                    int existingIdx = s_RootMenu.Elements.FindIndex((e, l) => e.Type == DMElementType.Submenu && e.Submenu.Submenu.Header.Label == l, menu.Header.Label);
                    if (existingIdx >= 0) {
                        DMInfo existing = s_RootMenu.Elements[existingIdx].Submenu.Submenu;
                        existing.AddDivider();
                        existing.MinimumWidth = Math.Max(existing.MinimumWidth, menu.MinimumWidth);
                        foreach (var element in menu.Elements) {
                            existing.Elements.PushBack(element);
                        }
                    } else {
                        s_RootMenu.AddSubmenu(menu);
                    }
                }
            }

            s_RootMenu.Elements.Sort((a, b) => a.Label.CompareTo(b.Label));
        }

        private void SetMenuVisible(bool visible) {
            if (m_MenuOpen == visible) {
                return;
            }

            m_MenuOpen = visible;
            if (visible) {
                if (!m_MenuUIInitialized) {
                    m_DebugMenus.GotoMenu(s_RootMenu);
                    m_MenuUIInitialized = true;
                }
                m_VisibilityWhenDebugMenuOpened = m_MinimalVisible;
                SetMinimalVisible(true);
                m_DebugMenus.gameObject.SetActive(true);
            } else {
                SetMinimalVisible(m_VisibilityWhenDebugMenuOpened);
            }
        }

        #endregion // Menu

        #region Minimal Layer

        private void UpdateMinimalLayer() {
            if (Input.GetKeyDown(m_ToggleKey)) {
                SetMinimalVisible(!m_MinimalVisible);
            }
        }

        private void SetMinimalVisible(bool visible) {
            if (m_MinimalVisible == visible) {
                return;
            }

            m_MinimalVisible = visible;
            m_MinimalGroup.alpha = visible ? 1 : 0;
            m_MinimalGroup.blocksRaycasts = visible;
            m_Canvas.enabled = visible;

            if (!visible) {
                SetMenuVisible(false);
            }
        }

        #endregion // Minimal Layer
    }

    /// <summary>
    /// Attribute marking a static method to be invoked to create a root debug menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DebugMenuFactoryAttribute : PreserveAttribute { }
}