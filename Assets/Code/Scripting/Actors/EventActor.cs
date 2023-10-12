using System;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
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

        #region Leaf

        StringHash32 ILeafActor.Id => Id;

        VariantTable ILeafActor.Locals => null;

        #endregion // Leaf
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
    }
}