using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.Rendering {
    public class EffectPlaybackState : SharedStateComponent {
        public ParticleSystem PoofEffect;
        public ParticleSystem PoofEffectRoads;

        public int DefaultPoofCount;
        public int DefaultPoofRoadCount;

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

    static public class VfxUtility {
        static public void PlayEffect(Vector3 position, EffectType type, int count = 0) {
            Game.SharedState.Get<EffectPlaybackState>().Requests.PushBack(new EffectRequest() {
                Position = position,
                Type = type,
                Count = count
            });
        }
    }
}