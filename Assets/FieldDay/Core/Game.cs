using BeauUtil;
using System;
using System.Collections;
using UnityEngine;
using FieldDay.Systems;
using FieldDay.SharedState;

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
        /// ISharedState manager. Maintains shared state components.
        /// </summary>
        static public SharedStateMgr SharedState { get; internal set; }

        /// <summary>
        /// Event dispatcher. Maintains event dispatch.
        /// </summary>
        static public IEventDispatcher Events { get; set; }

        /// <summary>
        /// Returns if the game loop is currently shutting down.
        /// </summary>
        static public bool IsShuttingDown {
            get { return GameLoop.s_CurrentPhase == GameLoopPhase.Shutdown; }
        }
    }
}