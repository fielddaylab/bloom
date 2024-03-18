using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Sim;
using Zavala.UI;
using Zavala.World;

namespace Zavala.Scripting
{
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100100, ZavalaGame.SimulationUpdateMask)] // after EventActorSystem
    public sealed class AlertCreationSystem : ComponentSystemBehaviour<EventActor>
    {
        static private readonly Vector3 EventDisplayOffset = new Vector3(0, 1.0f, 0);

        [SerializeField] private SpriteLibrary m_AlertAssets;
        public override void ProcessWork(float deltaTime) {
            base.ProcessWork(deltaTime); // process work per component.
        }
        public override void ProcessWorkForComponent(EventActor component, float deltaTime) {
            if (Loc.IsServiceLoading()) {
                // Don't create alerts until localization has loaded (avoids empty banners)
                // TODO: This check won't be needed once we actually have a loading screen
                return;
            }

            // if no active events, create alert
            if (component.QueuedEvents.Count > 0) { // only create if they have events and aren't displaying them
                
                
                component.QueuedEvents.TryPeekFront(out EventActorQueuedEvent peekEvent);
                if (peekEvent.Alert == EventActorAlertType.GlobalDummy) {
                    // do not create UI banners for the fake global alerts
                    return;
                }

                if (component.DisplayingEvent) {
                    if (component.DisplayingEvent.AlertType == EventActorAlertType.Dialogue && peekEvent.Alert != EventActorAlertType.Dialogue) {

                        //int preCount = component.QueuedEvents.Count;
                        // var displayString = component.DisplayingEvent.AlertType.ToString();

                        // If showing a dialogue event, clear it and move it to the back
                        // ClearAlert just removes the visual, does not pop the event
                        UIAlertUtility.ClearAlert(component.DisplayingEvent, true);
                        component.QueuedEvents.PushBack(component.QueuedEvents.PopFront());

                        /*
                        Debug.Log("[DisplayBug] New event incoming, but already displaying event.\n"
                             + "peekEvent alert type: " + peekEvent.Alert.ToString() + "\n"
                        + "Displaying event: " + displayString + "\n"
                        + "Queued events length before: " + preCount + "\n"
                        + "Queued events length after: " + component.QueuedEvents.Count);
                        */
                        /*
                        // the new event is at the front, but we want to push back the dialogue event which is at 2nd from the front
                        var replacingEvent = component.QueuedEvents.PopFront();
                        var dialogueEvent = component.QueuedEvents.PopFront();
                        component.QueuedEvents.PushBack(dialogueEvent);
                        component.QueuedEvents.PushFront(replacingEvent);
                         */
                    }
                    else {
                        // Skip queueing for this actor, it is already displaying an important (i.e. nondialogue) event
                       return;
                    }

                }

                ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, peekEvent.ScriptId);
                if ((node.Flags & ScriptNodeFlags.Once) != 0) {
                    if (node.QueuedToAlert) {
                        Log.Msg("[EventActorSystem] Attempted to attach node {0} to {1}, but it has already been queued to an alert", node.FullName, component.Id.ToDebugString());
                        return;
                    } else {
                        node.QueuedToAlert = true;
                    }
                }

                // allocate new alert from pool
                UIPools pools = Game.SharedState.Get<UIPools>();
                UIAlert alert = pools.Alerts.Alloc(SimWorldUtility.GetTileCenter(peekEvent.TileIndex) + component.EventDisplayOffset + EventDisplayOffset);

                // assign component as alert's Actor
                alert.Actor = component;
                alert.AlertType = peekEvent.Alert;
                // assign localized banner text

                alert.AlertBase.sprite = GetAlertBaseSprite(peekEvent.Alert, m_AlertAssets);
                if (alert.AlertType != EventActorAlertType.Dialogue) {
                    alert.EventText.SetText(GameAlerts.GetLocalizedName(peekEvent.Alert));
                    alert.AlertBanner.sprite = GetAlertBannerSprite(peekEvent.Alert, m_AlertAssets);
                }

                if (Game.Gui.GetShared<GlobalAlertButton>().QueuedActors.Contains(component)) {
                    alert.KeepFaded = true;
                    UIAlertUtility.SetAlertFaded(alert, true);
                }

                ZavalaGame.Events.Dispatch(GameEvents.AlertAppeared, new Data.AlertData(peekEvent.Alert, peekEvent.TileIndex, node.FullName));
                Debug.Log("[Alerts] Created new alert!" + node.FullName);
                component.DisplayingEvent = alert;
            }
        }

        static private Sprite GetAlertBaseSprite(EventActorAlertType type, SpriteLibrary library) {
            StringHash32 id = "base_";
            id = id.FastConcat(GameAlerts.GetAlertName(type));
            if (!library.TryLookup(id, out Sprite sprite)) {
                Log.Warn("[Alerts] No sprite found for '{0}', substituting with bloom", type);
                id = "base_";
                id = id.FastConcat(GameAlerts.GetAlertName(EventActorAlertType.Bloom));
                library.TryLookup(id, out sprite);
            }
            return sprite;
        }

        static private Sprite GetAlertBannerSprite(EventActorAlertType type, SpriteLibrary library) {
            if (type == EventActorAlertType.Dialogue) {
                // NO BANNER for dialogue alerts
                return null;
            }
            StringHash32 id = "banner_";
            id = id.FastConcat(GameAlerts.GetAlertName(type));
            if (!library.TryLookup(id, out Sprite sprite)) {
                Log.Warn("[Alerts] No sprite found for '{0}', substituting with bloom", type);
                id = "banner_";
                id = id.FastConcat(GameAlerts.GetAlertName(EventActorAlertType.Bloom));
                library.TryLookup(id, out sprite);
            }
            return sprite;
        }
    }
}