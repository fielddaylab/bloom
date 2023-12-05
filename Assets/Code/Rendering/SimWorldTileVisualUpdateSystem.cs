using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Roads;
using Zavala.Sim;

namespace Zavala.World {

    [SysUpdate(GameLoopPhase.LateUpdate, 1)]
    public sealed class SimWorldTileVisualUpdateSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, RoadNetwork> {
        #region Inspector

        #endregion // Inspector

        #region Work

        public override void ProcessWork(float deltaTime) {
            VisualUpdateRecord record;

            int count = m_StateA.QueuedVisualUpdates.Count;
            while(count-- > 0 && m_StateA.QueuedVisualUpdates.TryPopFront(out record)) {
                int region = m_StateB.Terrain.Regions[record.TileIndex];
                TerrainTileInfo info = m_StateB.Terrain.Info[record.TileIndex];
                switch (record.Type) {
                    case VisualUpdateType.Road: {
                        if (info.Category == TerrainCategory.Land) {
                            TileInstance t = m_StateA.Tiles[record.TileIndex];
                            bool hasRoad = (m_StateC.Roads.Info[record.TileIndex].Flags & RoadFlags.IsRoad) != 0;
                            if (hasRoad) {
                                m_StateA.Palettes[region].TileTopEmptyMesh.Apply(t.TopRenderer, t.TopFilter);
                            } else {
                                t.TopDefaultConfig.Apply(t.TopRenderer, t.TopFilter);
                            }
                        }
                        break;
                    }
                    case VisualUpdateType.Water: {
                        WaterTile wTile = m_StateA.Tiles[record.TileIndex].GetComponent<WaterTile>();
                        WaterTileUtility.UpdateWaterfallEdges(wTile, m_StateB, Game.SharedState.Get<WaterMaterialData>());
                        break;
                    }

                    default: {
                        m_StateA.QueuedVisualUpdates.PushBack(record);
                        break;
                    }
                }
            }
        }

        #endregion // Work
    }
}