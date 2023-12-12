using BeauUtil;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Rendering {
    public class EffectPlaybackState : SharedStateComponent {
        public ParticleSystem PoofEffect;
        public ParticleSystem PoofEffectRoads;

        public RingBuffer<EffectRequest> Requests = new RingBuffer<EffectRequest>(8, RingBufferMode.Expand);
    }

    public struct EffectRequest {
        public Vector3 Position;
        public EffectType Type;
        public int Count;
    }

    public enum EffectType {
        Poof,
        Poof_Road
    }
}