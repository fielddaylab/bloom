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
            var edgeDirections = m_StateB.Regions[regionIdx].EdgeDirections;

            m_StateA.MeshGeneratorA.Clear();
            m_StateA.MeshGeneratorB.Clear();

            for (int i = 0; i < edges.Length; i++) {
                Vector3 origin = SimWorldUtility.GetTileCenter(m_StateB, m_StateC, edges[i]);

                TileRendering.GenerateTileBorderMeshData(origin, edgeDirections[i], 1.05f, 0, 0.2f, Color.yellow, Color.yellow, m_StateA.MeshGeneratorA);
                TileRendering.GenerateTileBorderMeshData(origin, edgeDirections[i], 1.05f, 0.2f, 2, Color.gray, Color.gray.WithAlpha(0), m_StateA.MeshGeneratorB);
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
        }

        public override void Shutdown() {
            Game.Events?.DeregisterAllForContext(this);

            base.Shutdown();
        }

        #endregion // Work

    }
}