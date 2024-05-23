using System;
using System.Runtime.InteropServices;
using BeauUtil;
using FieldDay;
using FieldDay.HID;
using FieldDay.Scripting;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using Zavala.Advisor;
using Zavala.Data;
using Zavala.Economy;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    public class ZavalaGame : Game {

        public const int SimulationUpdateMask = 1 << 0;

        /// <summary>
        /// Global timer state.
        /// </summary>
        [SharedStateReference] static public SimTimeState SimTime { get; private set; }

        /// <summary>
        /// Global grid.
        /// </summary>
        [SharedStateReference] static public SimGridState SimGrid { get; private set; }

        /// <summary>
        /// Global world state.
        /// </summary>
        [SharedStateReference] static public SimWorldState SimWorld { get; private set; }

        /// <summary>
        /// Save state buffer.
        /// </summary>
        static public SaveMgr SaveBuffer { get; private set; }

        /// <summary>
        /// Event system.
        /// </summary>
        static public new EventDispatcher<object> Events { get; set; }

        [InvokePreBoot]
        static private void OnPreBoot() {
            SimAllocator.Initialize(700 * Unsafe.KiB); // 700KiB simulation allocation buffer
            Events = new EventDispatcher<object>();
            Game.SetEventDispatcher(Events);
            SaveBuffer = new SaveMgr();

            Game.Rendering.EnableMinimumAspectClamping(1024, 660);

            UserSettings settings = new UserSettings();
            Game.SharedState.Register(settings);

#if UNITY_EDITOR
            if (SceneHelper.ActiveScene().BuildIndex == 0) {
                settings.PlayerCode = PlayerPrefs.GetString("LatestPlayerCode", null);
            } else {
                settings.PlayerCode = null;
            }
#else
            settings.PlayerCode = PlayerPrefs.GetString("LatestPlayerCode", null);
#endif // UNITY_EDITOR

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;

            GameLoop.OnDebugUpdate.Register(() => {
                if (Input.IsKeyComboPressed(ModifierKeyCode.LCtrl, KeyCode.Space)) {
                    Input.ConsumeAllInputForFrame();
                    ScriptUtility.Trigger(GameTriggers.TutorialSkipped);
                }
            });

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

            GameLoop.OnShutdown.Register(OnShutdown);
            Game.Scenes.OnSceneUnload.Register(OnSceneUnload);
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            CursorUtility.HideCursor();
#if UNITY_EDITOR
            Profiler.enabled = false;
#endif // UNITY_EDITOR
        }

        static private void OnSceneUnload() {
            SimAllocator.Reset();
        }

        static private void OnShutdown() {
            SimAllocator.Destroy();
            SaveBuffer.Free();
        }
    }

    static public class ZavalaColors {
        static public readonly Color InterfaceBackgroundLightest = Colors.Hex("FFFBE3");
        static public readonly Color InterfaceBackgroundLight = Colors.Hex("F9F5E0");
        static public readonly Color InterfaceBackgroundMid = Colors.Hex("EFE6AB");

        static public readonly Color ShopItemDefault = Colors.Hex("7BE3EB");
        static public readonly Color ShopItemSelected = Colors.Hex("FFE27D");

        static public readonly Color TopBarPopupPlus = Colors.Hex("DFCD29");
        static public readonly Color TopBarPopupMinus = Colors.Hex("E07156");

    }
}