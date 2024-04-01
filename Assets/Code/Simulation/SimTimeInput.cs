using FieldDay;
using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Input;

namespace Zavala.Sim {
    public sealed class SimTimeInput : SharedStateSystemBehaviour<SimTimeState, InputState> {
        public override void ProcessWork(float deltaTime) {
            //if (m_StateB.ButtonDown(InputButton.FastForward)) {
            //    // m_StateA.TimeScale = 4;
            //} else {
            //    m_StateA.TimeScale = 1;
            //}

            SimTimeState time = m_StateA;
            if ((time.Paused & (SimPauseFlags.FullscreenCutscene | SimPauseFlags.Loading)) != 0) {
                return;
            }

            if (m_StateB.ButtonPressed(InputButton.Pause)) {
                if ((time.Paused & SimPauseFlags.User) != 0) {
                    SimTimeUtility.Resume(SimPauseFlags.User | SimPauseFlags.Help, time);

                    // only if the player has an opportunity to see that they've unpaused
                    if ((time.Paused & (SimPauseFlags.Blueprints | SimPauseFlags.Cutscene | SimPauseFlags.DialogBox | SimPauseFlags.Scripted)) == 0) {
                        Find.State<TutorialState>().AddFlag(TutorialState.Flags.GameResumedFromPause);
                    }
                } else {
                    SimTimeUtility.Pause(SimPauseFlags.User, time);
                }
            }
        }

        public static void SetPaused(bool pause, SimPauseFlags flag) {
            SimTimeState time = FieldDay.Game.SharedState.Get<SimTimeState>();
            if (pause) {
                SimTimeUtility.Pause(flag, time);
            } else {
                SimTimeUtility.Resume(flag, time);
            }
        }

        public static void TogglePause(SimPauseFlags flag) {
            SimTimeState time = FieldDay.Game.SharedState.Get<SimTimeState>();
            SetPaused((time.Paused & flag) == 0, flag);
        }

        [LeafMember("TogglePause")]
        public static void TogglePauseEvent() {
            TogglePause(SimPauseFlags.Scripted);
        }

        [LeafMember("Pause")]
        public static void PauseEvent() {
            SetPaused(true, SimPauseFlags.Scripted);
        }
        [LeafMember("PauseUser")] 
        public static void PausePlayerEvent() {
            SetPaused(true, SimPauseFlags.User);
        }

        [LeafMember("Unpause")]
        public static void UnpauseEvent() {
            SetPaused(false, SimPauseFlags.Scripted);
        }

    }
}