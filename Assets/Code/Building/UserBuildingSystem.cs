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
    [SysUpdate(GameLoopPhase.FixedUpdate)]
    public class UserBuildingSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, RoadNetwork, SimGridState> {
        public override void ProcessWork(float deltaTime) {
            if (ClickedToBuild()) {
                TryBuildTile(m_StateA.ActiveTool, RaycastTileIndex());
            }
        }

        /// <summary>
        /// Check if player clicked with a tool selected
        /// </summary>
        private bool ClickedToBuild() {
            // if mouse pressed
            // TODO: change to if ButtonDown && position significantly different from pressed position
            if (m_StateA.ButtonPressed(InputButton.PrimaryMouse)) {
                // and there is an active tool
                if (m_StateA.ActiveTool != UserBuildTool.None) {
                    return true;
                } else {
                    Log.Msg("[UserBuildingSystem] no tool selected!");
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// Try to get a tile index by raycasting to a tile. 
        /// </summary>
        private int RaycastTileIndex() {
            // do a raycast
            Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
            // TODO: 100 units a reasonable max raycast distance?
            if (Physics.Raycast(mouseRay, out RaycastHit hit, 100f)) {
                TileInstance tile = hit.collider.GetComponent<TileInstance>();
                if (tile == null) return -1;
                int TileIndexNew = tile.index;
                int PrevIndex = m_StateA.TileIndexPrev;
                if (TileIndexNew != PrevIndex) {
                    m_StateA.TileIndexPrev = TileIndexNew;
                    Log.Msg("[UserBuildingSystem] New raycast hit Tile {0}", TileIndexNew);
                } else {
                    Log.Msg("[UserBuildingSystem] Same as last raycast.");
                }
                return TileIndexNew;
            } else {
                Log.Msg("[UserBuildingSystem] Raycast missed.");
                return -1;
            }
        }

        /// <summary>
        /// Attempt to place
        /// </summary>
        private void TryBuildTile(UserBuildTool activeTool, int tileIndex) {
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
                    RoadUtility.AddRoad(m_StateC, m_StateD, tileIndex);
                    m_StateC.UpdateNeeded = true;
                    break;
                default:
                    break;
            }
        }
    }
}