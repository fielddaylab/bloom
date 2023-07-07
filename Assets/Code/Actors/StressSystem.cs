using System;
using System.Collections.Generic;
using System.Numerics;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Economy;

namespace Zavala.Actors {
    public sealed class StressSystem : ComponentSystemBehaviour<StressableActor, ActorTimer> {
        public override void ProcessWorkForComponent(StressableActor actor, ActorTimer timer, float deltaTime) {
            if (!timer.Timer.HasAdvanced()) {
                return;
            }
            CheckStressCap(actor);
        }
 
        /// <summary>
        /// Registers event listeners for the given StressableActor's EventResponses
        /// </summary>
        protected override void OnComponentAdded(StressableActor actor, ActorTimer timer) {
            foreach (KeyValuePair<StringHash32, Action> pair in actor.EventResponses) {
                // listen for stressor events, increase actor stress when fired
                // TODO: How to get context from the triggered events?
                ZavalaGame.Events.Register(pair.Key, pair.Value);
                Log.Msg("[StressSystem] Event registered: {0}", pair.Key.ToDebugString());
            }
        }

        // TODO: check if two tiles are adjacent
        private bool TilesAdjacent(int tile1, int tile2) {
            return false;
        }

        /// <summary>
        /// Checks the stress levels of the given actor and triggers its stress cap action if necessary.
        /// </summary>
        /// <param name="actor">Actor to check stress for</param>
        private void CheckStressCap(StressableActor actor) {
            if (actor.CurrentStress >= actor.StressCap) {
                actor.StressCapAction.Invoke();
                if (actor.ResetStressOnCap) {
                    actor.CurrentStress = 0;
                }
                Log.Msg("[StressSystem] Stress cap reached on {0}", actor.transform.name);
                DebugDraw.AddWorldText(actor.transform.position, "D:", Color.red, 3);
            }
        }
    }
}