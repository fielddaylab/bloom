using System;
using BeauUtil.Debugger;
using UnityEngine;

namespace Zavala {
    public unsafe struct InstancingHelper<T> : IDisposable where T : unmanaged {
        private T* m_DataHead;
        private readonly int m_MaxElements;
        private T* m_WriteHead;
        private int m_QueuedElements;

        public InstancingHelper(T* buffer, int bufferSize) {
            m_DataHead = buffer;
            m_MaxElements = Math.Min(bufferSize, 1023);
            m_WriteHead = buffer;
            m_QueuedElements = 0;
        }

        public void Queue(T data) {
            Assert.True(m_QueuedElements < m_MaxElements);
            *m_WriteHead++ = data;
            m_QueuedElements++;
        }

        public void Submit(in RenderParams renderParams, Mesh mesh, int submeshIndex = 0) {
            if (m_QueuedElements > 0) {
                Graphics.RenderMeshInstanced<T>(renderParams, mesh, submeshIndex, UnsafeExt.TempNativeArray(m_DataHead, m_QueuedElements), m_QueuedElements);
                m_QueuedElements = 0;
                m_WriteHead = m_DataHead;
            }
        }

        public void Dispose() {
            m_DataHead = null;
            m_WriteHead = null;
            m_QueuedElements = 0;
        }
    }
}