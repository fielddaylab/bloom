using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Scripting {
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100000)]
    public sealed class EventActorSystem : ComponentSystemBehaviour<EventActor> {
        public override void ProcessWorkForComponent(EventActor component, float deltaTime) {
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                while (component.QueuedTriggers.TryPopBack(out EventActorTrigger trigger)) {
                    if (!trigger.Argument.Id.IsEmpty) {
                        varTable.Set(trigger.Argument.Id, trigger.Argument.Value);
                    }

                    ScriptNode node = ScriptDatabaseUtility.FindRandomTrigger(ScriptUtility.Database, trigger.EventId, ScriptUtility.GetContext(ScriptUtility.Runtime, component, varTable), component.Id);
                    varTable.Clear();

                    // TODO: Do we need to handle @once tags between queueing events and running them?
                    // TODO: ensure only one of any particular node is "checked out" at any given time
                    // OR allow any number to display the alert, but once one has been clicked, remove all alerts that share that same ID

                    if (node != null) {
                        EventActorUtility.CancelEvent(component, trigger.EventId);
                        if (component.QueuedEvents.Count < component.MaxQueuedEvents) {
                            EventActorQueuedEvent queuedEvent = new EventActorQueuedEvent() {
                                Argument = trigger.Argument,
                                TypeId = trigger.EventId,
                                ScriptId = node.Id(),
                                TileIndex = trigger.TileIndex,
                                Alert = trigger.Alert
                            };
                            component.QueuedEvents.PushBack(queuedEvent);
                        }
                    }
                }
            }
        }
    }
}