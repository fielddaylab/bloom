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

            GenerateRegionBorder(regionIdx);
            GenerateRegionShadow(regionIdx);
        }

        public override void Initialize() {
            base.Initialize();

            Game.Events.Register(GameEvents.RegionSwitched, RefreshRegionRenderers)
                .Register(SimGridState.Event_RegionUpdated, RefreshRegionRenderers)
                .Register(GameEvents.BlueprintModeStarted, OnBlueprintStarted)
                .Register(GameEvents.BlueprintModeEnded, OnBlueprintEnded);
        }

        private void OnBlueprintStarted() {
            m_StateA.ShadowFade.Replace(m_StateA.ShadowMaterial.FadeTo(0.67f, 0.15f));
        }
        private void OnBlueprintEnded() {
            m_StateA.ShadowFade.Replace(m_StateA.ShadowMaterial.FadeTo(0.4f, 0.15f));

        }
        private void RefreshRegionRenderers() {
            if (!m_StateB) {
                return;
            }

            ushort currentRegionIdx = m_StateB.CurrRegionIndex;
            m_StateA.OutlineFilter.sharedMesh = m_StateC.OutlineMeshes[currentRegionIdx];
            m_StateA.OutlineMaterial.color = m_StateB.Regions[currentRegionIdx].BorderColor;
            for (int i = 0; i < m_StateA.ShadowRenderers.Length; i++) {
                m_StateA.ShadowRenderers[i].enabled = (i != currentRegionIdx);
            }
        }

        private void GenerateRegionBorder(ushort regionIdx) {
            var edges = m_StateB.Regions[regionIdx].Edges;

            m_StateA.MeshGeneratorA.Clear();

            for (int i = 0; i < edges.Length; i++) {
                RegionEdgeInfo edge = edges[i];
                Vector3 origin = SimWorldUtility.GetTileCenter(m_StateB, m_StateC, edge.Index);

                TileRendering.GenerateTileBorderMeshData(origin, edge.Directions, edge.SharedCornerCCW, edge.SharedCornerCW, m_StateA.RadiusMuliplier, 0, m_StateA.OutlineThickness, Color.white, Color.white.WithAlpha(0), m_StateA.MeshGeneratorA);
            }

            m_StateA.MeshGeneratorA.Flush(m_StateC.OutlineMeshes[regionIdx]);

            RefreshRegionRenderers();
        }

        private unsafe void GenerateRegionShadow(ushort regionIdx) {
            ShadowRenderState shadow;
            shadow.MeshData = m_StateA.MeshGeneratorB;
            shadow.TileToVertexRangeMap = Frame.AllocSpan<OffsetLengthU16>((int)m_StateB.HexSize.Size);
            shadow.TileRegions = Frame.AllocSpan<ushort>((int)m_StateB.HexSize.Size);
            Unsafe.Clear(shadow.TileToVertexRangeMap);
            for (int i = 0; i < shadow.TileRegions.Length; i++) {
                shadow.TileRegions[i] = Tile.InvalidIndex16;
            }
            HexGridSubregion region = m_StateB.Regions[regionIdx].GridArea;
            foreach (var idx in region) {
                if (m_StateB.Terrain.Regions[idx] == regionIdx && m_StateB.Terrain.NonVoidTiles.IsSet(idx)) {
                    BorderRenderUtility.AddTile(shadow, m_StateC.WorldSpace, idx, (ushort)regionIdx, m_StateB.Terrain.Height[idx], 1);
                }
            }
            BorderRenderUtility.AddEdgeFalloffIgnoreNonVoids(shadow, m_StateC.WorldSpace, m_StateB.HexSize, (ushort)regionIdx, m_StateB.Regions[regionIdx].Edges, m_StateB.Terrain.Height);
            BorderRenderUtility.ConnectTilesInner(shadow, m_StateB.HexSize, (ushort)regionIdx, region.Expand(1, 1, 1, 1), shadow.TileRegions);
            MeshDataTarget target = MeshDataTarget.CreateFromMesh(m_StateC.ShadowMeshes[regionIdx]);
            BorderRenderUtility.PushChanges(shadow, ref target, 0);
            shadow.MeshData.Clear();
        }

        public override void Shutdown() {
            Game.Events?.DeregisterAllForContext(this);

            base.Shutdown();
        }

        #endregion // Work

    }
}