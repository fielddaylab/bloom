using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Sim;
using Zavala.UI;

namespace Zavala.Scripting
{
    [SysUpdate(FieldDay.GameLoopPhase.Update, 100100)] // after EventActorSystem
    public sealed class AlertCreationSystem : ComponentSystemBehaviour<EventActor>
    {
        public GameObject AlertPrefab;

        public override void ProcessWorkForComponent(EventActor component, float deltaTime) {
            // if no active events, create alert
            if (!component.ActivelyDisplayingEvent && component.QueuedEvents.Count > 0) {
                // TODO: allocate new alert from pool
                UIAlert alert = Instantiate(AlertPrefab).GetComponent<UIAlert>();

                // assign component as alert's Actor
                alert.Actor = component;

                Debug.Log("[Alerts] Created new alert!");
                component.ActivelyDisplayingEvent = true;
            }
        }
    }
}