using System;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Scripting {
    [DisallowMultipleComponent]
    public sealed class EventActor : BatchedComponent, ILeafActor {
        public int MaxQueuedEvents = 3;
        public SerializedHash32 Id;
        public SerializedHash32 Class;

        [Header("Event Display")]
        public Vector3 EventDisplayOffset;

        [NonSerialized] public UIAlert DisplayingEvent;

        // not serialized

        public RingBuffer<EventActorTrigger> QueuedTriggers = new RingBuffer<EventActorTrigger>(8, RingBufferMode.Expand);
        public RingBuffer<EventActorQueuedEvent> QueuedEvents = new RingBuffer<EventActorQueuedEvent>(8, RingBufferMode.Expand);

        protected override void OnDisable() {
            QueuedTriggers.Clear();
            QueuedEvents.Clear();

            base.OnDisable();
        }

        #region Leaf

        StringHash32 ILeafActor.Id => Id;

        VariantTable ILeafActor.Locals => null;

        #endregion // Leaf

        #region Unity Events

        private void OnDestroy() {
            if (!Game.IsShuttingDown) {
                EventActorUtility.DeregisterActor(this);
            }
        }

        #endregion // Unity Events
    }

    public struct EventActorTrigger {
        public StringHash32 EventId;
        public NamedVariant Argument;
        public EventActorAlertType Alert;
        public int TileIndex;
    }

    public struct EventActorQueuedEvent {
        public StringHash32 ScriptId;
        public StringHash32 TypeId;
        public NamedVariant Argument;
        public EventActorAlertType Alert;
        public int TileIndex;
    }

    public enum EventActorAlertType {
        None,
        Bloom,
        ExcessRunoff,
        DieOff,
        CritImbalance,
        UnusedDigester,
        DecliningPop,
        SellingLoss,
        [Hidden]
        COUNT,
    }

    static public class EventActorUtility {
        static public void QueueTrigger(EventActor actor, StringHash32 eventId, int tileIndex, NamedVariant customArg = default) {
            actor.QueuedTriggers.PushBack(new EventActorTrigger() {
                EventId = eventId,
                Argument = customArg,
                TileIndex = tileIndex
            });
        }

        static public void QueueTrigger(EventActor actor, EventActorTrigger trigger) {
            actor.QueuedTriggers.PushBack(trigger);
        }

        static public void QueueAlert(EventActor actor, EventActorAlertType alert, int tileIndex) {
            actor.QueuedTriggers.PushBack(new EventActorTrigger() {
                EventId = GameTriggers.AlertExamined,
                Argument = GameAlerts.GetAlertTypeArgument(alert),
                Alert = alert,
                TileIndex = tileIndex
            });
        }

        static public bool CancelEvent(EventActor actor, StringHash32 eventId) {
            int index = actor.QueuedEvents.FindIndex((e, id) => e.TypeId == id, eventId);
            if (index >= 0) {
                actor.QueuedEvents.RemoveAt(index);
                return true;
            }

            return false;
        }

        static public bool IsAlertQueued(EventActor actor, EventActorAlertType alert) {
            foreach (var trigger in actor.QueuedTriggers) {
                // in the case of alerts, "value" is the alertType
                if (trigger.Alert == alert) {
                    return true;
                }
            }

            foreach (var aEvent in actor.QueuedEvents) {
                // in the case of alerts, "value" is the alertType
                if (aEvent.Alert == alert) {
                    return true;
                }
            }

            return false;
        }
    
        static public void RegisterActor(EventActor actor, StringHash32 id) {
            if (actor == null) {
                return;
            }

            if (actor.Id != id) {
                StringHash32 oldId = actor.Id;
                actor.Id = id;

                if (Game.SharedState.TryGet(out ScriptRuntimeState runtime)) {
                    if (!oldId.IsEmpty) {
                        runtime.NamedActors.Remove(oldId);
                    } else {
                        runtime.NamedActors[id] = actor;
                    }
                }
            }
        }

        static public void DeregisterActor(EventActor actor) {
            if (!actor.Id.IsEmpty) {
                StringHash32 oldId = actor.Id;

                if (Game.SharedState.TryGet(out ScriptRuntimeState runtime)) {
                    runtime.NamedActors.Remove(oldId);
                }
            }
        }
    }
}