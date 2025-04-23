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
            if (component.CooldownAccumulator < component.AlertCooldown) {
                component.CooldownAccumulator += deltaTime;
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
                        // If showing a dialogue event, clear it
                        // ClearAlert just removes the visual, does not pop the event
                        // component.QueuedEvents.MoveFrontToBackWhere(e => e.Alert == EventActorAlertType.Dialogue);
                        UIAlertUtility.ClearAlertImmediate(component.DisplayingEvent, true);
                    } else {
                        // Skip queueing for this actor, it is already displaying an important (i.e. nondialogue) event
                       return;
                    }

                }

                 ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, peekEvent.ScriptId);
                if (node == null)
                {
                    Debug.LogError("[EventActorSystem] Unable to find node for event " + peekEvent.ScriptId.ToDebugString());
                }
                else
                {
                    if ((node.Flags & ScriptNodeFlags.Once) != 0)
                    {
                        if (node.QueuedToTileIdx > 0 && node.QueuedToTileIdx != peekEvent.TileIndex)
                        {
                            Log.Msg("[EventActorSystem] Attempted to attach node {0} to {1}, but it has already been queued to tile {3}", node.FullName, peekEvent.TileIndex, node.QueuedToTileIdx);
                            component.QueuedEvents.PopFront();
                            return;
                        }
                        else
                        {
                            node.QueuedToTileIdx = peekEvent.TileIndex;
                        }
                    }
                }


                if (Game.Gui.GetShared<GlobalAlertButton>().QueuedActors.Contains(component)) {
                    return;
                    //alert.KeepFaded = true;
                    //UIAlertUtility.SetAlertFaded(alert, true);
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

                ZavalaGame.Events.Dispatch(GameEvents.AlertAppeared, EvtArgs.Box(new Data.AlertData(component, peekEvent.Alert, peekEvent.TileIndex, node.FullName)));
                Log.Debug("[Alerts] Created new alert!" + node.FullName);
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