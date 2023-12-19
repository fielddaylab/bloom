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
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

            base.OnDisable();
        }

        #region Handlers

        private void OnPauseUpdated(SimPauseFlags flags) {
            bool blueprints = (flags & SimPauseFlags.Blueprints) != 0;
            AlertBase.SetAlpha(blueprints ? 0.1f : 1f);
            Pointer.enabled = !blueprints;
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
            if (!alert.FullyOpened) {
                alert.Pointer.enabled = false;
                alert.Pointer.GetComponent<Collider>().enabled = false;
                alert.BannerRoutine.Replace(OpenRoutine(alert));
                return;
            }
            EventActorUtility.TriggerActorAlert(alert.Actor);
            //alert.BannerRoutine.Replace(CloseRoutine(alert, true));
        }

        public static void ClearAlert(UIAlert alert) {
            if (alert == null) {
                Log.Msg("[UIAlertUtility] Clear Alert: attempted to clear null alert, skipping.");
;                return; 
            }
            alert.BannerRoutine.Replace(CloseRoutine(alert, true));
        }

        public static void FreeAlert(UIAlert alert) {
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
            alert.BannerRoutine.Replace(HoldRoutine(alert, 5.0f));
            // ClickAlert(alert);
            yield return null;
        }

        public static IEnumerator HoldRoutine(UIAlert alert, float sec) {
            yield return Routine.WaitRealSeconds(sec);
            alert.BannerRoutine.Replace(CloseRoutine(alert, true));
            yield return null;
        }
        public static IEnumerator CloseRoutine(UIAlert alert, bool freeOnClose) {
            alert.FullyOpened = false;
            alert.Masking.SetState(true);
            yield return Routine.Combine(
                alert.AlertBannerRect.AnchorPosTo(-3f, 0.3f, Axis.X).Ease(Curve.QuadIn).DelayBy(0.15f),
                alert.EventText.FadeTo(0, 0.2f),
                alert.AlertBase.FadeTo(0, 0.2f).DelayBy(0.45f)
            );
            if (freeOnClose) FreeAlert(alert);
            yield return null;
        }
    }

}
