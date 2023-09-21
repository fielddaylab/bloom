using FieldDay.Systems;
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
                if ((m_StateA.Paused & SimPauseFlags.User) != 0) {
                    m_StateA.Paused &= ~SimPauseFlags.User;
                    m_StateA.PauseOverlay.gameObject.SetActive(false);
                } else {
                    m_StateA.Paused |= SimPauseFlags.User;
                    m_StateA.PauseOverlay.gameObject.SetActive(true);
                }
            }
        }
    }
}