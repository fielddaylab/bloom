using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Rendering;
using Zavala.Sim;

namespace Zavala.World {

    [SysUpdate(GameLoopPhase.LateUpdate)]
    public sealed class SimPhosphorusRenderSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, SimPhosphorusState, SimWorldCamera> {
        #region Inspector

        public Mesh PhosphorusMesh;
        public Mesh PhosphorusMeshLow;
        public Material PhosphorusMaterial;

        public float PhosphorusSnapRange = 0.08f;
        public float PhosphorusLODSwapZ = -22;

        #endregion // Inspector

        #region Work

        public override void ProcessWork(float deltaTime) {
            SimGridState gridState = m_StateB;
            SimPhosphorusState phosphorusState = m_StateC;
            PhosphorusHeatMap heatMap = Find.State<PhosphorusHeatMap>();

            if (m_StateA.NewRegions > 0) {
                AddNewRegionsToHeatMap(gridState, m_StateA, phosphorusState, heatMap, m_StateA.NewRegions);
            }

            HandlePipChanges(gridState, phosphorusState, m_StateA);
            HandleHeatMapChanges(phosphorusState, heatMap);

            bool shouldRender = (m_StateA.Overlays & SimWorldOverlayMask.Phosphorus) != 0;

            float scaledTime = SimTimeUtility.AdjustedDeltaTime(deltaTime, ZavalaGame.SimTime);
            if (scaledTime > 0) {
                if (!shouldRender) {
                    scaledTime *= 4;
                }
                PerformMovement(m_StateA, scaledTime);
            }

            if (shouldRender) {
                PerformRendering(m_StateA, m_StateD.Camera);
            }

            heatMap.TargetRenderer.enabled = shouldRender;
        }

        private void PerformMovement(SimWorldState component, float deltaTime) {
            float lerpAmount = TweenUtil.Lerp(PhosphorusRendering.ParticleLerp, 1, deltaTime);
            float minDistSq = PhosphorusSnapRange * PhosphorusSnapRange;
            for (int i = 0; i < component.RegionCount; i++) {
                PhosphorusRendering.ProcessMovement(component.Phosphorus[i], deltaTime, lerpAmount, minDistSq);
            }
        }

        #endregion // Work

        #region Handling Changes

        static private readonly PhosphorusRendering.RandomTilePositionDelegate RandomPosDelegate = RandomPosOnTile;

        static private Vector3 RandomPosOnTile(int tileIdx, ushort height, in HexGridWorldSpace worldSpace) {
            Vector3 pos = HexVector.ToWorld(tileIdx, height, worldSpace);
            pos.x += RNG.Instance.NextFloat(-0.4f, 0.4f) * worldSpace.Scale.x;
            pos.z += RNG.Instance.NextFloat(-0.4f, 0.4f) * worldSpace.Scale.z;
            pos.y += 0.2f;
            return pos;
        }

        static private void HandlePipChanges(SimGridState gridState, SimPhosphorusState phosphorusState, SimWorldState worldState) {
            Assert.NotNull(gridState);
            Assert.NotNull(phosphorusState);
            Assert.NotNull(worldState);
            PhosphorusRendering.PrepareChangeBuffer(phosphorusState.Phosphorus.Changes);
            PhosphorusRendering.ProcessChanges(worldState.Phosphorus, (int) gridState.RegionCount, phosphorusState.Phosphorus.Changes, phosphorusState.Phosphorus.CurrentState(), gridState.Terrain.Info, gridState.Terrain.Height, worldState.WorldSpace, RandomPosDelegate, gridState.Random, Frame.Index8);
        }

        static private void HandleHeatMapChanges(SimPhosphorusState phosphorusState, PhosphorusHeatMap heatMap) {
            if (phosphorusState.Phosphorus.Changes.AffectedTiles.Count > 0) {
                var phosBuff = phosphorusState.Phosphorus.CurrentState();
                foreach (var tile in phosphorusState.Phosphorus.Changes.AffectedTiles) {
                    HeatMapUtility.UpdateTile(heatMap, tile, phosBuff[tile].Count);
                }
                HeatMapUtility.PushChanges(heatMap, MeshDataUploadFlags.DontRecalculateBounds | MeshDataUploadFlags.SkipIndexBufferUpload);
            }
        }

