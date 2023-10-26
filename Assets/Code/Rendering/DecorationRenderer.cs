using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Zavala {
    public class DecorationRenderer : BatchedComponent {
        [Serializable]
        private struct DEBUG_Test {
            public Mesh Mesh;
            public TRS TRS;
        }

        [SerializeField, EditModeOnly] private DEBUG_Test[] m_DEBUG_InitialElements;

        [LayerIndex] public int Layer;
        public Material Material;

        [NonSerialized] public RingBuffer<Decoration> Decorations = new RingBuffer<Decoration>();
        [NonSerialized] public Transform CachedTransform;

        private void Awake() {
            this.CacheComponent(ref CachedTransform);

            foreach(var element in m_DEBUG_InitialElements) {
                DecorationUtility.AddDecoration(this, element.Mesh, element.TRS.Matrix);
            }
        }
    }

    public struct Decoration {
        public Mesh Mesh;
        public Matrix4x4 Matrix;
        public uint Tag;
    }

    static public class DecorationUtility {
        static public void AddDecoration(DecorationRenderer renderer, Mesh mesh, Matrix4x4 localMatrix, StringHash32 id = default) {
            AddDecoration(renderer, mesh, localMatrix, id.HashValue);
        }

        static public void AddDecoration(DecorationRenderer renderer, Mesh mesh, Matrix4x4 localMatrix, uint id) {
            Decoration d;
            d.Mesh = mesh;
            d.Matrix = renderer.CacheComponent(ref renderer.CachedTransform).localToWorldMatrix * localMatrix;
            d.Tag = id;
            renderer.Decorations.PushBack(d);
        }

        static public bool RemoveDecoration(DecorationRenderer renderer, StringHash32 id) {
            return RemoveDecoration(renderer, id.HashValue);
        }

        static public bool RemoveDecoration(DecorationRenderer renderer, uint id) {
            int idx = renderer.Decorations.FindIndex((d, i) => d.Tag == i, id);
            if (idx >= 0) {
                renderer.Decorations.FastRemoveAt(idx);
                return true;
            }

            return false;
        }

        static public void ClearDecorations(DecorationRenderer renderer) {
            renderer.Decorations.Clear();
        }
    }
}