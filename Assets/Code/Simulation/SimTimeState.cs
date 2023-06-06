using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Zavala.Sim {
    /// <summary>
    /// Shared simulation time state.
    /// </summary>
    public sealed class SimTimeState : SharedStateComponent {
        /// <summary>
        /// Time scale at which the simulation elements should animate and update.
        /// </summary>
        public float TimeScale = 1;

        /// <summary>
        /// What has paused the simulation.
        /// </summary>
        [AutoEnum] public SimPauseFlags Paused = 0;
    }

    [Flags]
    public enum SimPauseFlags : uint {
        User = 0x01,
        Event = 0x02
    }

    /// <summary>
    /// Lightweight struct representing a timer bound to the simulation state.
    /// </summary>
    [Serializable]
    public struct SimTimer {
        public float Period;
        [NonSerialized] public float Accumulator;
        [NonSerialized] public uint PeriodIndex;
        [NonSerialized] public ushort AdvancedOnFrame;

        /// <summary>
        /// Advances this timer by a simulation-scaled delta time.l
        /// Returns if this timer advance to its next period.
        /// </summary>
        public bool Advance(float deltaTime, SimTimeState timeState) {
            if (SimTimeUtility.AdvanceCycle(ref Accumulator, Period, deltaTime, timeState)) {
                PeriodIndex++;
                AdvancedOnFrame = Frame.Index;
                return true;
            }

            AdvancedOnFrame = Frame.InvalidIndex;
            return false;
        }

        /// <summary>
        /// Has this timer advanced this frame?
        /// </summary>
        public bool HasAdvanced() {
            return AdvancedOnFrame == Frame.Index;
        }

        /// <summary>
        /// Clears the advanced flag.
        /// </summary>
        public void ClearAdvancedOnFrame() {
            AdvancedOnFrame = Frame.InvalidIndex;
        }
    }

    /// <summary>
    /// Simulation time utility methods.
    /// </summary>
    static public class SimTimeUtility {
        /// <summary>
        /// Advances a timer by simulation-scaled delta time.
        /// Returns if the timer advanced to its next period.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool AdvanceCycle(ref float cycleTimer, float cyclePeriod, float deltaTime, SimTimeState timeState) {
            deltaTime *= timeState.TimeScale;
            if (timeState.Paused != 0 || deltaTime <= 0) {
                return false;
            }

            cycleTimer += deltaTime;
            if (cycleTimer > cyclePeriod) {
                cycleTimer -= cyclePeriod;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Advances a timer by simulation-scaled delta time.
        /// Returns if the timer advanced to its next period.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float AdjustedDeltaTime(float deltaTime, SimTimeState timeState) {
            if (timeState.Paused != 0) {
                return 0;
            }

            return deltaTime * timeState.TimeScale;
        }
    }
}