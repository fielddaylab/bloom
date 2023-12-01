using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.Scripting;
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
        /// Event system.
        /// </summary>
        static public new EventDispatcher<object> Events { get; set; }

        [InvokePreBoot]
        static private void OnPreBoot() {
            SimAllocator.Initialize(4 * Unsafe.MiB); // 6 MB simulation allocation buffer
            Events = new EventDispatcher<object>();
            Game.SetEventDispatcher(Events);

            GameLoop.OnShutdown.Register(OnShutdown);
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            Scenes.OnMainSceneReady.Register(() => {
                if (ScriptUtility.Runtime) {
                    ScriptUtility.Trigger(GameTriggers.GameBooted);
                }
            });
        }

        static private void OnShutdown() {
            SimAllocator.Destroy();
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