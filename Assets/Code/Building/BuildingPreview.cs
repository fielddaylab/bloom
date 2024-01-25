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
        // [SerializeField] private MeshRenderer m_Renderer;
        [SerializeField] private MeshRenderer[] m_Renderers;
        [SerializeField] private SnapToTile m_Snapping;
        [SerializeField] private ParticleSystem m_Particles;
        [SerializeField] private OccupiesTile m_Occupies;

        [Header("Config")]
        [SerializeField] private GameObject[] m_InitialHide;
        [SerializeField] private Mesh m_PreviewMesh;

        //[NonSerialized] private MeshFilter m_MeshFilter;
        [NonSerialized] private MeshFilter[] m_MeshFilters;
        //[NonSerialized] private Mesh m_OriginalMesh;
        [NonSerialized] private Mesh[] m_OriginalMeshes;
        //[NonSerialized] private Material m_OriginalMat;
        [NonSerialized] private Material[] m_OriginalMats;

        #region Unity Callbacks


        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (var obj in m_InitialHide) {
                obj.SetActive(false);
            }
            int numRenderers = m_Renderers.Length;

            if (m_PreviewMesh) {
                // allocate current and original meshes
                m_MeshFilters = new MeshFilter[numRenderers];
                m_OriginalMeshes = new Mesh[numRenderers];

                for (int i = 0; i < numRenderers; i++) {
                    m_MeshFilters[i] = m_Renderers[i].GetComponent<MeshFilter>();
                    m_OriginalMeshes[i] = m_MeshFilters[i].sharedMesh;
                }

            }

            if (numRenderers > 0 && m_OriginalMats == null) {
                // allocate original materials
                m_OriginalMats = new Material[numRenderers];
                for (int i = 0; i < numRenderers; i++) {
                    m_OriginalMats[i] = m_Renderers[i].sharedMaterial;
                }
            }
        }

        protected override void OnDisable() {
            if (Game.IsShuttingDown) {
                return;
            }

            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags &= ~Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            if (m_OriginalMats != null) {
                // revert to original materials
                for (int i = 0; i < m_Renderers.Length; i++) {
                    m_Renderers[i].sharedMaterial = m_OriginalMats[i];
                }
            }

            if (m_MeshFilters != null) {
                // revert to original meshes
                for (int i = 0; i < m_MeshFilters.Length; i++) {
                    m_MeshFilters[i].sharedMesh = m_OriginalMeshes[i];
                }
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
            for (int i = 0; i < m_Renderers.Length; i++) {
                // set material of all renderers to new material
                m_Renderers[i].sharedMaterial = newMat;
                // if this is set up to show preview mesh as well, set all filters to that
                if (m_MeshFilters != null) {
                    m_MeshFilters[i].sharedMesh = m_PreviewMesh;
                }
            }
            if (m_Occupies) {
                ZavalaGame.SimGrid.Terrain.Info[m_Occupies.TileIndex].Flags |= Sim.TerrainFlags.IsPreview;
                SimWorldUtility.QueueVisualUpdate((ushort) m_Occupies.TileIndex, VisualUpdateType.Preview);
            }

            m_Particles.Play();
        }

        private void ResetMaterial()
        {
            for (int i = 0; i < m_Renderers.Length; i++) {
                // set material of all renderers to new material
                m_Renderers[i].sharedMaterial = m_OriginalMats[i];
                // if this is set up to show preview mesh as well, set all filters to that
                if (m_MeshFilters != null) {
                    m_MeshFilters[i].sharedMesh = m_OriginalMeshes[i];
                }
            }
        }
    }
}