        static private void AddNewRegionsToHeatMap(SimGridState gridState, SimWorldState worldState, SimPhosphorusState phosphorusState, PhosphorusHeatMap heatMap, int newRegionCount) {
            int start = (int) gridState.RegionCount - newRegionCount;
            var phosBuff = phosphorusState.Phosphorus.CurrentState();
            for(int regionIdx = start; regionIdx < gridState.RegionCount; regionIdx++) {
                HexGridSubregion region = gridState.Regions[regionIdx].GridArea;
                foreach(var idx in region) {
                    if (gridState.Terrain.Regions[idx] == regionIdx && gridState.Terrain.NonVoidTiles.IsSet(idx)) {
                        HeatMapUtility.AddTile(heatMap, worldState.WorldSpace, idx, (ushort) regionIdx, gridState.Terrain.Height[idx], phosBuff[idx].Count);
                    }
                }
                HeatMapUtility.AddEdgeFalloff(heatMap, worldState.WorldSpace, gridState.HexSize, (ushort) regionIdx, gridState.Regions[regionIdx].Edges, gridState.Terrain.Height, gridState.Terrain.NonVoidTiles);
                HeatMapUtility.ConnectTilesInner(heatMap, gridState.HexSize, (ushort) regionIdx, region.Expand(1, 1, 1, 1), heatMap.TileRegions, gridState.Terrain.NonVoidTiles);
            }

            for(int regionIdx = 0; regionIdx < start; regionIdx++) {
                HeatMapUtility.PatchEdges(heatMap, gridState.HexSize, (ushort) start, heatMap.TileRegions, gridState.Regions[regionIdx].Edges, gridState.Terrain.NonVoidTiles);
            }

            for(int regionIdx = start; regionIdx < gridState.RegionCount - 1; regionIdx++) {
                HeatMapUtility.PatchEdges(heatMap, gridState.HexSize, (ushort) (regionIdx + 1), heatMap.TileRegions, gridState.Regions[regionIdx].Edges, gridState.Terrain.NonVoidTiles);
            }

            HeatMapUtility.PushChanges(heatMap, 0);
        }

        #endregion // Handling Changes

        #region Rendering

        private unsafe void PerformRendering(SimWorldState component, Camera camera) {
            DefaultInstancingParams* paramBuffer = stackalloc DefaultInstancingParams[512];
            RenderParams renderParams = new RenderParams(PhosphorusMaterial);
            Transform cameraTransform = camera.transform;
            Mesh mesh = cameraTransform.localPosition.z < PhosphorusLODSwapZ ? PhosphorusMeshLow : PhosphorusMesh;
            var instanceHelper = new InstancingHelper<DefaultInstancingParams>(paramBuffer, 512, renderParams, mesh);
            Matrix4x4 baseMatrix = Matrix4x4.TRS(default, Quaternion.LookRotation(-cameraTransform.forward, Vector3.up), PhosphorusRendering.ParticleSize * Vector3.one);

            for (int i = 0; i < component.RegionCount; i++) {
                bool isVisible = CullingHelper.IsRegionVisible(component.RegionCullingMask, i);
                if (isVisible) {
                    RenderPhosphorusForRegion(component.Phosphorus[i], baseMatrix, ref instanceHelper);
                }
            }

            instanceHelper.Submit();
            instanceHelper.Dispose();
        }

        private void RenderPhosphorusForRegion(PhosphorusRenderState renderState, Matrix4x4 mat, ref InstancingHelper<DefaultInstancingParams> instancing) {
            DefaultInstancingParams instParams = default;
            foreach (var inst in renderState.AnimatingInstances) {
                mat.m03 = inst.Position.x;
                mat.m13 = inst.Position.y;
                mat.m23 = inst.Position.z;
                instParams.objectToWorld = mat;
                instancing.Queue(ref instParams);
            }
        }

        #endregion // Rendering
    }
}