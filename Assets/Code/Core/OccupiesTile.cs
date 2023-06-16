using System;
using UnityEngine;
using FieldDay.Components;
using Zavala.World;
using FieldDay;
using Zavala.Sim;

namespace Zavala {
    [DisallowMultipleComponent]
    public sealed class OccupiesTile : MonoBehaviour, IComponentData {
        [NonSerialized] public int TileIndex;
        [NonSerialized] public HexVector TileVector;
        [NonSerialized] public ushort RegionIndex;

        private void OnEnable() {
            SimWorldUtility.TryGetTilePosFromWorld(ZavalaGame.SimGrid, ZavalaGame.SimWorld, transform.position, out TileVector);
            TileIndex = ZavalaGame.SimGrid.HexSize.FastPosToIndex(TileVector);
            RegionIndex = ZavalaGame.SimGrid.Terrain.Regions[TileIndex];
        }
    }
}