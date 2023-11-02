using FieldDay;
using FieldDay.Components;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zavala {
    [SysUpdate(GameLoopPhase.ApplicationPreRender)]
    public class DecorationRenderSystem : ComponentSystemBehaviour<DecorationRenderer> {
        public override void ProcessWork(float deltaTime) {
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

            foreach(var component in m_Components) {
                renderParms.layer = component.Layer;
                renderParms.material = component.Material;

                foreach(var decor in component.Decorations) {
                    Graphics.RenderMesh(renderParms, decor.Mesh, 0, decor.Matrix);
                }
            }
        }
    }
}