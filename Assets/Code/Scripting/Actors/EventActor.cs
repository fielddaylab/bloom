using System;
using System.Collections.Generic;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Alerts;
using Zavala.Building;
using Zavala.Data;
using Zavala.Sim;
using Zavala.UI;
using Zavala.World;

namespace Zavala.Scripting {
    [DisallowMultipleComponent]
    public sealed class EventActor : BatchedComponent, ILeafActor, IPersistBuildingComponent {
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

        void IPersistBuildingComponent.Write(PersistBuilding building, ref ByteWriter writer) {
            writer.Write((byte) QueuedTriggers.Count);
            for(int i = 0; i < QueuedTriggers.Count; i++) {
                writer.Write(QueuedTriggers[i]);
            }

            writer.Write((byte) QueuedEvents.Count);
            for (int i = 0; i < QueuedEvents.Count; i++) {
                writer.Write(QueuedEvents[i]);
            }
        }

        void IPersistBuildingComponent.Read(PersistBuilding building, ref ByteReader reader) {
            QueuedTriggers.Clear();
            QueuedEvents.Clear();

            int triggerCount = reader.Read<byte>();
            for (int i = 0; i < triggerCount; i++) {
                QueuedTriggers.PushBack(reader.Read<EventActorTrigger>());
            }

            int eventCount = reader.Read<byte>();
            for (int i = 0; i < eventCount; i++) {
                QueuedEvents.PushBack(reader.Read<EventActorQueuedEvent>());
            }
        }
    }

    public struct EventActorTrigger {
        public StringHash32 EventId;
        public NamedVariant Argument;
        public NamedVariant SecondArg;
        public NamedVariant RegionIndex;
        public EventActorAlertType Alert;
        public int TileIndex;
    }

    public struct EventActorQueuedEvent {
        public StringHash32 ScriptId;
        public StringHash32 TypeId;
        public NamedVariant Argument;
        public NamedVariant SecondArg;
        public NamedVariant RegionIndex;
        public EventActorAlertType Alert;
        public int TileIndex;
        // public bool WasSentToGlobal;
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
        Disconnected,
        Dialogue, // Specific case with no banner

        GlobalDummy,
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

        static public void QueueAlert(EventActor actor, EventActorAlertType alert, int tileIndex, int regionIndex, NamedVariant secondArg = default) {
            /*
            // skip queuing any paused events
            if (Game.SharedState.Get<AlertState>().PausedAlertTypes.Contains(alert)) {
                Log.Msg("[EventActorUtility] Skipping alert for {0}, event type {1} paused", actor.Id.ToDebugString(), alert);
                return;
            }
            */
            actor.QueuedTriggers.PushBack(new EventActorTrigger() {
                EventId = GameTriggers.AlertExamined,
                Argument = GameAlerts.GetAlertTypeArgument(alert),
                SecondArg = secondArg,
                Alert = alert,
                RegionIndex = new NamedVariant("alertRegion", regionIndex+1), // 0-indexed to 1-indexed
                TileIndex = tileIndex
            });
        }

        static public void QueueAlert(EventActor actor, EventActorAlertType type) {
            if (actor != null && actor.gameObject.TryGetComponent(out OccupiesTile tile)) {
                QueueAlert(actor, type, tile.TileIndex, tile.RegionIndex);
            } else {
                Log.Msg("[EventActorUtility] Failed to queue alert: actor {0} has no OccupiesTile", actor);
            }
        }

        static public void ClearAndPopAlert(EventActor actor) {
            if (UIAlertUtility.ClearAlert(actor.DisplayingEvent)) {
                actor.QueuedEvents.PopFront();
            }
        }

        static public void AddAutoAlertCondition(AutoAlertCondition cond) {
            Log.Msg("[EventActorUtility] Adding AutoTriggerAlert condition: {0} in Region {1}", cond.Alert, cond.RegionIndex);
            Game.SharedState.Get<AlertState>().AutoTriggerAlerts.Add(cond);
        }

        [LeafMember("AddAutoAlertCondition")]
        static public void AddAutoAlertConditionLeaf(EventActorAlertType alertType = default, int regionIndex = -1, bool cafosOnly = false) {
            AddAutoAlertCondition(new AutoAlertCondition() {
                Alert = alertType,
                RegionIndex = regionIndex,
                CafosOnly = cafosOnly, // for the specific case of auto-triggering CAFO runoff but not grain farm
            });
        }

