using FieldDay.Systems;
using FieldDay.SharedState;
using FieldDay.Components;
using FieldDay.Processes;

namespace FieldDay {
    /// <summary>
    /// Maintains references to game engine components.
    /// </summary>
    public class Game {
        /// <summary>
        /// ISystem manager. Maintains system updates.
        /// </summary>
        static public SystemsMgr Systems { get; internal set; }

        /// <summary>
        /// IComponentData manager. Maintains component lists.
        /// </summary>
        static public ComponentMgr Components { get; internal set; }

        /// <summary>
        /// ISharedState manager. Maintains shared state components.
        /// </summary>
        static public SharedStateMgr SharedState { get; internal set; }

        /// <summary>
        /// Process manager. Maintains process states.
        /// </summary>
        static public ProcessMgr Processes { get; internal set; }

        /// <summary>
        /// Event dispatcher. Maintains event dispatch.
        /// </summary>
        static public IEventDispatcher Events { get; internal set; }

        /// <summary>
        /// Returns if the game loop is currently shutting down.
        /// </summary>
        static public bool IsShuttingDown {
            get { return GameLoop.s_CurrentPhase == GameLoopPhase.Shutdown; }
        }

        /// <summary>
        /// Sets the current event dispatcher.
        /// </summary>
        static public void SetEventDispatcher(IEventDispatcher eventDispatcher) {
            Events = eventDispatcher;
        }
    }
}