using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Zavala.Input
{
    /// <summary>
    /// 
    /// </summary>
    public class InteractFilter : BatchedComponent, IRegistrationCallbacks
    {
        public InteractionMask InteractMask;

        [NonSerialized] public GraphicRaycaster Raycaster;

        #region Registration

        public void OnRegister()
        {
            Raycaster = GetComponent<GraphicRaycaster>();
            if (!Raycaster)
            {
                Debug.LogWarning("[InteractFilter] Interact filter on object " + gameObject.name + " has no GraphicRaycaster to filter.");
            }

            InteractionUtility.RegisterFilter(this);
        }

        public void OnDeregister()
        {

        }

        #endregion // Registration
    }
}