using System;
using BeauUtil.Debugger;
using UnityEngine;

namespace Zavala {
    public unsafe struct InstancingHelper<T> : IDisposable where T : unmanaged {
        private T* m_DataHead;
        private readonly int m_MaxElements;
        private T* m_WriteHead;
        private int m_QueuedElements;

        private RenderParams m_RenderParams;
        private Mesh m_Mesh;
        private int m_SubmeshIndex;

        public InstancingHelper(T* buffer, int bufferSize, RenderParams renderParams, Mesh mesh, int submeshIndex = 0) {
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

        public void Queue(T data) {
            if (IsFull()) {
                Submit();
            }

            Assert.True(m_QueuedElements < m_MaxElements);
            *m_WriteHead++ = data;
            m_QueuedElements++;
        }

        public void Submit() {
            if (m_QueuedElements > 0) {
                Graphics.RenderMeshInstanced<T>(m_RenderParams, m_Mesh, m_SubmeshIndex, UnsafeExt.TempNativeArray(m_DataHead, m_QueuedElements), m_QueuedElements);
                m_QueuedElements = 0;
                m_WriteHead = m_DataHead;
            }
        }

        public void Dispose() {
            m_DataHead = null;
            m_WriteHead = null;
            m_QueuedElements = 0;
            m_RenderParams = default;
            m_Mesh = null;
        }
    }

    public struct DefaultInstancingParams {
        public Matrix4x4 objectToWorld;
        public Matrix4x4 prevObjectToWorld;
        public uint renderingLayerMask;
    }
}