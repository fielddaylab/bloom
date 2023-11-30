using FieldDay.Systems;
using FieldDay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Input
{
    // Process after UserInteractionSystem
    [SysUpdate(GameLoopPhase.Update, 500)]
    public class InteractResetSystem : SharedStateSystemBehaviour<InteractionState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Reset any triggers that were fired

            if (m_State.InteractionUpdated)
            {
                m_State.InteractionUpdated = false;
            }
        }
    }
}