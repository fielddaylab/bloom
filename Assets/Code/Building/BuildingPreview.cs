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
        [Header("Components")]
        [SerializeField] private MeshRenderer m_Renderer;
        [SerializeField] private SnapToTile m_Snapping;
        [SerializeField] private ParticleSystem m_Particles;
        [SerializeField] private OccupiesTile m_Occupies;

        [Header("Config")]
        [SerializeField] private GameObject[] m_InitialHide;
        [SerializeField] private Mesh m_PreviewMesh;

        [NonSerialized] private MeshFilter m_MeshFilter;
        [NonSerialized] private Mesh m_OriginalMesh;
        [NonSerialized] private Material m_OriginalMat;

        #region Unity Callbacks

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (var obj in m_InitialHide) {
                obj.SetActive(false);
            }

            if (m_PreviewMesh && m_Renderer) {
                m_MeshFilter = m_Renderer.GetComponent<MeshFilter>();
                m_OriginalMesh = m_MeshFilter.sharedMesh;
            }

            if (m_Renderer && !m_OriginalMat) { m_OriginalMat = m_Renderer.sharedMaterial; }
        }

        protected override void OnDisable() {
            base.OnDisable();

            if (Game.IsShuttingDown || !Frame.IsLoadingOrLoaded(this)) {
                return;
            }

            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags &= ~Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            if (m_Renderer && m_OriginalMat) {
                m_Renderer.sharedMaterial = m_OriginalMat;
            }

            if (m_MeshFilter) {
                m_MeshFilter.sharedMesh = m_OriginalMesh;
            }
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

            m_Particles.Stop();

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

            m_Particles.Stop();

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

            if (m_MeshFilter) {
                m_MeshFilter.sharedMesh = m_PreviewMesh;
            }

            m_Particles.Play();
        }

        private void ResetMaterial()
        {
            m_Renderer.sharedMaterial = m_OriginalMat;

            if (m_MeshFilter) {
                m_MeshFilter.sharedMesh = m_OriginalMesh;
            }
        }
    }
}