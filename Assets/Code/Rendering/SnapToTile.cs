using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;
using Zavala.World;

namespace Zavala {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class SnapToTile : MonoBehaviour {
        public float HeightOffset;
        [SerializeField] private bool m_initial; // whether this object is enabled before the first update
        public bool m_replaceTop; // whether this object should hide the top of the pillar it occupies

        private void OnEnable() {
            if (m_initial) {
                // wait for world to finish setting up
                GameLoop.QueuePreUpdate(SnapOnEnable);
            }
            else {
                // snap to tile as soon as possible
                SnapOnEnable();
            }
        }

        private void SnapOnEnable() {
            OccupiesTile tile = GetComponent<OccupiesTile>();
            if (tile) {
                SnapUtility.Snap(this, GetComponent<OccupiesTile>());
            }
        }
    }

    public static class SnapUtility {
        public static void Snap(SnapToTile snap, OccupiesTile tile) {
            HexVector pos = tile.TileVector;
            Vector3 worldPos = SimWorldUtility.GetTileCenter(pos);
            if (snap.m_replaceTop) {
                Debug.LogWarning("[SnapToTile] Trying to replace top at Tile " + tile.TileIndex);
                if (!TileEffectRendering.ToggleTopVisibility(ZavalaGame.SimWorld.Tiles[tile.TileIndex])) {

                }
            }
            worldPos.y += snap.HeightOffset;
            snap.transform.position = worldPos;
        }
    }
}