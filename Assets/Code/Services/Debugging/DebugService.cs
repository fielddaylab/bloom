#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Diagnostics;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using UnityEngine;
using Debug = UnityEngine.Debug;
using BeauUtil.Debugger;
using System.Collections.Generic;
using TMPro;
using EasyAssetStreaming;
using EasyBugReporter;
using BeauUtil.Variants;
using System.Text;
using System.IO;
using FieldDay;
using FieldDay.Debugging;

namespace Zavala.Debugging
{
    [ServiceDependency(typeof(UnityEditor.MPE.EventService))]
    public partial class DebugService : ServiceBehaviour, IDebuggable
    {
        #if DEVELOPMENT

        #region Static

        [ServiceReference, UnityEngine.Scripting.Preserve] static private DebugService s_Instance;

        static private DMInfo s_RootMenu;

        #endregion // Static

        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleMinimalKey = KeyCode.BackQuote;
        [SerializeField, Required] private CanvasGroup m_MinimalLayer = null;
        [SerializeField, Required] private GameObject m_KeyboardReference = null;
        [SerializeField, Required] private ConsoleCamera m_DebugCamera = null;
        [SerializeField, Required] private DMMenuUI m_DebugMenu = null;
        [SerializeField, Required] private GameObject m_CameraReference = null;
        [SerializeField, Required] private TMP_Text m_StreamingDebugText = null;
        [Space]
        [SerializeField] private bool m_StartOn = true;

        #endregion // Inspector

        [NonSerialized] private bool m_MinimalOn;
        [NonSerialized] private bool m_FirstMenuToggle;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private float m_TimeScale = 1;
        [NonSerialized] private bool m_VisibilityWhenDebugMenuOpened;
        [NonSerialized] private Vector2 m_CameraCursorPivot;
        [NonSerialized] private bool m_CameraLock;

        [NonSerialized] private int m_LastKnownNetworkCount;
        [NonSerialized] private long m_LastKnownStreamingMem;
        [NonSerialized] private long m_UnlockAllLastPress;

        private readonly StringBuilder m_TextBuilder = new StringBuilder(128);

        private void LateUpdate()
        {
            CheckInput();

            if (m_DebugMenu.isActiveAndEnabled)
                m_DebugMenu.UpdateElements();
        }

        private void CheckInput()
        {
            
        }

        #region Asset Reloading

        private void OnSceneLoaded(SceneBinding inBinding, object inContext)
        {
            TryReloadAssets();
        }

        private void TryReloadAssets()
        {
            ReloadableAssetCache.TryReloadAll();
        }

        #endregion // Asset Reloading

        #region IService

        protected override void Initialize()
        {
            SceneHelper.OnSceneLoaded += OnSceneLoaded;
            // Game.Events.Register(GameEvents.ProfileLoaded, HideMenu, this);

            m_Canvas.gameObject.SetActive(true);
            transform.FlattenHierarchy();

            #if DEVELOPMENT
            RootDebugMenu();
            #endif // DEVELOPMENT

            // BugReporter.OnExceptionOrAssert((s) => {
            //     BugReporter.DumpContextToMemory(DumpFormat.Text, (d) => {
            //         UnityEngine.Debug.LogError(d.Contents);
            //     });
            // });
            // TODO: on exception, maybe log something with analytics?
        }

        protected override void Shutdown()
        {
            BugReporter.DefaultSources = null;
            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
            // Game.Events?.DeregisterAll(this);
        }

        #endregion // IService

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus(FindOrCreateMenu findOrCreate)
        {
            DMInfo loggingMenu = new DMInfo("Logging");
            loggingMenu.AddToggle("Enable Crash Handler", () => CrashHandler.Enabled, (b) => CrashHandler.Enabled = b);
            loggingMenu.AddDivider();
            RegisterLogToggle(loggingMenu, LogMask.Input);
            RegisterLogToggle(loggingMenu, LogMask.Scripting);
            RegisterLogToggle(loggingMenu, LogMask.Audio);
            RegisterLogToggle(loggingMenu, LogMask.Loading, "Loading");
            RegisterLogToggle(loggingMenu, LogMask.Camera);
            RegisterLogToggle(loggingMenu, LogMask.UI);
            RegisterLogToggle(loggingMenu, LogMask.Localization);
            yield return loggingMenu;
        }

        static private void RegisterLogToggle(DMInfo inMenu, LogMask inMask, string inName = null)
        {
            inMenu.AddToggle(inName ?? inMask.ToString(), () => IsLogging(inMask),
            (b) => {
                if (b)
                    AllowLogs(inMask);
                else
                    DisallowLogs(inMask);
            });
        }

        #endregion // IDebuggable

        #endif // DEVELOPMENT
    
        #region Logging Stuff

        #if DEVELOPMENT

        static private uint s_LoggingMask = (uint) (LogMask.DEFAULT);

        static internal void AllowLogs(LogMask inMask)
        {
            s_LoggingMask |= (uint) inMask;
        }

        static internal void DisallowLogs(LogMask inMask)
        {
            s_LoggingMask &= ~(uint) inMask;
        }

        static public bool IsLogging(LogMask inMask) { return (s_LoggingMask & (uint) inMask) != 0; }

        static public void Log(LogMask inMask, string inMessage) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage); }
        static public void Log(LogMask inMask, string inMessage, object inArg0) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0, inArg1); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0, inArg1, inArg2); }
        static public void Log(LogMask inMask, string inMessage, params object[] inParams) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inParams); }


        #else

        static internal void AllowLogs(LogMask inMask) { }
        static internal void DisallowLogs(LogMask inMask) { }
        static public bool IsLogging(LogMask inMask) { return false; }

        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, params object[] inParams) { }

        [Conditional("ALWAYS_EXCLUDE")] static public void Hide() { }

        #endif // DEVELOPMENT

        #endregion // Logging Stuff
    
        #region Debug Menu

        #if DEVELOPMENT

        static public DMInfo RootDebugMenu() { return s_RootMenu ?? (s_RootMenu = new DMInfo("Debug", 16)); }

        #endif // DEVELOPMENT

        /// <summary>
        /// Dumps the given table.
        /// </summary>
        static internal void Dump(VariantTable table, IDumpWriter writer) {
            foreach(var namedPair in table) {
                TableKeyPair keyPair = new TableKeyPair(table.Name, namedPair.Id);
                writer.KeyValue(keyPair.ToDebugString(), namedPair.Value.ToDebugString());
            }
        }

        #endregion // Debug Menu
    }
}