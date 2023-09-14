using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class RegionUnlockSystem : SharedStateSystemBehaviour<RegionUnlockState, SimGridState, SimPhosphorusState>
    {
        #region Work

        public override void ProcessWork(float deltaTime) {
            // Check for unlocks
            for (int i = 0; i < m_StateA.UnlockGroups.Count; i++) {
                UnlockGroup currUnlockGroup = m_StateA.UnlockGroups[i];

                // Implement checks
                bool passedCheck = true;

                // ALL conditions must be passed (&&, not ||)
                foreach(UnlockConditionGroup conditionGroup in currUnlockGroup.UnlockConditions) {
                    switch(conditionGroup.Type) {
                        case UnlockConditionType.AvgPhosphorus:
                            foreach(int region in conditionGroup.ChecksRegions) {
                                // TODO: would be cleaner to convert these into delegates
                                if (m_StateC.HistoryPerRegion[region].TryGetAvg(conditionGroup.Scope, out float avg)) {
                                    if (avg > conditionGroup.Threshold) {
                                        // did not meet threshold
                                        passedCheck = false;
                                    }
                                }
                                else {
                                    // not enough data -- false by default
                                    passedCheck = false;
                                }
                            }
                            break;
                        case UnlockConditionType.TotalPhosphorus:
                            foreach (int region in conditionGroup.ChecksRegions) {
                                // TODO: would be cleaner to convert these into delegates
                                if (m_StateC.HistoryPerRegion[region].TryGetTotal(conditionGroup.Scope, out int total)) {
                                    if (total > conditionGroup.Threshold) {
                                        // did not meet threshold
                                        passedCheck = false;
                                    }
                                }
                                else {
                                    // not enough data -- false by default
                                    passedCheck = false;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

                if (passedCheck) {
                    // Unlock regions
                    foreach(int region in currUnlockGroup.RegionIndexUnlocks) {
                        if (region >= m_StateB.WorldData.Regions.Length) {
                            // region to unlock not registered
                            continue;
                        }

                        SimDataUtility.LoadAndRegenRegionDataFromWorld(m_StateB, m_StateB.WorldData, region);
                        // TODO: trigger RegionUnlocked for scripting purposes
                        using (TempVarTable varTable = TempVarTable.Alloc()) {
                            varTable.Set("regionId", ((RegionId)region).ToString());
                            ScriptUtility.Trigger(GameTriggers.RegionUnlocked, varTable);
                        }
                        Debug.Log("[RegionUnlockSystem] Unlocked region " + region);
                    }

                    // Remove this from list of unlock conditions
                    m_StateA.UnlockGroups.RemoveAt(i);
                    i--;
                }
            }
        }

        #endregion // Work
    }
}

