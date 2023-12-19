using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using FieldDay.UI;
using Leaf.Runtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI {
    public class GlobalAlertButton : SharedPanel {

        //public SpriteMaskGroup Masking;
        //public TMP_Text EventText;
        //public SpriteRenderer AlertBase;
        //public SpriteRenderer AlertBanner;
        //public RectTransform AlertBannerRect;
        //public PointerListener Pointer;
        public RectTransform m_Rect; 
        public int MaxQueuedEvents = 3; // there should really only ever be 1 but let's keep it to 3 for emergencies

        public Button m_Button;

        public RingBuffer<EventActor> QueuedActors = new RingBuffer<EventActor>();
        [NonSerialized] public Routine m_Routine;

        protected override void Awake() {
            base.Awake();

            UpdateButtonRoutine();
            m_Button.onClick.AddListener(HandleButtonClicked);
        }

        protected override void OnDestroy() {
            m_Button.onClick.RemoveListener(HandleButtonClicked);
        }

        #region Handlers

        private void HandleButtonClicked() {
            QueuedActors.TryPopFront(out EventActor actor);
            Assert.NotNull(actor);

            EventActorUtility.TriggerActorAlert(actor);
            UIAlertUtility.ClearAlert(actor.DisplayingEvent);
            UpdateButtonRoutine();
           // UIAlertUtility.ClickAlert(Actor);
        }

        public void UpdateButtonRoutine() {
            if (QueuedActors.Count > 0) {
                m_Routine.Replace(GlobalAlertUtility.AppearRoutine(this));
                SimTimeInput.SetPaused(true, SimPauseFlags.PendingGlobalAlert);
            } else {
                m_Routine.Replace(GlobalAlertUtility.DisappearRoutine(this));
                SimTimeInput.SetPaused(false, SimPauseFlags.PendingGlobalAlert);
            }
        }

        #endregion // Handlers
    }

    public static class GlobalAlertUtility {

        // static private readonly string[] RegionIndexToString = Enum.GetNames(typeof(RegionId));
        public static void PushEventOfActorToGlobal(GlobalAlertButton button, EventActor actor) {
            button.QueuedActors.PushBack(actor);
            button.UpdateButtonRoutine();
        }

        // TODO: add a special case for region unlock "alerts"
        public static void CreateEventForGlobal(GlobalAlertButton button, EventActor actor, StringHash32 nodeId) {
            EventActorQueuedEvent fakeEvent = new() {
                ScriptId = nodeId,
                Alert = EventActorAlertType.GlobalDummy
            };
            actor.QueuedEvents.PushBack(fakeEvent);
            PushEventOfActorToGlobal(button, actor);
        }

        [LeafMember("SendGlobalAlertForNode")]
        public static void GlobalAlertLeaf(StringHash32 actorId, StringHash32 scriptId) {
            EventActor actor = ScriptUtility.LookupActor(actorId);
            CreateEventForGlobal(Game.Gui.GetShared<GlobalAlertButton>(), actor, scriptId);
        }

        public static IEnumerator AppearRoutine(GlobalAlertButton button) {
            button.gameObject.SetActive(true);
            yield return Routine.Combine(
                button.m_Rect.ScaleTo(1f, 0.2f).Ease(Curve.CubeIn),
                button.m_Button.targetGraphic.FadeTo(1, 0.1f));
            button.m_Routine.Replace(PulseRoutine(button));
            yield return null;
        }

        public static IEnumerator PulseRoutine(GlobalAlertButton button) {
            while (true) {
                yield return button.m_Rect.ScaleTo(1.1f, 0.5f).Ease(Curve.Smooth);
                yield return button.m_Rect.ScaleTo(0.9f, 0.5f).Ease(Curve.Smooth);
            }   
        }
        public static IEnumerator DisappearRoutine(GlobalAlertButton button) {
            yield return Routine.Combine(
                button.m_Rect.ScaleTo(0.1f, 0.2f).Ease(Curve.CubeIn),
                button.m_Button.targetGraphic.FadeTo(0f, 0.2f));
            button.gameObject.SetActive(false);
            yield return null;
        }
    }

}
