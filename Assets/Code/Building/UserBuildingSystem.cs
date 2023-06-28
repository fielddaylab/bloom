using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.Roads;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Building {
    // TODO: is this the right update phase?
    [SysUpdate(GameLoopPhase.Update)]
    public class UserBuildingSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BuildToolState> {
        public override void ProcessWork(float deltaTime) {
            UserBuildTool tool = ClickedWithTool();
            if (tool != UserBuildTool.None) {
                SimWorldState world = ZavalaGame.SimWorld;
                SimGridState grid = ZavalaGame.SimGrid;
                TryBuildTile(grid, tool, RaycastTileIndex(world, grid));;
            }
        }

        /// <summary>
        /// Check if player clicked with a tool selected
        /// </summary>
        private UserBuildTool ClickedWithTool() {
            // if mouse pressed
            // TODO: change to if ButtonDown && position significantly different from pressed position
            if (m_StateA.ButtonPressed(InputButton.PrimaryMouse) || m_StateA.MouseDragging)
                return m_StateC.ActiveTool;
            return UserBuildTool.None;
        }
        /// <summary>
        /// Try to get a tile index by raycasting to a tile. 
        /// </summary>
        private int RaycastTileIndex(SimWorldState world, SimGridState grid) {
            // do a raycast
            // TODO: only raycast if the mouse has moved significantly since last placed tile?
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            // TODO: 100 units a reasonable max raycast distance?
            if (Physics.Raycast(mouseRay, out RaycastHit hit, 100f)) {
                if (!hit.collider.TryGetComponent<TileInstance>(out var tile)) return -1;
                HexVector vec = HexVector.FromWorld(hit.collider.transform.position, world.WorldSpace);
                if (vec.Equals(m_StateC.VecPrev)){
                    // same as last
                    return -1;
                }
                int i = grid.HexSize.FastPosToIndex(vec);
                Log.Msg("[UserBuildingSystem] New raycast hit Tile {0}", i);
                m_StateC.VecPrev = vec;
                return i;
            } else {
                Log.Msg("[UserBuildingSystem] Raycast missed.");
                return -1;
            }
        }

        /// <summary>
        /// Attempt to place tile on given tile index using active tool
        /// </summary>
        private void TryBuildTile(SimGridState grid, UserBuildTool activeTool, int tileIndex) {
            if (tileIndex < 0) {
                Log.Msg("[UserBuildingSystem] Invalid build location.");
                return;
            }
            switch (activeTool) {
                // TODO: Add building costs

                case UserBuildTool.Destroy:
                    // TODO: Add road removal
                    break;
                case UserBuildTool.Road:
                    // TODO: check if the tile is buildable
                    RoadUtility.AddRoad(Game.SharedState.Get<RoadNetwork>(), grid, tileIndex);
                    break;
                default:
                    break;
            }
        }
    }
}