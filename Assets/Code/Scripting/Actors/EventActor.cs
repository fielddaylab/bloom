using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Scripting {
    [DisallowMultipleComponent]
    public sealed class EventActor : BatchedComponent, ILeafActor {
        public int MaxQueuedEvents = 3;
        public SerializedHash32 Id;

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
    }

    public struct EventActorQueuedEvent {
        public StringHash32 ScriptId;
        public StringHash32 TypeId;
        public NamedVariant Argument;
    }

    static public class EventActorUtility {
        static public void QueueTrigger(EventActor actor, StringHash32 eventId, NamedVariant customArg = default) {
            actor.QueuedTriggers.PushBack(new EventActorTrigger() {
                EventId = eventId,
                Argument = customArg
            });
        }

        static public void QueueTrigger(GameObject actorGO, StringHash32 eventId, NamedVariant customArg = default) {
            if (actorGO.TryGetComponent(out EventActor act)) {
                QueueTrigger(act, eventId, customArg);
            }
        }

        static public bool CancelEvent(EventActor actor, StringHash32 eventId) {
            int index = actor.QueuedEvents.FindIndex((e, id) => e.TypeId == id, eventId);
            if (index >= 0) {
                actor.QueuedEvents.RemoveAt(index);
                return true;
            }

            return false;
        }
    }
}