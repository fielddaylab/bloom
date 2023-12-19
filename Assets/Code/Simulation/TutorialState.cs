using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Advisor;
using Zavala.Cards;
using Zavala.UI.Tutorial;

namespace Zavala.Sim
{
    public class TutorialState : SharedStateComponent
    {
        public enum State {
            InactiveSim, // deactivate sim forces until basic tutorial (connecting roads) is completed
            ActiveSim,
            Completed
        }

        public State CurrState = State.InactiveSim;

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

        [DebugMenuFactory]
        static private DMInfo SkipTutorialMenu() {
            DMInfo menu = new DMInfo("Tutorial");
            menu.AddButton("Skip Tutorial", () => {
                ScriptUtility.Trigger(GameTriggers.TutorialSkipped);
            });
            return menu;
        }
    }
}