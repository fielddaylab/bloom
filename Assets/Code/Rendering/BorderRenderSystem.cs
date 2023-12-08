using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Rendering;
using Zavala.Sim;

namespace Zavala.World
{
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class BorderRenderSystem : SharedStateSystemBehaviour<BorderRenderState, SimGridState, SimWorldState>
    {
        #region Work

        public override bool HasWork() {
            return base.HasWork() && m_StateA.RegionQueue.Count > 0;
        }

        public override void ProcessWork(float deltaTime) {
            ushort regionIdx = m_StateA.RegionQueue.PopFront();

            var edges = m_StateB.Regions[regionIdx].Edges;

            m_StateA.MeshGeneratorA.Clear();
            m_StateA.MeshGeneratorB.Clear();

            for (int i = 0; i < edges.Length; i++) {
                RegionEdgeInfo edge = edges[i];
                Vector3 origin = SimWorldUtility.GetTileCenter(m_StateB, m_StateC, edge.Index);

                TileRendering.GenerateTileBorderMeshData(origin, edge.Directions, edge.SharedCornersCCW, edge.SharedCornersCW, 1.05f, 0, m_StateA.OutlineThickness, Color.white, Color.white.WithAlpha(0), m_StateA.MeshGeneratorA);
                TileRendering.GenerateTileBorderMeshData(origin, edge.Directions, edge.SharedCornersCCW, edge.SharedCornersCW, 1.05f, m_StateA.OutlineThickness, m_StateA.ThickOutlineThickness, Color.white, Color.white.WithAlpha(0), m_StateA.MeshGeneratorB);
            }

            m_StateA.MeshGeneratorA.Flush(m_StateC.OutlineMeshes[regionIdx]);
            m_StateA.MeshGeneratorB.Flush(m_StateC.ThickOutlineMeshes[regionIdx]);

            RefreshOutlineRenderer();
        }

        public override void Initialize() {
            base.Initialize();

            Game.Events.Register(GameEvents.RegionSwitched, RefreshOutlineRenderer)
                .Register(SimGridState.Event_RegionUpdated, RefreshOutlineRenderer);
        }

        private void RefreshOutlineRenderer() {
            if (!m_StateB) {
                return;
            }

            ushort currentRegionIdx = m_StateB.CurrRegionIndex;
            m_StateA.OutlineFilter.sharedMesh = m_StateC.OutlineMeshes[currentRegionIdx];
            m_StateA.LockedOutlineFilter.sharedMesh = m_StateC.ThickOutlineMeshes[currentRegionIdx];
        }

        public override void Shutdown() {
            Game.Events?.DeregisterAllForContext(this);

            base.Shutdown();
        }

        #endregion // Work

    }
}