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
    [SysUpdate(GameLoopPhase.Update, 410)]
    public class TilePreviewSystem : SharedStateSystemBehaviour<InputState, SimWorldCamera, BlueprintState, TilePreviewState>
    {
        public override void ProcessWork(float deltaTime)
        {
            BuildToolState btState = Game.SharedState.Get<BuildToolState>();
            BuildingPools pools = Game.SharedState.Get<BuildingPools>();
            SimGridState grid = Game.SharedState.Get<SimGridState>();

            if (btState.ToolUpdated) {
                switch (btState.ActiveTool) {
                    case UserBuildTool.None:
                    case UserBuildTool.Road: {
                        m_StateD.Previewing = false;
                        HideIcon();
                        break;
                    }

                    case UserBuildTool.Destroy: {
                        m_StateD.Previewing = true;
                        ConfigureIconMesh(null);
                        SetPreviewColor(m_StateD.DeleteHexColor);
                        break;
                    }

                    case UserBuildTool.Storage: {
                        m_StateD.Previewing = true;
                        ConfigureIconMesh(pools.StorageMesh);
                        break;
                    }

                    case UserBuildTool.Digester: {
                        m_StateD.Previewing = true;
                        ConfigureIconMesh(pools.DigesterMesh);
                        break;
                    }
                }
            }

            if (m_StateD.Previewing) {
                int idx = SimWorldUtility.RaycastTile(m_StateA.ViewportMouseRay);
                if (idx < 0 || grid.Terrain.Regions[idx] != grid.CurrRegionIndex) {
                    HideIcon();
                } else {
                    if (idx != m_StateD.TileIndex) {
                        ShowIcon();
                        m_StateD.Icon.transform.position = SimWorldUtility.GetTileCenter(idx);
                        m_StateD.TileIndex = idx;
                        ZavalaGame.Events.Dispatch(GameEvents.HoverTile, idx);
                        if (btState.ActiveTool != UserBuildTool.Destroy) {
                            if (btState.BlockedIdxs.Contains(idx)) {
                                m_StateD.Icon.MeshRenderer.sharedMaterial = m_StateD.BuildingMaterialInvalid;
                                SetPreviewColor(m_StateD.InvalidHexColor);
                            } else {
                                m_StateD.Icon.MeshRenderer.sharedMaterial = m_StateD.BuildingMaterialValid;
                                SetPreviewColor(m_StateD.ValidHexColor);
                            }
                        }
                    }
                }
            }
        }

        private void ShowIcon() {
            m_StateD.Icon.gameObject.SetActive(true);
        }

        private void HideIcon() {
            m_StateD.TileIndex = -1;
            m_StateD.Icon.gameObject.SetActive(false);
        }

        private void ConfigureIconMesh(Mesh mesh) {
            m_StateD.Icon.MeshFilter.sharedMesh = mesh;
            m_StateD.Icon.MeshRenderer.enabled = mesh != null;
        }

        private void SetPreviewColor(Color color) {
            var main = m_StateD.Icon.Particles.main;
            var mainColor = main.startColor;
            mainColor.color = color;
            main.startColor = mainColor;
        }
    }
}