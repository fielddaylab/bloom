using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.UI;

namespace Zavala.Scripting
{
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100100)] // after EventActorSystem
    public sealed class AlertCreationSystem : ComponentSystemBehaviour<EventActor, OccupiesTile>
    {
        public GameObject AlertPrefab;

        public override void ProcessWorkForComponent(EventActor component, OccupiesTile tile, float deltaTime) {
            if (Loc.IsServiceLoading()) {
                // Don't create alerts until localization has loaded (avoids empty banners)
                // TODO: This check won't be needed once we actually have a loading screen
                return;
            }

            // if no active events, create alert
            if (!component.ActivelyDisplayingEvent && component.QueuedEvents.Count > 0) {
                // allocate new alert from pool
                UIPools pools = Game.SharedState.Get<UIPools>();
                UIAlert alert = pools.Alerts.Alloc(tile.transform.position);

                // assign component as alert's Actor
                alert.Actor = component;

                // assign localized banner text
                alert.Actor.QueuedEvents.TryPeekBack<EventActorQueuedEvent>(out EventActorQueuedEvent peekEvent);
                alert.EventText.SetText(GameAlerts.ConvertArgToLocText(peekEvent.Argument.Value));

                Debug.Log("[Alerts] Created new alert!");
                component.ActivelyDisplayingEvent = true;
            }
        }
    }
}