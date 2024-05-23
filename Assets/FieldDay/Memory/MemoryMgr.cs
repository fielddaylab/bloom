using System;
using System.Diagnostics;
using System.Runtime;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Memory {

    /// <summary>
    /// Manages memory pools.
    /// </summary>
    public class MemoryMgr {
        private int m_LastKnownGenerationCount;
        private int[] m_GCCollectCounts;
        private long[] m_GCCollectTimestamps;
        private long m_MostRecentGCTimestamp;

        private long m_MainThreadAllocationTracker;

        private IPool<Mesh> m_MeshPool;
        private IPool<Material> m_MaterialPool;
        private Shader m_DefaultShader;

        #region Mesh

        #endregion // Mesh

        #region GC

        internal void UpdateGCMarkers(ushort frameIndex) {
            long now = Stopwatch.GetTimestamp();

            int maxGen = GC.MaxGeneration + 1;
            int genMask = 0;

            if (m_LastKnownGenerationCount < maxGen) {
                m_LastKnownGenerationCount = maxGen;
                Array.Resize(ref m_GCCollectCounts, maxGen);
                Array.Resize(ref m_GCCollectTimestamps, maxGen);
                Log.Msg("[MemoryMgr] Generation count changed to {0}", maxGen);
            }

            for (int i = 0; i < maxGen; i++) {
                int genCount = GC.CollectionCount(i);
                if (Ref.Replace(ref m_GCCollectCounts[i], genCount)) {
                    m_GCCollectTimestamps[i] = now;
                    genMask |= (1 << i);
                }
            }

            if (genMask != 0) {
                //using (Log.DisableMsgStackTrace()) {
                //    Log.Msg("[MemoryMgr] Garbage collected {0}", genMask);
                //}
                m_MostRecentGCTimestamp = now;
                Mem.InvokeGCOccurred(genMask);
            }

            long allocated = GC.GetTotalMemory(false);
            if (m_MainThreadAllocationTracker != allocated) {
                long diff = allocated - m_MainThreadAllocationTracker;
                //using (Log.DisableMsgStackTrace()) {
                //    Log.Msg("[MemoryMgr] GC allocated {0}b", diff);
                //}
                m_MainThreadAllocationTracker = allocated;
            }
        }

        #endregion // GC

        #region Events

        internal MemoryMgr() {
            Mem.Mgr = this;
        }

        internal void Initialize(MemoryPoolConfiguration configuration) {
            int genCount = GC.MaxGeneration + 1;
            m_GCCollectCounts = new int[genCount];
            m_GCCollectTimestamps = new long[genCount];
            m_LastKnownGenerationCount = genCount;

            m_MeshPool = new DynamicPool<Mesh>(configuration.MeshCapacity, (p) => new Mesh(), false);
            m_MeshPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

            m_MaterialPool = new DynamicPool<Material>(configuration.MaterialCapacity, (p) => new Material(m_DefaultShader), false);
            m_MaterialPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

            Log.Msg("[MemoryMgr] GC Latency is {0}, {1} generations", GCSettings.LatencyMode, genCount);
            //GCSettings.LatencyMode = GCLatencyMode.LowLatency;

            m_DefaultShader = Shader.Find("Hidden/InternalColored");
        }

        internal void Shutdown() {
            m_MeshPool.Dispose();
            m_MaterialPool.Dispose();
            m_DefaultShader = null;

            Mem.Mgr = null;
        }

        #endregion // Events
    }

    [Serializable]
    public struct MemoryPoolConfiguration {
        public int MeshCapacity;
        public int MaterialCapacity;
    }
}