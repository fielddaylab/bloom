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
    public sealed class SimPhosphorusRenderSystem : SharedStateSystemBehaviour<SimWorldState, SimGridState, SimPhosphorusState> {
        #region Inspector

        public Mesh PhosphorusMesh;
        public Material PhosphorusMaterial;

        public float PhosphorusLerpSpeed = 7;
        public float PhosphorusSnapRange = 0.08f;
        public float PhospohorusRenderSize = 0.1f;

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
                    scaledTime *= 2;
                }
                PerformMovement(m_StateA, scaledTime);
            }

            if (shouldRender) {
                PerformRendering(m_StateA);
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
            PhosphorusRendering.ProcessChanges(worldState.Phosphorus, (int) gridState.RegionCount, phosphorusState.Phosphorus.Changes, phosphorusState.Phosphorus.CurrentState(), gridState.Terrain.Info, gridState.Terrain.Height, worldState.WorldSpace, RandomPosOnTile, gridState.Random, Frame.Index8);
        }

        #endregion // Handling Changes

        #region Rendering

        private unsafe void PerformRendering(SimWorldState component) {
            DefaultInstancingParams* paramBuffer = stackalloc DefaultInstancingParams[512];
            RenderParams renderParams = new RenderParams(PhosphorusMaterial);
            var instanceHelper = new InstancingHelper<DefaultInstancingParams>(paramBuffer, 512, renderParams, PhosphorusMesh);

            for (int i = 0; i < component.RegionCount; i++) {
                bool isVisible = CullingHelper.IsRegionVisible(component.RegionCullingMask, i);
                if (isVisible) {
                    RenderPhosphorusForRegion(component.Phosphorus[i], ref instanceHelper);
                }
            }

            instanceHelper.Submit();
            instanceHelper.Dispose();
        }

        private void RenderPhosphorusForRegion(PhosphorusRenderState renderState, ref InstancingHelper<DefaultInstancingParams> instancing) {
            Vector3 size = PhospohorusRenderSize * Vector3.one;

            DefaultInstancingParams instParams = default;
            foreach (var inst in renderState.StationaryInstances) {
                instParams.objectToWorld = Matrix4x4.TRS(inst.Position, Quaternion.identity, size);
                instancing.Queue(instParams);
            }

            foreach (var inst in renderState.AnimatingInstances) {
                instParams.objectToWorld = Matrix4x4.TRS(inst.Position, Quaternion.identity, size);
                instancing.Queue(instParams);
            }
        }

        #endregion // Rendering
    }
}