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
            foreach (var component in m_Components) {
                CheckStressCap(component.Primary);
            }
        }
 
        /// <summary>
        /// Registers event listeners for the given StressableActor's EventResponses
        /// </summary>
        protected override void OnComponentAdded(StressableActor actor, ActorTimer timer) {
            foreach (KeyValuePair<StringHash32, Action<int>> pair in actor.EventResponses) {
                // listen for stressor events, increase actor stress when fired
                // TODO: How to get context from the triggered events?
                ZavalaGame.Events.Register(pair.Key, pair.Value);
                Log.Msg("[StressSystem] Event registered: {0}", pair.Key.ToDebugString());
            }
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
                Log.Msg("[StressSystem] Stress cap of {0} reached on {1}, now {2}", actor.StressCap, actor.transform.name, actor.CurrentStress);
                DebugDraw.AddWorldText(actor.transform.position, "Stress cap of "+actor.StressCap+"reached!", Color.red, 3);
            }
        }
    }
}