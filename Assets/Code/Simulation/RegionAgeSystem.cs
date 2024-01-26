using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.World;
using FieldDay.Scripting;
using System;
using Zavala.UI;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, -50, ZavalaGame.SimulationUpdateMask)]
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
                int age = ++m_StateB.Regions[i].Age;
                CheckTrigger((RegionId) i, age);
            }

            // TODO: Find a better place for this? Could create a system, but GlobalAlertButton isn't a SharedState
            GlobalAlertUtility.TickGlobalAlertDelay(Game.Gui.GetShared<GlobalAlertButton>());
        }

        private void CheckTrigger(RegionId region, int age) {
            //Debug.Log("[RegionAgeSystem] Checking... "+age);
            int triggerAge = m_StateA.AgeTriggers[region];
            if (triggerAge > 0 && age >= triggerAge) {
                Debug.Log("[RegionAgeSystem] Sending Trigger: "+region+" aged "+age);
                using (TempVarTable varTable = TempVarTable.Alloc()) {
                    varTable.Set("regionId", (int)region + 1); //0-indexed to 1-indexed
                    varTable.Set("age", age);
                    ScriptUtility.Trigger(GameTriggers.RegionReachedAge, varTable);
                }
                m_StateA.AgeTriggers[region] = 0;
            }
        }

        #endregion // Work

    }
}