using FieldDay;
using FieldDay.SharedState;
using UnityEngine.Scripting;
using Zavala.Sim;

namespace Zavala {
    public class ZavalaGame : Game {

        /// <summary>
        /// Global timer state.
        /// </summary>
        [SharedStateReference] static public SimTimeState SimTime { get; private set; }

        /// <summary>
        /// Event system.
        /// </summary>
        static public new EventDispatcher<object> Events { get; set; }

        [InvokePreBoot]
        static private void OnPreBoot() {
            SimAllocator.Initialize(4 * 1024 * 1024); // 4 MB simulation allocation buffer
            Events = new EventDispatcher<object>();
            Game.Events = Events;

            GameLoop.OnShutdown.Register(OnShutdown);
        }

        static private void OnShutdown() {
            SimAllocator.Destroy();
        }
    }
}