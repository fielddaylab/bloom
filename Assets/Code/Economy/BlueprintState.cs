using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.UI;

namespace Zavala.Economy
{
    public class BlueprintState : SharedStateComponent, IScenePreload
    {
        private UIBlueprint m_BlueprintUI;

        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            m_BlueprintUI = FindAnyObjectByType<UIBlueprint>(FindObjectsInactive.Include);
            return null;
        }

        public void UpdateRunningCostDisplay(int runningCost, int deltaCost, long playerFunds)
        {
            m_BlueprintUI.UpdateTotalCost(runningCost, deltaCost, playerFunds);
        }
    }
}