using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using Zavala.Scripting;
using Zavala.Sim;

namespace Zavala.Data {
    [SysUpdate(GameLoopPhase.Update, 100)]
    public class SaveCommitSystem : SharedStateSystemBehaviour<SaveLoadState, SimPhosphorusState, WinLossState> {
        public override bool HasWork() {
            return base.HasWork() && m_StateA.TicksToCommit > 0 && !m_StateC.HasMetEnding;
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateB.Timer.HasAdvanced()) {
                if (!ZavalaGame.SaveBuffer.HasUncommittedSave) {
                    Log.Warn("[SaveCommitSystem] Save buffer discarded the uncommmitted save. Canceling.");
                    m_StateA.TicksToCommit = 0;
                } else {
                    m_StateA.TicksToCommit--;
                    Log.Msg("[SaveCommitSystem] {0} ticks before save data is committed", m_StateA.TicksToCommit);
                    if (m_StateA.TicksToCommit == 0) {
                        SaveUtility.Commit();
                    }
                }
            }
        }
    }
}