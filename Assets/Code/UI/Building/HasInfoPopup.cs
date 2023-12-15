using System;
using BeauUtil;
using UnityEngine;

namespace Zavala.UI.Info {
    [RequireComponent(typeof(OccupiesTile)), DisallowMultipleComponent]
    [RequireComponent(typeof(LocationDescription))]
    public class HasInfoPopup : MonoBehaviour {
        [NonSerialized] public OccupiesTile Position;

        private void Awake() {
            Position = GetComponent<OccupiesTile>();
        }
    }
}