using BeauUtil;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Zavala.Roads;
using static UnityEngine.Rendering.CoreUtils;

namespace Zavala.Economy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public class ResourceSupplierProxy : BatchedComponent
    {
        [NonSerialized] public OccupiesTile Position;
        [AutoEnum] public ResourceMask ProxyMask;

        private void Awake() {
            this.CacheComponent(ref Position);
        }

        private void Start() {
            // Ensure register road anchor happens after OccupiesTile
            RoadUtility.RegisterRoadAnchor(Position);
            RoadUtility.RegisterExportDepot(this);
        }

        protected override void OnDisable() {
            RoadUtility.DeregisterRoadAnchor(Position);
            RoadUtility.DeregisterExportDepot(this);

            base.OnDisable();
        }
    }
}