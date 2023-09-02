using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Scripting;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Zavala.Scripting;

namespace Zavala.UI {
    public class UIAlert : MonoBehaviour
    {
        [SerializeField] private MultiImageButton m_Button;
        public EventActor Actor; // The event actor this alert is anchored to

        private void OnEnable() {
            m_Button.onClick.AddListener(HandleButtonClicked);
        }

        #region Handlers

        private void HandleButtonClicked() {
            Assert.NotNull(Actor);

            // Activate queued script node event
            using (TempVarTable varTable = TempVarTable.Alloc()) {
                if (!Actor.QueuedEvents.TryPopBack(out EventActorQueuedEvent newEvent)) {
                    // No event linked with this event
                    return;
                }
                if (!newEvent.Argument.Id.IsEmpty) {
                    varTable.Set(newEvent.Argument.Id, newEvent.Argument.Value);
                }

                //ScriptNode node = ScriptDatabaseUtility.FindSpecificNode(ScriptUtility.Database, newEvent.ScriptId, ScriptUtility.GetContext(ScriptUtility.Runtime, Actor, varTable), Actor.Id);
                ScriptNode node = ScriptDatabaseUtility.FindRandomTrigger(ScriptUtility.Database, newEvent.TypeId, ScriptUtility.GetContext(ScriptUtility.Runtime, Actor, varTable), Actor.Id);

                // TODO: What if this particular node has already run between when the alert was created and when it was clicked?

                ScriptUtility.Runtime.Plugin.Run(node, Actor, varTable);
                varTable.Clear();

                // TODO: hide/destroy this alert

                // Allow next queued events to be generated
                Actor.ActivelyDisplayingEvent = false;
            }
        }

        #endregion // Handlers
    }
}
