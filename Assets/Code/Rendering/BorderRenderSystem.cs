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
    [SysUpdate(GameLoopPhaseMask.UnscaledLateUpdate | GameLoopPhaseMask.DebugUpdate)]
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

            for (int i = 0; i < edges.Length; i++) {
                RegionEdgeInfo edge = edges[i];
                Vector3 origin = SimWorldUtility.GetTileCenter(m_StateB, m_StateC, edge.Index);

                TileRendering.GenerateTileBorderMeshData(origin, edge.Directions, edge.SharedCornerCCW, edge.SharedCornerCW, m_StateA.RadiusMuliplier, 0, m_StateA.OutlineThickness, Color.white, Color.white.WithAlpha(0), m_StateA.MeshGeneratorA);
            }

            m_StateA.MeshGeneratorA.Flush(m_StateC.OutlineMeshes[regionIdx]);

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
            m_StateA.OutlineMaterial.color = m_StateB.Regions[currentRegionIdx].BorderColor;
        }

        public override void Shutdown() {
            Game.Events?.DeregisterAllForContext(this);

            base.Shutdown();
        }

        #endregion // Work

    }
}