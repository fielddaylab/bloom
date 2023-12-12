using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.World;

namespace Zavala.Building
{
    public class BuildingPreview : BatchedComponent
    {
        [SerializeField] private MeshRenderer m_Renderer;
        [SerializeField] private GameObject[] m_InitialHide;
        [SerializeField] private OccupiesTile m_Occupies;
        [SerializeField] private SnapToTile m_Snapping;

        [NonSerialized] private Material m_OriginalMat;

        #region Unity Callbacks

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (var obj in m_InitialHide) {
                obj.SetActive(false);
            }

            if (m_Renderer && !m_OriginalMat) { m_OriginalMat = m_Renderer.sharedMaterial; }
        }

        protected override void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags &= ~Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            if (m_Renderer && m_OriginalMat) {
                m_Renderer.sharedMaterial = m_OriginalMat;
            }

            base.OnDisable();
        }

        #endregion // Unity Callbacks

        public void Apply()
        {
            ResetMaterial();

            if (m_Occupies) {
                m_Occupies.Pending = false;
            }

            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags &= ~Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            foreach(var obj in m_InitialHide) {
                obj.SetActive(true);
            }
        }

        public void Cancel() {
            ResetMaterial();

            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags &= ~Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            foreach (var obj in m_InitialHide) {
                obj.SetActive(true);
            }
        }

        public void Preview(Material newMat)
        {
            m_Renderer.sharedMaterial = newMat;
            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags |= Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }
        }

        private void ResetMaterial()
        {
            m_Renderer.sharedMaterial = m_OriginalMat;
        }
    }
}