        /*
        [LeafMember("PauseAlertType")]
        static public bool PauseAlertType(EventActorAlertType alertType) {
            return Game.SharedState.Get<AlertState>().PausedAlertTypes.Add(alertType);
        }

        [LeafMember("UnpauseAlertType")]
        static public bool UnpauseAlertType(EventActorAlertType alertType) {
            return Game.SharedState.Get<AlertState>().PausedAlertTypes.Remove(alertType);
        }
        

        [LeafMember("ToggleAlertTypeActive")]
        static public void ToggleAlertTypeActive(EventActorAlertType alertType) {
            // pause if it's not paused already
            if (!PauseAlertType(alertType)) {
                // false - it's paused already, so unpause
                UnpauseAlertType(alertType); 
            }
        }
        */

        [LeafMember("QueueDialogueBubble")]
        static public void QueueDialogueAlert(StringHash32 actor, StringHash32 targetNode) {
            EventActor target = ScriptUtility.LookupActor(actor);
            if (target == null) {
                Log.Warn("[EventActorUtility] Failed to queue dialogue for actor {0}: actor not found!", actor.ToDebugString());
                return;
            }
            OccupiesTile ot = target.GetComponent<OccupiesTile>();
            EventActorQueuedEvent fakeEvent = new() {
                ScriptId = targetNode,
                TileIndex = ot.TileIndex,
                RegionIndex = new NamedVariant("alertRegion", ot.RegionIndex+1), //0-indexed to 1-indexed
                Alert = EventActorAlertType.Dialogue
            };
            target.QueuedEvents.PushBack(fakeEvent);
        }

        [LeafMember("QueueDialogueBubbleGeneric")]
        static public void QueueDialogueAlert(int regionOneIndexed, string type, StringHash32 targetNode) {
            QueueDialogueAlert("region" + regionOneIndexed + "_" + type + "1", targetNode);
        }

        public static void TriggerActorAlert(EventActor actor) {

            // Activate queued script node event
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                if (!actor.QueuedEvents.TryPopFront(out EventActorQueuedEvent newEvent)) {
                    // No event linked with this event
                    return;
                }
                if (!newEvent.Argument.Id.IsEmpty) {
                    varTable.Set(newEvent.Argument.Id, newEvent.Argument.Value);
                }
                if (!newEvent.SecondArg.Id.IsEmpty) {
                    varTable.Set(newEvent.SecondArg.Id, newEvent.SecondArg.Value);
                }

                ZavalaGame.Events.Dispatch(GameEvents.AlertClicked, new AlertData(newEvent));

                // TODD: shift screen focus to this event, updating current region index (may need to store the occupies tile index or region number in the queued event)
                WorldCameraUtility.PanCameraToTransform(actor.transform, -1.5f);

                // Use region index as a condition for alerts
                // SimGridState grid = Game.SharedState.Get<SimGridState>();
                varTable.Set(newEvent.RegionIndex.Id, newEvent.RegionIndex.Value); // i.e. region == Hillside
                varTable.Set("class", actor.Class);

                ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, newEvent.ScriptId);

                Log.Msg("[UIAlertUtility] Node is '{0}' ({1})", newEvent.ScriptId, node);
                // TODO: What if this particular node has already run between when the alert was created and when it was clicked?

                ScriptUtility.Runtime.Plugin.Run(node, actor, varTable);
                varTable.Clear();

                // Dialogue has no animation, "manually" cleared over here
                if (newEvent.Alert == EventActorAlertType.Dialogue) {
                    UIAlertUtility.ClearAlert(actor.DisplayingEvent);
                }
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

        static public bool CancelEventType(EventActor actor, EventActorAlertType alertType)
        {
            int index = actor.QueuedEvents.FindIndex((e, id) => e.Alert == id, alertType);
            if (index >= 0)
            {
                actor.QueuedEvents.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the actor has any QueuedTriggers or QueuedEvents of the given type.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <param name="alert">The alert type to check for.</param>
        /// <returns></returns>
        static public bool IsAlertQueued(EventActor actor, EventActorAlertType alert) {
            if (actor == null) return false;
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

        /// <summary>
        ///  Returns whether the actor has QueuedTriggers to QueuedEvents of any type.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        static public bool IsAlertQueued(EventActor actor) {
            if (actor == null) return false;
            return actor.QueuedTriggers.Count > 0 || actor.QueuedEvents.Count > 0;
        }

        static public bool IsAlertEventQueued(EventActor actor, EventActorAlertType alert)
        {
            foreach (var aEvent in actor.QueuedEvents)
            {
                // in the case of alerts, "value" is the alertType
                if (aEvent.Alert == alert)
                {
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