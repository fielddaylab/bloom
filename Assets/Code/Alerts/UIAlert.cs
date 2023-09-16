using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI {
    [RequireComponent(typeof(SnapToTile))]
    public class UIAlert : MonoBehaviour
    {
        [SerializeField] private MultiImageButton m_Button;
        public EventActor Actor; // The event actor this alert is anchored to
        public TMP_Text EventText;

        private void OnEnable() {
            m_Button.onClick.AddListener(HandleButtonClicked);
        }

        #region Handlers

        private void HandleButtonClicked() {
            Assert.NotNull(Actor);

            UIAlertUtility.ClickAlert(this);
        }

        #endregion // Handlers
    }

    public static class UIAlertUtility {

        static private readonly string[] RegionIndexToString = Enum.GetNames(typeof(RegionId));

        public static void ClickAlert(UIAlert alert) {
            // Activate queued script node event
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                if (!alert.Actor.QueuedEvents.TryPopBack(out EventActorQueuedEvent newEvent)) {
                    // No event linked with this event
                    return;
                }
                if (!newEvent.Argument.Id.IsEmpty) {
                    varTable.Set(newEvent.Argument.Id, newEvent.Argument.Value);
                }

                // TODD: shift screen focus to this event, updating current region index (may need to store the occupies tile index or region number in the queued event)

                // Use region index as a condition for alerts
                SimGridState grid = Game.SharedState.Get<SimGridState>();
                varTable.Set("region", RegionIndexToString[grid.CurrRegionIndex]); // i.e. region == Hillside
                varTable.Set("class", alert.Actor.Class);

                ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, newEvent.ScriptId);

                Log.Msg("[UIAlertUtility] Node is '{0}' ({1})", newEvent.ScriptId, node);

                // TODO: What if this particular node has already run between when the alert was created and when it was clicked?

                ScriptUtility.Runtime.Plugin.Run(node, alert.Actor, varTable);
                varTable.Clear();

                // free this alert
                UIPools pools = Game.SharedState.Get<UIPools>();
                pools.Alerts.Free(alert);

                // Allow next queued events to be generated
                alert.Actor.ActivelyDisplayingEvent = false;
            }
        }
    }

}
