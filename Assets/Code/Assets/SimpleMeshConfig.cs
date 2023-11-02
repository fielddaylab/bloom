using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Data;
using UnityEngine;
using Zavala.Sim;
using Zavala.World;

namespace Zavala {
    [Serializable]
    public struct SimpleMeshConfig {
        public Mesh Mesh;
        public Material Material;

        public SimpleMeshConfig(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                Material = renderer.sharedMaterial;
            } else {
                Material = null;
            }

            if (filter) {
                Mesh = filter.sharedMesh;
            } else {
                Mesh = null;
            }
        }

        public void Apply(MeshRenderer renderer, MeshFilter filter) {
            if (renderer) {
                renderer.sharedMaterial = Material;
            }

            if (filter) {
                filter.sharedMesh = Mesh;
            }
        }
    }
}