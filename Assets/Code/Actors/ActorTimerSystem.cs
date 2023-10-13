using FieldDay.Systems;
using Zavala.Sim;

namespace Zavala.Actors {
    /// <summary>
    /// Ticks all actor timers.
    /// We keep this a separate process instead of having separate timers per actor
    /// so multiple actor components can be synced up.
    /// </summary>
    [SysUpdate(FieldDay.GameLoopPhase.Update, -1, ZavalaGame.SimulationUpdateMask)]
    public sealed class ActorTimerSystem : ComponentSystemBehaviour<ActorTimer> {
        public override void ProcessWork(float deltaTime) {
            SimTimeState timeState = ZavalaGame.SimTime;
            foreach(var timer in m_Components) {
                timer.Timer.Advance(deltaTime, timeState);
            }
        }
    }
}