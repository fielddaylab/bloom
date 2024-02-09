using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System;
using System.ComponentModel;
using System.Globalization;
using UnityEngine;
using Zavala.Sim;

namespace Zavala.World {

    [SysUpdate(GameLoopPhase.LateUpdate)]
    public sealed class SimPhosphorusRenderSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, SimPhosphorusState, SimWorldCamera> {
        #region Inspector

        public Mesh PhosphorusMesh;
        public Mesh PhosphorusMeshLow;
        public Material PhosphorusMaterial;

        public float PhosphorusLerpSpeed = 7;
        public float PhosphorusSnapRange = 0.08f;
        public float PhospohorusRenderSize = 0.1f;
        public float PhosphorusLODSwapZ = -22;

        #endregion // Inspector

        #region Work

        public override void ProcessWork(float deltaTime) {
            SimGridState gridState = m_StateB;
            SimPhosphorusState phosphorusState = m_StateC;

            HandleChanges(gridState, phosphorusState, m_StateA);

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
        }

        private void PerformMovement(SimWorldState component, float deltaTime) {
            float lerpAmount = TweenUtil.Lerp(PhosphorusLerpSpeed, 1, deltaTime);
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

        static private void HandleChanges(SimGridState gridState, SimPhosphorusState phosphorusState, SimWorldState worldState) {
            Assert.NotNull(gridState);
            Assert.NotNull(phosphorusState);
            Assert.NotNull(worldState);
            PhosphorusRendering.PrepareChangeBuffer(phosphorusState.Phosphorus.Changes);
            PhosphorusRendering.ProcessChanges(worldState.Phosphorus, (int) gridState.RegionCount, phosphorusState.Phosphorus.Changes, phosphorusState.Phosphorus.CurrentState(), gridState.Terrain.Info, gridState.Terrain.Height, worldState.WorldSpace, RandomPosDelegate, gridState.Random, Frame.Index8);
        }

        #endregion // Handling Changes

        #region Rendering

        private unsafe void PerformRendering(SimWorldState component, Camera camera) {
            DefaultInstancingParams* paramBuffer = stackalloc DefaultInstancingParams[512];
            RenderParams renderParams = new RenderParams(PhosphorusMaterial);
            Transform cameraTransform = camera.transform;
            Mesh mesh = cameraTransform.localPosition.z < PhosphorusLODSwapZ ? PhosphorusMeshLow : PhosphorusMesh;
            var instanceHelper = new InstancingHelper<DefaultInstancingParams>(paramBuffer, 512, renderParams, mesh);
            Matrix4x4 baseMatrix = Matrix4x4.TRS(default, Quaternion.LookRotation(-cameraTransform.forward, Vector3.up), PhospohorusRenderSize * Vector3.one);

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
            foreach (var inst in renderState.StationaryInstances) {
                mat.m03 = inst.Position.x;
                mat.m13 = inst.Position.y;
                mat.m23 = inst.Position.z;
                instParams.objectToWorld = mat;
                instancing.Queue(ref instParams);
            }

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