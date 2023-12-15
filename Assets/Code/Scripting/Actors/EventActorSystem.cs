using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Alerts;
using Zavala.Sim;

namespace Zavala.Scripting {
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100000, ZavalaGame.SimulationUpdateMask)]
    public sealed class EventActorSystem : ComponentSystemBehaviour<EventActor> {
        public override void ProcessWorkForComponent(EventActor component, float deltaTime) {
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                while (component.QueuedTriggers.TryPopBack(out EventActorTrigger trigger)) {
                    if (!trigger.Argument.Id.IsEmpty) {
                        varTable.Set(trigger.Argument.Id, trigger.Argument.Value);
                    }
                    if (!trigger.SecondArg.Id.IsEmpty) {
                        varTable.Set(trigger.SecondArg.Id, trigger.SecondArg.Value);
                    }
                    if (!trigger.RegionIndex.Id.IsEmpty) {
                        varTable.Set(trigger.RegionIndex.Id, trigger.RegionIndex.Value);
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
                                SecondArg = trigger.SecondArg,
                                TypeId = trigger.EventId,
                                ScriptId = node.Id(),
                                RegionIndex = trigger.RegionIndex,
                                TileIndex = trigger.TileIndex,
                                Alert = trigger.Alert
                            };
                            component.QueuedEvents.PushBack(queuedEvent);
                            HashSet<AutoAlertCondition> conditions = ZavalaGame.SharedState.Get<AlertState>().AutoTriggerAlerts;
                            foreach (AutoAlertCondition autoTrig in conditions) {
                                Log.Msg("[EventActorUtility] Checking for AutoTriggerAlert...");
                                if ((autoTrig.Alert == EventActorAlertType.None || autoTrig.Alert == queuedEvent.Alert) &&
                                    (autoTrig.RegionIndex == -1 || autoTrig.RegionIndex == queuedEvent.RegionIndex.Value)) {
                                                                       
                                    Log.Msg("[EventActorUtility] AutoTriggerAlert ACTIVATED");
                                    EventActorUtility.TriggerActorAlert(component);
                                    conditions.Remove(autoTrig);
                                    break;
                                } else {
                                    Log.Msg("[EventActorUtility] AutoTriggerAlert BYPASSED.");
                                }

                            }

                        }
                    }
                }
            }
        }
    }
}