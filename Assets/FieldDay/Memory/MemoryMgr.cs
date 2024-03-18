using System;
using BeauPools;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Memory {

    /// <summary>
    /// Manages memory pools.
    /// </summary>
    public class MemoryMgr {
        private IPool<Mesh> m_MeshPool;
        private IPool<Material> m_MaterialPool;
        private Shader m_DefaultShader;

        #region Mesh

        #endregion // Mesh

        #region Events

        internal MemoryMgr() {
            Mem.Mgr = this;
        }

        internal void Initialize(MemoryPoolConfiguration configuration) {
            m_MeshPool = new DynamicPool<Mesh>(configuration.MeshCapacity, (p) => new Mesh(), false);
            m_MeshPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

            m_MaterialPool = new DynamicPool<Material>(configuration.MaterialCapacity, (p) => new Material(m_DefaultShader), false);
            m_MaterialPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

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