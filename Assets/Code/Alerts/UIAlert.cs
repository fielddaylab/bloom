using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIAlert : BatchedComponent
    {
        [SerializeField] private CanvasGroup m_Group;
        [SerializeField] private MultiImageButton m_Button;
        public TMP_Text EventText;
        public Image AlertBase;
        public Image AlertBanner;

        [NonSerialized] public EventActor Actor; // The event actor this alert is anchored to
        [NonSerialized] public Routine BannerRoutine;
        [NonSerialized] public bool FullyOpened = false;
        [NonSerialized] public Routine FadeRoutine;

        protected override void OnEnable() {
            base.OnEnable();

            m_Button.onClick.AddListener(HandleButtonClicked);
            SimTimeUtility.OnPauseUpdated.Register(OnPauseUpdated);

            OnPauseUpdated(ZavalaGame.SimTime.Paused);
        }

        protected override void OnDisable() {
            m_Button.onClick.RemoveListener(HandleButtonClicked);
            SimTimeUtility.OnPauseUpdated.Deregister(OnPauseUpdated);

            base.OnDisable();
        }

        #region Handlers

        private void OnPauseUpdated(SimPauseFlags flags) {
            bool blueprints = (flags & SimPauseFlags.Blueprints) != 0;
            m_Group.alpha = blueprints ? 0.1f : 1f;
            m_Group.blocksRaycasts = !blueprints;
        }

        private void HandleButtonClicked() {
            Assert.NotNull(Actor);

            UIAlertUtility.ClickAlert(this);
        }

        #endregion // Handlers
    }

    public static class UIAlertUtility {

        static private readonly string[] RegionIndexToString = Enum.GetNames(typeof(RegionId));

        public static void ClickAlert(UIAlert alert) {
            if (!alert.FullyOpened) {
                alert.BannerRoutine.Replace(OpenRoutine(alert));
                return;
            }
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

                alert.BannerRoutine.Replace(CloseRoutine(alert, true));
            }
        }

        public static void FreeAlert(UIAlert alert) {
            // free this alert
            UIPools pools = Game.SharedState.Get<UIPools>();
            pools.Alerts.Free(alert);

            // Allow next queued events to be generated
            alert.Actor.DisplayingEvent = null;
        }

        public static IEnumerator OpenRoutine(UIAlert alert) {
            yield return alert.AlertBanner.rectTransform.AnchorPosTo(new Vector2(0, 0), 0.3f).Ease(Curve.CubeIn);
            alert.FullyOpened = true;
            alert.BannerRoutine.Replace(HoldRoutine(alert, 5.0f));
            yield return null;
        }

        public static IEnumerator HoldRoutine(UIAlert alert, float sec) {
            yield return Routine.WaitRealSeconds(sec);
            alert.BannerRoutine.Replace(CloseRoutine(alert, false));
            yield return null;
        }
        public static IEnumerator CloseRoutine(UIAlert alert, bool freeOnClose) {
            alert.FullyOpened = false;
            yield return alert.AlertBanner.rectTransform.AnchorPosTo(new Vector2(-120, 0), 0.3f).Ease(Curve.CubeIn);
            if (freeOnClose) FreeAlert(alert);
            yield return null;
        }
    }

}
