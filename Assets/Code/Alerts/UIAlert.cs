using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Zavala.Data;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI {
    public class UIAlert : BatchedComponent
    {
        public SpriteMaskGroup Masking;
        public TMP_Text EventText;
        public SpriteRenderer AlertBase;
        public SpriteRenderer AlertBanner;
        public RectTransform AlertBannerRect;
        public PointerListener Pointer;

        [NonSerialized] public EventActor Actor; // The event actor this alert is anchored to
        [NonSerialized] public Routine BannerRoutine;
        [NonSerialized] public bool FullyOpened = false;
        [NonSerialized] public Routine FadeRoutine;
        [NonSerialized] public bool KeepFaded;
        [NonSerialized] public EventActorAlertType AlertType;

        private void Awake() {
            AlertBannerRect.SetAnchorPos(-3f, Axis.X);
            //Masking.MaskComponent.sharedMaterial = 
        }

        protected override void OnEnable() {
            base.OnEnable();

            Pointer.GetComponent<Collider>().enabled = true;
            Pointer.onClick.AddListener(HandleButtonClicked);
            SimTimeUtility.OnPauseUpdated.Register(OnPauseUpdated);

            AlertBanner.enabled = false;
            EventText.enabled = false;
            EventText.alpha = 0;
            AlertBase.SetAlpha(1);
            Masking.SetState(false);

            OnPauseUpdated(ZavalaGame.SimTime.Paused);
        }

        protected override void OnDisable() {
            Pointer.onClick.RemoveListener(HandleButtonClicked);
            SimTimeUtility.OnPauseUpdated.Deregister(OnPauseUpdated);
            FadeRoutine.Stop();
            BannerRoutine.Stop();

            base.OnDisable();
        }

        #region Handlers

        private void OnPauseUpdated(SimPauseFlags flags) {
            bool blueprints = (flags & SimPauseFlags.Blueprints) != 0;
            // bool globalAlert = (flags & SimPauseFlags.PendingGlobalAlert) != 0;
            UIAlertUtility.SetAlertFaded(this, blueprints);
        }

        private void HandleButtonClicked(PointerEventData evt) {
            Assert.NotNull(Actor);

            UIAlertUtility.ClickAlert(this);
        }

        #endregion // Handlers
    }

    public static class UIAlertUtility {

        // static private readonly string[] RegionIndexToString = Enum.GetNames(typeof(RegionId));

        public static void ClickAlert(UIAlert alert) {
            if (alert.AlertType != EventActorAlertType.Dialogue && !alert.FullyOpened) {
                alert.Pointer.enabled = false;
                alert.Pointer.GetComponent<Collider>().enabled = false;
                alert.BannerRoutine.Replace(OpenRoutine(alert));
                return;
            }

            EventActorUtility.TriggerActorAlert(alert.Actor);
            //alert.BannerRoutine.Replace(CloseRoutine(alert, true));
        }

        public static void SetAlertFaded(UIAlert alert, bool faded) {
            if (alert == null) {
                return;
            }
            if (alert.KeepFaded) {
                faded = true;
            }
            alert.Pointer.enabled = !faded;
            alert.AlertBase.SetAlpha(faded ? 0.1f : 1.0f);
        }

        public static bool ClearAlert(UIAlert alert, bool recycleEvent = false) {
            if (alert == null) {
                Log.Msg("[UIAlertUtility] Clear Alert: attempted to clear null alert, skipping.");
;                return false; 
            }
            alert.KeepFaded = false;
            alert.AlertBase.SetAlpha(1.0f);
            alert.BannerRoutine.Replace(CloseRoutine(alert, true, recycleEvent));
            return true;
        }

        public static void FreeAlert(UIAlert alert, bool recycleEvent = false) {
            if (!recycleEvent) { 
                EventActorUtility.CancelEventType(alert.Actor, alert.AlertType);
            }
            // free this alert
            UIPools pools = Game.SharedState.Get<UIPools>();
            pools.Alerts.Free(alert);

            // Allow next queued events to be generated
            alert.Actor.DisplayingEvent = null;
        }

        public static IEnumerator OpenRoutine(UIAlert alert) {
            alert.Masking.SetState(true);
            alert.AlertBanner.enabled = alert.EventText.enabled = true;
            yield return Routine.Combine(
                alert.AlertBannerRect.AnchorPosTo(0, 0.3f, Axis.X).Ease(Curve.CubeOut),
                alert.EventText.FadeTo(1, 0.2f).DelayBy(0.25f)
            );
            alert.FullyOpened = true;
            alert.Masking.SetState(false);
            alert.BannerRoutine.Replace(HoldRoutine(alert, 3.0f));
            ClickAlert(alert);
            yield return null;
        }

        public static IEnumerator HoldRoutine(UIAlert alert, float sec) {
            yield return Routine.WaitRealSeconds(sec);
            alert.BannerRoutine.Replace(CloseRoutine(alert, true, false));
            yield return null;
        }
        public static IEnumerator CloseRoutine(UIAlert alert, bool freeOnClose, bool recycleEvent) {
            alert.FullyOpened = false;
            alert.Masking.SetState(true);
            if (alert.AlertType == EventActorAlertType.Dialogue) {
                yield return alert.AlertBase.FadeTo(0, 0.2f).DelayBy(0.45f);
            } else {
                yield return Routine.Combine(
                    alert.AlertBannerRect.AnchorPosTo(-3f, 0.3f, Axis.X).Ease(Curve.QuadIn).DelayBy(0.15f),
                    alert.EventText.FadeTo(0, 0.2f),
                    alert.AlertBase.FadeTo(0, 0.2f).DelayBy(0.45f)
                );
            }
            if (freeOnClose) FreeAlert(alert, recycleEvent);
            yield return null;
        }
    }

}
