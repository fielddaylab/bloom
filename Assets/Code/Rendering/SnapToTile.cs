using FieldDay;
using FieldDay.Components;
using UnityEngine;
using Zavala.World;

namespace Zavala {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class SnapToTile : MonoBehaviour {
        public float HeightOffset;

        private void OnEnable() {
            GameLoop.QueuePreUpdate(Snap);
        }

        private void Snap() {
            HexVector pos = GetComponent<OccupiesTile>().TileVector;
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            worldPos.y += HeightOffset;
            transform.position = worldPos;
        }
    }
}