using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Alerts;
using Zavala.Sim;
using Zavala.UI;

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
                        // cancel any events of the same trigger type
                        EventActorUtility.CancelEvent(component, trigger.EventId);

                        // If the node has @once, check if it has been queued to an alert.
                        if ((node.Flags & ScriptNodeFlags.Once) != 0 && node.QueuedToAlert) {
                            Log.Msg("[EventActorSystem] Attempted to attach node {0} to {1}, but it has already been queued to an alert", node.FullName, component.Id.ToDebugString());
                            return;
                        }

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
                            if (component.QueuedEvents.TryPeekFront(out EventActorQueuedEvent qEvent) && qEvent.Alert == EventActorAlertType.Dialogue) {
                                component.QueuedEvents.PushFront(queuedEvent);
                            } else {
                                component.QueuedEvents.PushBack(queuedEvent);
                            }
                            HashSet<AutoAlertCondition> conditions = ZavalaGame.SharedState.Get<AlertState>().AutoTriggerAlerts;
                            foreach (AutoAlertCondition autoTrig in conditions) {
                                Log.Msg("[EventActorUtility] Checking for AutoTriggerAlert...");
                                if ((autoTrig.Alert == EventActorAlertType.None || autoTrig.Alert == queuedEvent.Alert) &&
                                    (autoTrig.RegionIndex == -1 || autoTrig.RegionIndex == queuedEvent.RegionIndex.Value) &&
                                    (autoTrig.CafosOnly == false || (queuedEvent.SecondArg.Id == "isFromGrainFarm" && queuedEvent.SecondArg.Value == false))) {
                                    // node.QueuedToAlert = true;
                                    Log.Msg("[EventActorUtility] AutoTriggerAlert ACTIVATED");
                                    //EventActorUtility.TriggerActorAlert(component);
                                    GlobalAlertUtility.PushEventOfActorToGlobal(Game.Gui.GetShared<GlobalAlertButton>(), component);
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