using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.Data;
using Zavala.UI.Tutorial;

namespace Zavala.Sim
{
    public class TutorialState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject
    {
        public enum State {
            InactiveSim, // deactivate sim forces until basic tutorial (connecting roads) is completed
            ActiveSim,
            Completed
        }

        [System.Flags]
        public enum Flags : uint {
            ValidRoadPreviewed = 0x01,
            GameResumedFromPause = 0x02
        }

        public State CurrState = State.InactiveSim;
        public Flags CurrFlags = 0;

        public bool AddFlag(Flags flag) {
            if ((CurrFlags & flag) == 0) {
                CurrFlags |= flag;
                return true;
            }

            return false;
        }

        [LeafMember("ActivateSim")]
        static public void ActivateSim() {
            TutorialState tutorial = Game.SharedState.Get<TutorialState>();
            tutorial.CurrState = State.ActiveSim;
        }

        [LeafMember("ShowAnimatedTutorial")]
        static public void ShowPanel(string tutorialName) {
            Game.Gui.GetShared<TutorialPanel>().Open(tutorialName);
        }

        [LeafMember("HideAnimatedTutorial")]
        static public void HidePanel() {
            Game.Gui.GetShared<TutorialPanel>().Close();
        }

        static public void HidePanelWithName(string tutorialName) {
            Game.Gui.GetShared<TutorialPanel>().Close(tutorialName);
        }

        [LeafMember("HasTutorialFlag")]
        static public bool HasTutorialFlag(Flags flag) {
            return (Find.State<TutorialState>().CurrFlags & flag) != 0;
        }

        [LeafMember("SetTutorialFlag")]
        static public void SetTutorialFlag(Flags flag) {
            Find.State<TutorialState>().CurrFlags |= flag;
        }

        [DebugMenuFactory]
        static private DMInfo SkipTutorialMenu() {
            DMInfo menu = new DMInfo("Tutorial");
            menu.AddButton("Skip Tutorial", () => {
                ScriptUtility.Trigger(GameTriggers.TutorialSkipped);
            });
            return menu;
        }

        unsafe void IRegistrationCallbacks.OnRegister() {
            ZavalaGame.SaveBuffer.RegisterHandler("Tutorial", this);
        }

        void IRegistrationCallbacks.OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("Tutorial");
        }

        unsafe void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            writer.Write(CurrState);
            writer.Write(CurrFlags);
        }

        unsafe void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            reader.Read(ref CurrState);
            if (consts.Version >= 2) {
                reader.Read(ref CurrFlags);
            }
        }
    }
}