using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.Economy
{
    public class BlueprintSystem : SharedStateSystemBehaviour<BlueprintState, ShopState, SimGridState>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Process Build button clicked
            if (m_StateA.NewBuildConfirmed)
            {
                m_StateA.NewBuildConfirmed = false;

                BlueprintUtility.ConfirmBuild(m_StateA, m_StateB, m_StateC.CurrRegionIndex);
            }
        }
    }
}
