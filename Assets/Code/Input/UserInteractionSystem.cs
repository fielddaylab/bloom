using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zavala.Input {
    public class UserInteractionSystem : SharedStateSystemBehaviour<InteractionState>
    {
        public override void ProcessWork(float deltaTime)
        {
            if (m_State.InteractionUpdated)
            {
                // Activate relevant interaction filters
                foreach(var filter in m_State.Filters)
                {
                    filter.Raycaster.enabled = (filter.InteractMask & m_State.AllowedInteractions) != 0;
                }
            }
        }
    }
}