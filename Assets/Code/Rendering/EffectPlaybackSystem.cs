using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Zavala.Rendering {
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class EffectPlaybackSystem : SharedStateSystemBehaviour<EffectPlaybackState> {
        public override void ProcessWork(float deltaTime) {
            while(m_State.Requests.TryPopFront(out EffectRequest request)) {
                ParticleSystem system = GetParticleSystemForEffect(request.Type);

                ParticleSystem.EmitParams emit = default;
                emit.applyShapeToPosition = true;
                emit.position = request.Position;

                if (request.Count <= 0) {
                    request.Count = GetDefaultParticleCountForEffect(request.Type);
                }

                system.Emit(emit, request.Count);
                system.Play();
            }
        }

        private int GetDefaultParticleCountForEffect(EffectType type) {
            switch (type) {
                case EffectType.Poof: {
                    return m_State.DefaultPoofCount;
                }
                case EffectType.Poof_Road: {
                    return m_State.DefaultPoofRoadCount;
                }
                case EffectType.Poop_Runoff: {
                    return m_State.DefaultPoopRunoffCount;
                }
                case EffectType.Algae_Remove: {
                    return m_State.DefaultAlgaeCount;
                }
                case EffectType.Road_AnchorInteract: {
                    return m_State.DefaultRoadAnchorInteractCount;
                }
                default: {
                    Log.Warn("[EffectPlaybackSystem] No effects loaded for type '{0}'", type);
                    return 0;
                }
            }
        }

        private ParticleSystem GetParticleSystemForEffect(EffectType type) {
            switch (type) {
                case EffectType.Poof: {
                    return m_State.PoofEffect;
                }
                case EffectType.Poof_Road: {
                    return m_State.PoofEffectRoads;
                }
                case EffectType.Poop_Runoff: {
                    return m_State.PoopRunoff;
                }
                case EffectType.Algae_Remove: {
                    return m_State.AlgaeRemove;
                }
                case EffectType.Road_AnchorInteract: {
                    return m_State.RoadAnchorInteract;
                }
                default: {
                    Log.Warn("[EffectPlaybackSystem] No effects loaded for type '{0}'", type);
                    return null;
                }
            }
        }
    }
}