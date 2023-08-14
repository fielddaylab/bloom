using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.World;

namespace Zavala {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class SnapToTile : MonoBehaviour {
        public float HeightOffset;
        [SerializeField] private bool m_initial; // whether this object is enabled before the first update

        private void OnEnable() {
            if (m_initial) {
                // wait for world to finish setting up
                GameLoop.QueuePreUpdate(Snap);
            }
            else {
                // snap to tile as soon as possible
                Snap();
            }
        }

        private void Snap() {
            HexVector pos = GetComponent<OccupiesTile>().TileVector;
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            worldPos.y += HeightOffset;
            transform.position = worldPos;
        }
    }
}