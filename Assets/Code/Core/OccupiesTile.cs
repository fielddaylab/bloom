using System;
using UnityEngine;
using FieldDay.Components;
using Zavala.World;
using FieldDay;
using Zavala.Sim;
using BeauUtil.Debugger;
using Zavala.Building;
using BeauPools;

namespace Zavala {
    [DisallowMultipleComponent, DefaultExecutionOrder(-5)]
    public sealed class OccupiesTile : MonoBehaviour, IComponentData, IPoolAllocHandler, IPoolConstructHandler {
        [SerializeField] public BuildingType Type;
        
        [NonSerialized] public int TileIndex;
        [NonSerialized] public HexVector TileVector;
        [NonSerialized] public ushort RegionIndex;

        public bool IsExternal = false; // true if external commercial fertilizer seller/export depot
        public bool Pending; // true if this building was built in BP mode without being confirmed

        private void OnEnable() {
            RefreshData();
            Game.Events.Register(SimGridState.Event_RegionUpdated, RefreshData);
            Game.Components.Register(this);
        }

        private void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }
            Game.Components.Deregister(this);
            Game.Events.Deregister(SimGridState.Event_RegionUpdated, RefreshData);
        }

        private void RefreshData() {
            if (IsExternal) {
                return;
            }

            SimWorldUtility.TryGetTilePosFromWorld(ZavalaGame.SimGrid, ZavalaGame.SimWorld, transform.position, out TileVector);
            TileIndex = ZavalaGame.SimGrid.HexSize.FastPosToIndex(TileVector);
            RegionIndex = ZavalaGame.SimGrid.Terrain.Regions[TileIndex];
            Assert.True(RegionIndex < RegionInfo.MaxRegions, "Region Index {0} is out of range", RegionIndex);
        }

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            gameObject.SetActive(false);
        }

        void IPoolConstructHandler.OnConstruct() {
            gameObject.SetActive(false);
        }

        void IPoolConstructHandler.OnDestruct() {
        }
    }
}