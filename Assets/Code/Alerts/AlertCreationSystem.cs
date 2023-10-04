using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.UI;

namespace Zavala.Scripting
{
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100100)] // after EventActorSystem
    public sealed class AlertCreationSystem : ComponentSystemBehaviour<EventActor, OccupiesTile>
    {
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
                Variant eventVal = peekEvent.Argument.Value;
                alert.EventText.SetText(GameAlerts.ConvertArgToLocText(eventVal));

                string spritePath = "UIAlert/base_" + eventVal.ToDebugString().Replace("\"","");
                alert.AlertBase.sprite = Resources.Load<Sprite>(spritePath);
                if (alert.AlertBase.sprite == null) {
                    Debug.LogWarning("[Alerts] Alert path "+spritePath+" invalid, substituting with bloom");
                    alert.AlertBase.sprite = Resources.Load<Sprite>("UIAlert/base_bloom");
                }
                spritePath = "UIAlert/banner_" + eventVal.ToDebugString().Replace("\"", "");
                alert.AlertBanner.sprite = Resources.Load<Sprite>(spritePath);
                if (alert.AlertBanner.sprite == null) {
                    Debug.LogWarning("[Alerts] Alert path " + spritePath + " invalid, substituting with bloom");
                    alert.AlertBanner.sprite = Resources.Load<Sprite>("UIAlert/banner_bloom");
                }
                

                Debug.Log("[Alerts] Created new alert!");
                component.ActivelyDisplayingEvent = true;
            }
        }
    }
}