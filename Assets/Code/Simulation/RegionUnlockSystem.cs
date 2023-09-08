using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Sim
{
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class RegionUnlockSystem : SharedStateSystemBehaviour<RegionUnlockState, SimGridState>
    {
        #region Work

        public override void ProcessWork(float deltaTime) {
            // Check for unlocks
            for (int i = 0; i < m_StateA.UnlockGroups.Count; i++) {
                UnlockGroup currGroup = m_StateA.UnlockGroups[i];

                // TODO: implement checks
                bool passedCheck = false;

                if (passedCheck) {
                    // Unlock regions
                    foreach(int region in currGroup.RegionIndexUnlocks) {
                        if (region >= m_StateB.WorldData.Regions.Length) {
                            // region to unlock not registered
                            continue;
                        }

                        SimDataUtility.LoadAndRegenRegionDataFromWorld(m_StateB, m_StateB.WorldData, region);
                        // TODO: trigger RegionUnlocked for scripting purposes
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

