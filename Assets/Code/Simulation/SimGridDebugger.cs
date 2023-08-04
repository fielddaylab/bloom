using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Debugging {

    [SysUpdate(GameLoopPhase.DebugUpdate, 1000)]
    public class SimGridDebugger : SharedStateSystemBehaviour<SimGridState, SimWorldState, SimWorldCamera>, IDevModeOnly {
        public override bool HasWork() {
            return s_MenuActive && base.HasWork();
        }

        public override void ProcessWork(float deltaTime) {
            Ray ray = m_StateC.Camera.ScreenPointToRay(UnityEngine.Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);
            if (!TryRaycastBuilding(ray)) {
                TryRaycastTile(ray);
            }
        }

        private bool TryRaycastBuilding(Ray ray) {
            bool hit = Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMasks.Building_Mask, QueryTriggerInteraction.Collide);
            if (hit) {
                Bounds hitBounds = hitInfo.collider.bounds;
                Vector3 hitCenter = hitBounds.center;
                DebugDraw.AddPoint(hitCenter, 0.1f, Color.white.WithAlpha(0.5f));
                OccupiesTile occupies = hitInfo.collider.GetComponent<OccupiesTile>();
                DebugDraw.AddWorldText(hitCenter, string.Format("{0}\nPosition {1} [{2}]\nRegion {3}", occupies.gameObject.name, occupies.TileVector, occupies.TileIndex, occupies.RegionIndex), Color.white, 0, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
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
                DebugDraw.AddWorldText(hitCenter, string.Format("Position {0} [{1}]\nHeight {2}\nRegion {3}", point, tileIdx, m_StateA.Terrain.Height[tileIdx], m_StateA.Terrain.Regions[tileIdx]), Color.white, 0, TextAnchor.MiddleCenter, DebugTextStyle.BackgroundDark);
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