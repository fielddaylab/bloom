using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {

    [SysUpdate(GameLoopPhase.LateUpdate, -1)]
    public sealed class SimWorldTileSpawnSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState> {
        #region Inspector

        #endregion // Inspector

        #region Work

        public override bool HasWork() {
            if (base.HasWork()) {
                return m_StateA.NewRegions != 0;
            }
            return false;
        }

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.NewRegions > 0) {
                int regionStart = (int) m_StateA.RegionCount - m_StateA.NewRegions;
                for(int i = 0; i < m_StateA.NewRegions; i++) {
                    int regionIdx = regionStart + i;
                    HexGridSubregion subRegion = m_StateB.Regions[regionIdx].GridArea;
                    var terrainData = m_StateB.Terrain.Info;
                    foreach(var index in subRegion) {
                        if (terrainData[index].RegionIndex != regionIdx) {
                            continue;
                        }
                        InstantiateTile(m_StateA, index, m_StateB.HexSize.FastIndexToPos(index), terrainData[index]);
                    }
                }
            } else {
                Log.Error("[SimWorldTileSpawnSystem] Number of regions decreased somehow?");
            }
        }

        static private void InstantiateTile(SimWorldState world, int index, in HexVector position, in TerrainTileInfo tileInfo) {
            Vector3 pos = HexVector.ToWorld(position, tileInfo.Height, world.WorldSpace);
            TileInstance inst;
            if ((tileInfo.Flags & TerrainFlags.IsWater) != 0) {
                inst = Instantiate(world.DefaultWaterPrefab, pos, Quaternion.identity);
            } else {
                inst = Instantiate(world.DefaultTilePrefab, pos, Quaternion.identity);
            }
            //inst.index = index;
            inst.gameObject.name = "Tile " + index.ToStringLookup();
            world.Tiles[index] = inst;
        }

        #endregion // Work
    }
}