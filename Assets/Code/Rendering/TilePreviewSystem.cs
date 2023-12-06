using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Building;
using Zavala.Economy;
using Zavala.Input;
using Zavala.Sim;
using Zavala.World;

namespace Zavala.Rendering
{
    /// <summary>
    /// Controls an object that snaps to the tile under the mouse cursor
    /// </summary>
    public class TilePreviewSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BlueprintState, TilePreviewState>
    {
        private static string TILE_LAYER = "HexTile";

        public override void ProcessWork(float deltaTime)
        {
            // Render destroy bulldozer when in destroy mode
            if (m_StateC.CommandState == ActionType.Destroy)
            {
                if (m_StateD.LeadDestroyIcon == null)
                {
                    Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
                    if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, LayerMasks.HexTile_Mask))
                    {
                        if (hit.collider)
                        {
                            m_StateD.LeadDestroyIcon = Game.SharedState.Get<BuildingPools>().DestroyIcons.Alloc(hit.collider.transform.position);
                            m_StateD.LeadDestroySnap = m_StateD.LeadDestroyIcon.GetComponent<SnapToTile>();
                        }
                    }
                }
                else
                {
                    Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
                    if (!Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, LayerMasks.HexTile_Mask))
                    {
                        ClearAllDestroyIcons();
                    }
                }

                if (m_StateA.ScreenMousePos != m_StateD.PrevMousePosition)
                {
                    // Reposition the icon over the right tile
                    Ray mouseRay = m_StateB.Camera.ScreenPointToRay(m_StateA.ScreenMousePos);
                    if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, LayerMasks.HexTile_Mask))
                    {
                        if (hit.collider)
                        {
                            SimWorldState world = Game.SharedState.Get<SimWorldState>();
                            SimGridState grid = Game.SharedState.Get<SimGridState>();
                            BuildingPools pools = Game.SharedState.Get<BuildingPools>();

                            HexVector vec = HexVector.FromWorld(hit.collider.transform.position, world.WorldSpace);
                            int index = grid.HexSize.FastPosToIndex(vec);

                            m_StateD.LeadDestroyIcon.TileIndex = index;
                            m_StateD.LeadDestroyIcon.TileVector = vec;
                            SnapUtility.Snap(m_StateD.LeadDestroySnap, m_StateD.LeadDestroyIcon);
                        }
                    }
                }

                m_StateD.PrevMousePosition = m_StateA.ScreenMousePos;
            }
            else if (m_StateD.LeadDestroyIcon != null)
            {
                ClearAllDestroyIcons();
            }
        }

        private void ClearAllDestroyIcons()
        {
            Game.SharedState.Get<BuildingPools>().DestroyIcons.Free(m_StateD.LeadDestroyIcon);
            m_StateD.LeadDestroyIcon = null;
            m_StateD.LeadDestroySnap = null;
        }
    }
}