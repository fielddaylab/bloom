using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI.Info {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocationDescription))]
    public class HasInfoPopup : MonoBehaviour {
        [NonSerialized] public OccupiesTile Position;
        public BuildingType OverrideType;

        private void Awake() {
            Position = GetComponent<OccupiesTile>();
        }
    }
}