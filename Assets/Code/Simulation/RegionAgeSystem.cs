using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.World;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, -50)]
    public class RegionAgeSystem : SharedStateSystemBehaviour<RegionAgeState, SimGridState>
    {
        public override bool HasWork() {
            if (base.HasWork()) {
                return Game.SharedState.Get<TutorialState>().CurrState >= TutorialState.State.ActiveSim;
            }
            return false;
        }

        #region Work

        public override void ProcessWork(float deltaTime) {
            // Only check unlocks every sim tick
            if (!m_StateA.SimPhosphorusAdvanced) {
                return;
            }
            else {
                m_StateA.SimPhosphorusAdvanced = false;
            }

            for (int i = 0; i < m_StateB.RegionCount; i++) {
                m_StateB.Regions[i].Age++;
            }
        }

        #endregion // Work

    }
}