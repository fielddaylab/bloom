using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;
using Zavala.World;

namespace Zavala {
    [SysUpdate(GameLoopPhase.ApplicationPreRender)]
    public class DecorationRenderSystem : ComponentSystemBehaviour<DecorationRenderer> {
        static private readonly InstanceBucket[] s_WorkBuckets = new InstanceBucket[64];
        
        [SerializeField] private bool m_Instancing = true;

        public unsafe override void ProcessWork(float deltaTime) {
            if (m_Instancing) {
                RenderInstanced();
            } else {
                RenderNonInstanced();
            }
        }

        private void RenderNonInstanced() {
            SimWorldState world = Find.State<SimWorldState>();

            RenderParams renderParms = default;
            renderParms.renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask;
            renderParms.rendererPriority = 0;
            renderParms.worldBounds = new Bounds(Vector3.zero, Vector3.zero);
            renderParms.motionVectorMode = MotionVectorGenerationMode.Camera;
            renderParms.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderParms.lightProbeUsage = LightProbeUsage.Off;
            renderParms.lightProbeProxyVolume = null;
            renderParms.receiveShadows = false;
            renderParms.shadowCastingMode = ShadowCastingMode.Off;

            foreach (var component in m_Components) {
                if (component.RegionIndex != Tile.InvalidIndex16 && !CullingHelper.IsRegionVisible(world.RegionCullingMask, component.RegionIndex)) {
                    continue;
                }

                renderParms.layer = component.Layer;
                renderParms.material = component.Material;

                foreach (var decor in component.Decorations) {
                    Graphics.RenderMesh(renderParms, decor.Mesh, 0, decor.Matrix);
                }
            }
        }

        private unsafe void RenderInstanced() {
            SimWorldState world = Find.State<SimWorldState>();

            InstanceBucket[] buckets = s_WorkBuckets;
            int usedBuckets = 0;
            int* bucketKeys = stackalloc int[buckets.Length];
            int prevKey = 0;
            int prevKeyIdx = -1;

            RenderParams renderParms = default;
            renderParms.renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask;
            renderParms.rendererPriority = 0;
            renderParms.worldBounds = new Bounds(Vector3.zero, Vector3.zero);
            renderParms.motionVectorMode = MotionVectorGenerationMode.Camera;
            renderParms.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderParms.lightProbeUsage = LightProbeUsage.Off;
            renderParms.lightProbeProxyVolume = null;
            renderParms.receiveShadows = false;
            renderParms.shadowCastingMode = ShadowCastingMode.Off;

            DefaultInstancingParams instance = default;

            foreach (var component in m_Components) {
                if (component.RegionIndex != Tile.InvalidIndex16 && !CullingHelper.IsRegionVisible(world.RegionCullingMask, component.RegionIndex)) {
                    continue;
                }

                renderParms.layer = component.Layer;
                renderParms.material = component.Material;

                int materialKey = component.Material.GetInstanceID();

                foreach (var decor in component.Decorations) {
                    int meshKey = decor.Mesh.GetInstanceID();

                    int key = materialKey << 5 ^ meshKey;

                    int bucketIdx;
                    if (key == prevKey) {
                        bucketIdx = prevKeyIdx;
                    } else {
                        prevKey = key;
                        bucketIdx = -1;
                        for (int i = 0; i < usedBuckets; i++) {
                            if (bucketKeys[i] == key) {
                                bucketIdx = i;
                                break;
                            }
                        }
                        if (bucketIdx < 0) {
                            Assert.True(usedBuckets < buckets.Length);
                            bucketIdx = prevKeyIdx = usedBuckets;
                            ref InstanceBucket bucket = ref buckets[usedBuckets++];
                            bucket.Reset(Frame.AllocArray<DefaultInstancingParams>(32), 32, renderParms, decor.Mesh);
                        }
                    }

                    instance.objectToWorld = decor.Matrix;
                    buckets[bucketIdx].Queue(ref instance);
                }
            }

            for (int i = 0; i < usedBuckets; i++) {
                buckets[i].SubmitAndDispose();
            }
        }

        private unsafe struct InstanceBucket : IDisposable {
            private DefaultInstancingParams* m_DataHead;
            private int m_MaxElements;
            private DefaultInstancingParams* m_WriteHead;
            private int m_QueuedElements;

            private RenderParams m_RenderParams;
            private Mesh m_Mesh;
            private int m_SubmeshIndex;

            public void Reset(DefaultInstancingParams* buffer, int bufferSize, RenderParams renderParams, Mesh mesh, int submeshIndex = 0) {
                m_DataHead = buffer;
                m_MaxElements = Math.Min(bufferSize, 1023);
                m_WriteHead = buffer;
                m_QueuedElements = 0;
                m_RenderParams = renderParams;
                m_Mesh = mesh;
                m_SubmeshIndex = submeshIndex;
            }

            public bool IsFull() {
                return m_QueuedElements == m_MaxElements;
            }

            public void Queue(ref DefaultInstancingParams data) {
                if (IsFull()) {
                    Submit();
                }

                Assert.True(m_QueuedElements < m_MaxElements);
                *m_WriteHead++ = data;
                m_QueuedElements++;
            }

            public void Submit() {
                if (m_QueuedElements > 0) {
                    Graphics.RenderMeshInstanced<DefaultInstancingParams>(m_RenderParams, m_Mesh, m_SubmeshIndex, Unsafe.NativeArray(m_DataHead, m_QueuedElements), m_QueuedElements);
                    m_QueuedElements = 0;
                    m_WriteHead = m_DataHead;
                }
            }

            public void SubmitAndDispose() {
                Submit();
                Dispose();
            }

            public void Dispose() {
                m_DataHead = null;
                m_WriteHead = null;
                m_QueuedElements = 0;
                m_RenderParams = default;
                m_Mesh = null;
            }
        }
    }
}