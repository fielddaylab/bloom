using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.UI;
using Zavala.World;

namespace Zavala.Scripting
{
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100100)] // after EventActorSystem
    public sealed class AlertCreationSystem : ComponentSystemBehaviour<EventActor>
    {
        static private readonly Vector3 EventDisplayOffset = new Vector3(0, 0.5f, 0);

        [SerializeField] private SpriteLibrary m_AlertAssets;

        public override void ProcessWorkForComponent(EventActor component, float deltaTime) {
            if (Loc.IsServiceLoading()) {
                // Don't create alerts until localization has loaded (avoids empty banners)
                // TODO: This check won't be needed once we actually have a loading screen
                return;
            }

            // if no active events, create alert
            if (!component.DisplayingEvent && component.QueuedEvents.Count > 0) {
                component.QueuedEvents.TryPeekBack<EventActorQueuedEvent>(out EventActorQueuedEvent peekEvent);

                // allocate new alert from pool
                UIPools pools = Game.SharedState.Get<UIPools>();
                UIAlert alert = pools.Alerts.Alloc(SimWorldUtility.GetTileCenter(peekEvent.TileIndex) + component.EventDisplayOffset + EventDisplayOffset);

                // assign component as alert's Actor
                alert.Actor = component;

                // assign localized banner text
                alert.EventText.SetText(GameAlerts.GetLocalizedName(peekEvent.Alert));

                alert.AlertBase.sprite = GetAlertBaseSprite(peekEvent.Alert, m_AlertAssets);
                alert.AlertBanner.sprite = GetAlertBannerSprite(peekEvent.Alert, m_AlertAssets);

                Debug.Log("[Alerts] Created new alert!");
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