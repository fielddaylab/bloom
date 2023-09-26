using FieldDay.Systems;
using Leaf.Runtime;
using UnityEngine;
using Zavala.Input;

namespace Zavala.Sim {
    public sealed class SimTimeInput : SharedStateSystemBehaviour<SimTimeState, InputState> {
        public override void ProcessWork(float deltaTime) {
            if (m_StateB.ButtonDown(InputButton.FastForward)) {
                m_StateA.TimeScale = 4;
            } else {
                m_StateA.TimeScale = 1;
            }

            if (m_StateB.ButtonPressed(InputButton.Pause)) {
                TogglePause(SimPauseFlags.User);
            }
        }

        public static void SetPaused(bool pause, SimPauseFlags flag) {
            SimTimeState time = FieldDay.Game.SharedState.Get<SimTimeState>();
            if (pause) {
                time.Paused |= flag;
                time.PauseOverlay.gameObject.SetActive(true);
            } else {
                if (time.Paused > flag) {
                    Debug.Log("[SimTimeInput] User attempting to unpause an event pause, ignoring...");
                    // player attempts to unpause an event pause
                    return;
                }
                time.Paused = 0;
                time.PauseOverlay.gameObject.SetActive(false);
            }
        }

        public static void TogglePause(SimPauseFlags flag) {
            SimTimeState time = FieldDay.Game.SharedState.Get<SimTimeState>();
            SetPaused(time.Paused == 0, flag);
        }

        [LeafMember("TogglePause")]
        public static void TogglePauseEvent() {
            TogglePause(SimPauseFlags.Event);
        }

        [LeafMember("Pause")]
        public static void PauseEvent() {
            SetPaused(true, SimPauseFlags.Event);
        }

        [LeafMember("Unpause")]
        public static void UnpauseEvent() {
            SetPaused(false, SimPauseFlags.Event);
        }

    }
}