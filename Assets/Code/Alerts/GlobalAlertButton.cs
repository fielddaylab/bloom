using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Scripting;
using FieldDay.UI;
using Leaf.Runtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Zavala.Data;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.UI {
    public class GlobalAlertButton : SharedPanel, ISaveStateChunkObject, ISaveStatePostLoad {

        //public SpriteMaskGroup Masking;
        //public TMP_Text EventText;
        //public SpriteRenderer AlertBase;
        //public SpriteRenderer AlertBanner;
        //public RectTransform AlertBannerRect;
        //public PointerListener Pointer;
        public RectTransform m_Rect;
        public int MaxQueuedEvents = 5;

        public Button m_Button;
        public RingBuffer<EventActor> QueuedActors = new RingBuffer<EventActor>(5);
        [NonSerialized] public Routine m_Routine;
        [NonSerialized] public int TicksSinceFired = 0;

        protected override void Awake() {
            base.Awake();

            m_Routine.Replace(GlobalAlertUtility.DisappearRoutine(this));
            m_Button.onClick.AddListener(HandleButtonClicked);

            ZavalaGame.SaveBuffer.RegisterHandler("GlobalAlert", this);
            ZavalaGame.SaveBuffer.RegisterPostLoad(this);
        }

        protected override void OnDestroy() {
            m_Routine.Stop();
            m_Button.onClick.RemoveListener(HandleButtonClicked);

            ZavalaGame.SaveBuffer.DeregisterHandler("GlobalAlert");
            ZavalaGame.SaveBuffer.DeregisterPostLoad(this);

            base.OnDestroy();
        }

        #region Handlers

        private void HandleButtonClicked() {
            if (!QueuedActors.TryPopFront(out EventActor actor)) {
                Log.Warn("[GlobalAlertButton] Couldn't pop front of QueuedActors :( Button showing without actors queued?");
            }

            Assert.NotNull(actor);

            Log.Msg("[GlobalAlertButton] Popped actor {0}", actor.gameObject.name);
            if (actor.QueuedEvents.TryPeekFront(out EventActorQueuedEvent evt)) {
                ZavalaGame.Events.Dispatch(GameEvents.GlobalAlertClicked, EvtArgs.Box(new AlertData(actor, evt)));
                actor.QueuedEvents.MoveFrontToBackWhere(e => e.Alert == EventActorAlertType.Dialogue);
            }

            EventActorUtility.TriggerActorAlert(actor);
            UIAlertUtility.ClearAlert(actor.DisplayingEvent);
            TicksSinceFired = 0;
            UpdateButtonRoutine();
            m_Routine.Replace(GlobalAlertUtility.DisappearRoutine(this));
            SimTimeInput.SetPaused(false, SimPauseFlags.PendingGlobalAlert);

            // UIAlertUtility.ClickAlert(Actor);
        }

        public void UpdateButtonRoutine() {
            EventActor actor;
            EventActorQueuedEvent evt;

            // check if event overrides delay. If so, override the global alert delay
            bool overrideDelay = QueuedActors.Count > 0
                && QueuedActors.TryPeekFront(out actor)
                && actor.QueuedEvents.TryPeekFront(out evt)
                && evt.OverrideDelay;

            if (!overrideDelay && TicksSinceFired < GlobalAlertParams.GlobalAlertDelay) return;
            if (QueuedActors.Count > 0) {
                m_Routine.Replace(GlobalAlertUtility.AppearRoutine(this));
                SimTimeInput.SetPaused(true, SimPauseFlags.PendingGlobalAlert);
                if (QueuedActors.TryPeekFront(out actor) && actor.QueuedEvents.TryPeekFront(out evt)) {
                    ZavalaGame.Events.Dispatch(GameEvents.GlobalAlertAppeared, EvtArgs.Box(new AlertData(actor, evt)));
                }
                // UIAlertUtility.SetAlertFaded(QueuedActors.PeekFront().DisplayingEvent, true);
            } else {
                // SimTimeInput.SetPaused(false, SimPauseFlags.PendingGlobalAlert);               
            }
        }
        


        void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write(TicksSinceFired);

            writer.Write((byte) QueuedActors.Count);
            for(int i = 0; i < QueuedActors.Count; i++) {
                writer.Write(QueuedActors[i].Id.Hash());
            }
        }

        void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            reader.Read(ref TicksSinceFired);

            QueuedActors.Clear();

            int queuedActorCount = reader.Read<byte>();
            var alertIds = scratch.CreateBlock<StringHash32>("GlobalAlertIds", queuedActorCount);
            for(int i = 0; i < queuedActorCount; i++) {
                alertIds[i] = reader.Read<StringHash32>();
            }
        }

        void ISaveStatePostLoad.PostLoad(SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            var alertIds = scratch.GetBlock<StringHash32>("GlobalAlertIds");
            for(int i = 0; i < alertIds.Length; i++) {
                QueuedActors.PushBack(ScriptUtility.LookupActor(alertIds[i]));
            }
        }

        #endregion // Handlers
    }

    public static class GlobalAlertUtility {

        public static void TickGlobalAlertDelay(GlobalAlertButton button) {
            if (button.TicksSinceFired < GlobalAlertParams.GlobalAlertDelay) {
                button.TicksSinceFired++;
                Log.Debug("[GlobalAlertButton] Ticking global alert delay... {0} of {1}", button.TicksSinceFired, GlobalAlertParams.GlobalAlertDelay);
            }
            if (button.TicksSinceFired >= GlobalAlertParams.GlobalAlertDelay) {
                button.UpdateButtonRoutine();
            }
        }

        // static private readonly string[] RegionIndexToString = Enum.GetNames(typeof(RegionId));
        public static void PushEventOfActorToGlobal(GlobalAlertButton button, EventActor actor) {
            Assert.NotNull(actor, "Cannot create event for null actor");
            Log.Msg("[GlobalAlertUtility] Pushing actor '{0}'", actor.gameObject.FullPath());
            button.QueuedActors.PushBack(actor);
            if (actor.DisplayingEvent && actor.DisplayingEvent.AlertType != EventActorAlertType.Dialogue) {
                actor.DisplayingEvent.KeepFaded = true;
                UIAlertUtility.SetAlertFaded(actor.DisplayingEvent, true);
            }
            button.UpdateButtonRoutine();
        }

        // TODO: add a special case for region unlock "alerts"
        public static void CreateEventForGlobal(GlobalAlertButton button, EventActor actor, StringHash32 nodeId, bool overrideDelay) {
            Assert.NotNull(actor, "Cannot create global event for null actor");
            EventActorQueuedEvent fakeEvent = new() {
                ScriptId = nodeId,
                Alert = EventActorAlertType.GlobalDummy,
                OverrideDelay = overrideDelay
            };
            actor.QueuedEvents.PushFront(fakeEvent);
            PushEventOfActorToGlobal(button, actor);
        }

        [LeafMember("SendGlobalAlertForNode")]
        public static void GlobalAlertLeaf(StringHash32 actorId, StringHash32 scriptId, bool overrideDelay = false) {
            EventActor actor = ScriptUtility.LookupActor(actorId);
            Assert.NotNull(actor, "Cannot create global event for null actor with id '{0}'", scriptId);
            CreateEventForGlobal(Game.Gui.GetShared<GlobalAlertButton>(), actor, scriptId, overrideDelay);
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
    public static class GlobalAlertParams {
        [ConfigVar("Global Alert Delay", 0, 30, 2)] static public int GlobalAlertDelay = 10;
    }
}
