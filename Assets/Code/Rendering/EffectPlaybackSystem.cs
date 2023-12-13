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

                system.Emit(emit, request.Count);
                system.Play();
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
                default: {
                    Log.Warn("[EffectPlaybackSystem] No effects loaded for type '{0}'", type);
                    return null;
                }
            }
        }
    }
}