using System;
using UnityEngine;
using FieldDay.Components;
using Zavala.World;
using FieldDay;
using Zavala.Sim;
using BeauUtil.Debugger;

namespace Zavala {
    [DisallowMultipleComponent]
    public sealed class OccupiesTile : MonoBehaviour, IComponentData {
        [NonSerialized] public int TileIndex;
        [NonSerialized] public HexVector TileVector;
        [NonSerialized] public ushort RegionIndex;

        private void OnEnable() {
            RefreshData();
            Game.Events.Register(SimGridState.Event_RegionUpdated, RefreshData);
        }

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }
            Game.Events.Deregister(SimGridState.Event_RegionUpdated, RefreshData);
        }

        private void RefreshData() {
            SimWorldUtility.TryGetTilePosFromWorld(ZavalaGame.SimGrid, ZavalaGame.SimWorld, transform.position, out TileVector);
            TileIndex = ZavalaGame.SimGrid.HexSize.FastPosToIndex(TileVector);
            RegionIndex = ZavalaGame.SimGrid.Terrain.Regions[TileIndex];
            Assert.True(RegionIndex < RegionInfo.MaxRegions, "Region Index {0} is out of range", RegionIndex);
        }
    }
}