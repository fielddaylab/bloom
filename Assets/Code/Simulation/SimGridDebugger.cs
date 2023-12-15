using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Debugging {

    [SysUpdate(GameLoopPhase.DebugUpdate, 1000)]
    public class SimGridDebugger : SharedStateSystemBehaviour<SimGridState, SimWorldState, SimWorldCamera, RoadNetwork>, IDevModeOnly {
        public override bool HasWork() {
            return s_MenuActive && base.HasWork();
        }

        public override void ProcessWork(float deltaTime) {
            Ray ray = m_StateC.Camera.ScreenPointToRay(UnityEngine.Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);
            if (!TryRaycastBuilding(ray)) {
                TryRaycastTile(ray);
            }

            for (int i = 0; i < m_StateA.RegionCount; i++) {
                DebugDraw.AddBounds(m_StateB.RegionBounds[i], (m_StateB.RegionCullingMask & (1 << i)) != 0 ? Color.green : Color.red, 1, 0, true, -1);
            }
        }

        private bool TryRaycastBuilding(Ray ray) {
            bool hit = Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMasks.Building_Mask, QueryTriggerInteraction.Collide);
            if (hit) {
                Bounds hitBounds = hitInfo.collider.bounds;
                Vector3 hitCenter = hitBounds.center;
                DebugDraw.AddPoint(hitCenter, 0.1f, Color.white.WithAlpha(0.5f));
                OccupiesTile occupies = hitInfo.collider.GetComponent<OccupiesTile>();
                DebugDraw.AddWorldText(hitCenter, string.Format("{0}\nPosition {1} [{2}]\nRegion {3}\nFlags {4}\nRoad {5}\nConnections {6}", occupies.gameObject.name, occupies.TileVector, occupies.TileIndex, occupies.RegionIndex, m_StateA.Terrain.Info[occupies.TileIndex].Flags, m_StateD.Roads.Info[occupies.TileIndex].Flags, m_StateD.Roads.Info[occupies.TileIndex].FlowMask), Color.white, 0, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);

                if (!m_StateD.UpdateNeeded) {
                    RoadSourceInfo sourceData = m_StateD.Sources.Find(RoadUtility.FindSourceByTileIndex, (ushort) occupies.TileIndex);
                    if (sourceData.Connections.Length > 0) {
                        for (int i = 0; i < sourceData.Connections.Length; i++) {
                            UnsafeSpan<ushort> path = sourceData.Connections[i].Tiles;
                            for(int j = 1; j < path.Length; j++) {
                                ushort prev = path[j - 1];
                                ushort now = path[j];

                                Vector3 prevPos = SimWorldUtility.GetTileCenter(prev) + Vector3.up * 0.3f;
                                Vector3 nowPos = SimWorldUtility.GetTileCenter(now) + Vector3.up * 0.3f;

                                DebugDraw.AddLine(prevPos, nowPos, Color.yellow, 1);
                            }
                        }
                    }
                }
            }
            return hit;
        }

        private bool TryRaycastTile(Ray ray) {
            bool hit = Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMasks.HexTile_Mask, QueryTriggerInteraction.Ignore);
            if (hit) {
                Bounds hitBounds = hitInfo.collider.bounds;
                Vector3 hitCenter = hitBounds.center;
                hitCenter.y = hitBounds.max.y;
                DebugDraw.AddPoint(hitCenter, 0.1f, Color.white.WithAlpha(0.5f));
                HexVector point = HexVector.FromWorld(hitCenter, m_StateB.WorldSpace);
                int tileIdx = m_StateA.HexSize.FastPosToIndex(point);
                DebugDraw.AddWorldText(hitCenter, string.Format("Position {0} [{1}]\nHeight {2}\nRegion {3}\nFlags {4}\nRoad {5}\nConnections {6}", point, tileIdx, m_StateA.Terrain.Height[tileIdx], m_StateA.Terrain.Regions[tileIdx], m_StateA.Terrain.Info[tileIdx].Flags, m_StateD.Roads.Info[tileIdx].Flags, m_StateD.Roads.Info[tileIdx].FlowMask), Color.white, 0, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);

                if ((m_StateA.Terrain.Info[tileIdx].Flags & TerrainFlags.IsWater) != 0) {
                    SimBuffer<AlgaeTileState> algae = Game.SharedState.Get<SimAlgaeState>().Algae.State;
                    DebugDraw.AddWorldText(hitCenter + Vector3.down, string.Format("Algae: {0}\nHasPeaked: {1}", algae[tileIdx].PercentAlgae, algae[tileIdx].IsPeaked), Color.white, 0, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);;
                }

            }
            return hit;
        }

        static private bool s_MenuActive;

        [DebugMenuFactory]
        static private DMInfo Debug_CreateMenu() {
            DMInfo menu = new DMInfo("Hex Grid", 8);
            menu.OnEnter.Register(() => s_MenuActive = true);
            menu.OnExit.Register(() => s_MenuActive = false);
            return menu;
        }
    }
}