using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World
{
    public class ExportRevealState : SharedStateComponent, IRegistrationCallbacks
    {
        [NonSerialized] public List<GameObject>[] ObstructionsPerRegion;
        [NonSerialized] public GameObject[] DepotsPerRegion;

        public void OnRegister()
        {
            ObstructionsPerRegion = new List<GameObject>[RegionInfo.MaxRegions];
            for (int i = 0; i <  RegionInfo.MaxRegions; i++)
            {
                ObstructionsPerRegion[i] = new List<GameObject>();
            }

            DepotsPerRegion = new GameObject[RegionInfo.MaxRegions];
        }

        public void OnDeregister()
        {
        }
    }
}