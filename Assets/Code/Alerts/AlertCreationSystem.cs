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
            // if no active events, create alert
            if (!component.ActivelyDisplayingEvent && component.QueuedEvents.Count > 0) {
                // allocate new alert from pool
                UIPools pools = Game.SharedState.Get<UIPools>();
                UIAlert alert = pools.Alerts.Alloc(tile.transform.position);

                // assign component as alert's Actor
                alert.Actor = component;

                Debug.Log("[Alerts] Created new alert!");
                component.ActivelyDisplayingEvent = true;
            }
        }
    }
}