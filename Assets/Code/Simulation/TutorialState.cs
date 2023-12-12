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