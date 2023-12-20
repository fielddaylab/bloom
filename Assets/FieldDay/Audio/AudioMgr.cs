using System;
using BeauUtil;

namespace FieldDay.Audio {
    public sealed class AudioMgr {
        #region Types

        [Serializable]
        public struct Config {
        }

        #endregion // Types

        public const int MaxSampleVoices = 128;
        public const int MaxStreamVoices = 16;
        public const int MaxBuses = 16;

        #region State

        //private readonly UniqueIdAllocator16 m_SamplePlayerIdAllocator = new UniqueIdAllocator16(MaxSampleVoices);
        //private readonly UniqueIdAllocator16 m_StreamPlayerIdAllocator = new UniqueIdAllocator16(MaxStreamVoices);

        //private readonly SampleVoiceData[] m_SampleVoiceData = new SampleVoiceData[MaxSampleVoices];
        //private readonly StreamVoiceData[] m_StreamVoiceData = new StreamVoiceData[MaxStreamVoices];

        //private readonly RingBuffer<UniqueId16> m_AllocatedSamples = new RingBuffer<UniqueId16>(MaxSampleVoices, RingBufferMode.Expand);
        //private readonly RingBuffer<UniqueId16> m_AllocatedStreams = new RingBuffer<UniqueId16>(MaxStreamVoices, RingBufferMode.Expand);

        private Unsafe.ArenaHandle m_Arena;

        #endregion // State

        internal AudioMgr(Config config) {
            //m_Arena = Unsafe.CreateArena(2 * Unsafe.MiB, "Audio", Unsafe.AllocatorFlags.ZeroOnAllocate);
        }

        #region Events

        internal void PreUpdate(float deltaTime) {

        }

        internal void Update(float deltaTime) {

        }

        internal void Shutdown() {
            //Unsafe.TryDestroyArena(ref m_Arena);
        }

        #endregion // Events

        #region Audio Updates

        private void UpdateEmitterLocations(float deltaTime) {

        }

        private void UpdateMix(float deltaTime) {

        }

        private void UpdatePlayback(float deltaTime) {

        }

        #endregion // Audio Updates
    }
}