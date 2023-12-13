using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OccupiesTile))]
    public sealed class SnapToTile : MonoBehaviour {
        public float HeightOffset;
        [SerializeField] private bool m_initial; // whether this object is enabled before the first update
        public bool m_hideTop; // whether this object should hide the top renderer of the tile it's placed on

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

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            if (m_hideTop) {
                OccupiesTile tile = GetComponent<OccupiesTile>();
                ZavalaGame.SimGrid.Terrain.Info[tile.TileIndex].Flags &= ~TerrainFlags.TopHidden;
                SimWorldUtility.QueueVisualUpdate((ushort) tile.TileIndex, VisualUpdateType.Building);
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
            worldPos.y += snap.HeightOffset;
            if (snap.m_hideTop) {
                ZavalaGame.SimGrid.Terrain.Info[tile.TileIndex].Flags |= TerrainFlags.TopHidden;
                SimWorldUtility.QueueVisualUpdate((ushort) tile.TileIndex, VisualUpdateType.Building);
            }
            snap.transform.position = worldPos;
        }
    }
}